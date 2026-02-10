using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainTracking.Application.Interfaces;
using TrainTracking.Domain.Enums;
using TrainTracking.Domain.Entities;
using TrainTracking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrainTracking.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ITripRepository _tripRepository;
        private readonly ITrainRepository _trainRepository;
        private readonly IStationRepository _stationRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly ISmsService _smsService;
        private readonly INotificationRepository _notificationRepository;
        private readonly TrainTrackingDbContext _context;
        private readonly IDateTimeService _dateTimeService;
        private readonly ITripService _tripService;
        private readonly IUserService _userService;

        public AdminController(ITripRepository tripRepository, ITrainRepository trainRepository, 
            IStationRepository stationRepository, IBookingRepository bookingRepository, ISmsService smsService,
            INotificationRepository notificationRepository, TrainTrackingDbContext context, IDateTimeService dateTimeService,
            ITripService tripService, IUserService userService)
        {
            _tripRepository = tripRepository;
            _trainRepository = trainRepository;
            _stationRepository = stationRepository;
            _bookingRepository = bookingRepository;
            _smsService = smsService;
            _notificationRepository = notificationRepository;
            _context = context;
            _dateTimeService = dateTimeService;
            _tripService = tripService;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            await PurgeStaleTrips();

            // Total Stats
            var revenueData = await _context.Bookings
                .Where(b => b.Status == BookingStatus.Confirmed)
                .Select(b => b.Price)
                .ToListAsync();
            ViewBag.TotalRevenue = revenueData.Sum();

            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            var now = _dateTimeService.Now;
            
            ViewBag.ActiveTrips = await _context.Trips
                .Where(t => t.DepartureTime > now && t.Status != TripStatus.Completed)
                .CountAsync();
            ViewBag.TotalUsers = await _userService.GetTotalUsersCountAsync();

            // Recent Bookings
            var recentBookings = await _context.Bookings
                .Include(b => b.Trip)
                .OrderByDescending(b => b.BookingDate)
                .Take(5)
                .ToListAsync();

            // Data for Charts (Last 7 Days)
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => now.Date.AddDays(-i))
                .OrderBy(d => d)
                .ToList();

            var chartData = new List<int>();
            var chartLabels = new List<string>();

            foreach (var day in last7Days)
            {
                var startOfDay = day.Date;
                var endOfDay = startOfDay.AddDays(1);
                var count = await _context.Bookings
                    .CountAsync(b => b.BookingDate >= startOfDay && b.BookingDate < endOfDay);
                chartData.Add(count);
                chartLabels.Add(day.ToString("MMM dd"));
            }

            ViewBag.ChartData = chartData;
            ViewBag.ChartLabels = chartLabels;

            return View(recentBookings);
        }

        // --- Trips Management ---
        public async Task<IActionResult> Trips()
        {
            await PurgeStaleTrips();
            var trips = await _tripRepository.GetUpcomingTripsAsync();
            return View(trips);
        }

        public async Task<IActionResult> CreateTrip()
        {
            ViewBag.Trains = await _trainRepository.GetAllAsync();
            ViewBag.Stations = await _stationRepository.GetAllAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateTrip(Trip trip)
        {
            //لاكتشاف الخطا
            //if (!ModelState.IsValid)
            //{
            //    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            //    {
            //        Console.WriteLine(error.ErrorMessage);
            //    }
            //}


            if (ModelState.IsValid)
            {
                // Adjust times using DateTimeService
                trip.DepartureTime = new DateTimeOffset(trip.DepartureTime.DateTime, _dateTimeService.Now.Offset);
                trip.ArrivalTime = new DateTimeOffset(trip.ArrivalTime.DateTime, _dateTimeService.Now.Offset);

                await _tripRepository.AddAsync(trip);
                return RedirectToAction(nameof(Trips));
            }
            ViewBag.Trains = await _trainRepository.GetAllAsync();
            ViewBag.Stations = await _stationRepository.GetAllAsync();
            return View(trip);
        }

        [HttpGet]
        public async Task<IActionResult> GetCalculatedArrivalTime(Guid fromStationId, Guid toStationId, DateTime departureTime)
        {
            try
            {
                // Use the Kuwait offset
                var departureOffset = new DateTimeOffset(departureTime, _dateTimeService.Now.Offset);
                var arrivalTime = await _tripService.CalculateArrivalTimeAsync(fromStationId, toStationId, departureOffset);
                
                return Json(new { 
                    success = true, 
                    arrivalTime = arrivalTime.ToString("yyyy-MM-ddTHH:mm"),
                    displayTime = arrivalTime.ToString("hh:mm tt")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCalculatedArrivalAndPrice(Guid fromStationId, Guid toStationId, DateTime departureTime)
        {
            try
            {
                // Calculate arrival time
                var departureOffset = new DateTimeOffset(departureTime, _dateTimeService.Now.Offset);
                var arrivalTime = await _tripService.CalculateArrivalTimeAsync(fromStationId, toStationId, departureOffset);

                // Calculate price
                var fromStation = await _stationRepository.GetByIdAsync(fromStationId);
                var toStation = await _stationRepository.GetByIdAsync(toStationId);
                if (fromStation == null || toStation == null)
                {
                    return Json(new { success = false, message = "محطة غير موجودة" });
                }

                // Calculate distance
                var distanceKm = CalculateDistance(fromStation.Latitude, fromStation.Longitude, toStation.Latitude, toStation.Longitude);

                // Calculate price Egyptian Pounds
                const decimal ratePerKm = 0.5m;  // Example: 0.5 EGP per km
                decimal calculatedPrice = (decimal)distanceKm * ratePerKm;

                // Round to 3 decimal places
                calculatedPrice = Math.Round(calculatedPrice, 3);

                return Json(new
                {
                    success = true,
                    arrivalTime = arrivalTime.ToString("yyyy-MM-ddTHH:mm"),
                    displayTime = arrivalTime.ToString("hh:mm tt"),
                    price = calculatedPrice  // رجع decimal كـ number
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Radius of the earth in km
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c; // Distance in km
            return d;
        }

        private double ToRadians(double deg)
        {
            return deg * (Math.PI / 180);
        }
        private async Task PrepareTripDropdowns()
        {
            ViewBag.Trains = await _trainRepository.GetAllAsync();
            ViewBag.Stations = await _stationRepository.GetAllAsync();
        }
        public async Task<IActionResult> EditTrip(Guid id)
        {
            var trip = await _tripRepository.GetTripWithStationsAsync(id);
            if (trip == null) return NotFound();

            ViewBag.Trains = await _trainRepository.GetAllAsync();
            ViewBag.Stations = await _stationRepository.GetAllAsync();
            return View("CreateTrip", trip);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTrip(Trip trip)
        {
            // Make sure about
            if (ModelState.IsValid)
            {
                // Make sure about the times
                trip.DepartureTime = new DateTimeOffset(trip.DepartureTime.DateTime, _dateTimeService.Now.Offset);
                trip.ArrivalTime = new DateTimeOffset(trip.ArrivalTime.DateTime, _dateTimeService.Now.Offset);

                // Handle cancellation logic
                if (trip.Status == TripStatus.Cancelled && trip.CancelledAt == null)
                {
                    trip.CancelledAt = _dateTimeService.Now;
                }

                // Update in the database
                // Since the trip object now contains the Price coming from the form, the price will also be updated
                await _tripRepository.UpdateAsync(trip);

                // Handle delay logic
                if (trip.Status == TripStatus.Delayed)
                {
                    var bookings = await _bookingRepository.GetBookingsByTripIdAsync(trip.Id);
                    foreach (var booking in bookings)
                    {
                        var phoneNumber = booking.PassengerPhone;
                        if (!phoneNumber.StartsWith("+") && phoneNumber.Length == 8)
                        {
                            phoneNumber = "+965" + phoneNumber;
                        }

                        var delayMsg = $"تنبيه: رحلتك {trip.Id.ToString().Substring(0, 5)} متأخرة {trip.DelayMinutes} دقيقة. نعتذر عن الإزعاج. 🏛️🚅";

                        // Send the message
                        var smsResult = await _smsService.SendSmsAsync(phoneNumber, delayMsg);

                        // Save to the log
                        await _notificationRepository.CreateAsync(new Notification
                        {
                            Recipient = phoneNumber,
                            Message = delayMsg,
                            Type = NotificationType.SMS,
                            TripId = trip.Id,
                            BookingId = booking.Id,
                            IsSent = smsResult.Success,
                            ErrorMessage = smsResult.ErrorMessage
                        });
                    }
                }

                return RedirectToAction(nameof(Trips)); // Redirect to Trips action
            }

            // In case of error (like entering a negative price), we reload the dropdowns
            ViewBag.Trains = await _trainRepository.GetAllAsync();
            ViewBag.Stations = await _stationRepository.GetAllAsync();

            return View("EditTrip", trip);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteTrip(Guid id)
        {
            try
            {
                await _tripRepository.DeleteAsync(id);
                TempData["SuccessMessage"] = "تم حذف الرحلة بنجاح";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء حذف الرحلة: " + ex.Message;
            }
            return RedirectToAction(nameof(Trips));
        }

        // --- Trains Management ---
        public async Task<IActionResult> Trains()
        {
            var trains = await _trainRepository.GetAllAsync();
            return View(trains);
        }

        public IActionResult CreateTrain()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateTrain(Train train)
        {
            if (ModelState.IsValid)
            {
                await _trainRepository.AddAsync(train);
                return RedirectToAction(nameof(Trains));
            }
            return View(train);
        }

        public async Task<IActionResult> EditTrain(Guid id)
        {
            var train = await _trainRepository.GetByIdAsync(id);
            if (train == null) return NotFound();
            return View("CreateTrain", train);
        }

        [HttpPost]
        public async Task<IActionResult> EditTrain(Train train)
        {
            if (ModelState.IsValid)
            {
                await _trainRepository.UpdateAsync(train);
                return RedirectToAction(nameof(Trains));
            }
            return View("CreateTrain", train);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTrain(Guid id)
        {
            // Check if train is used in any trips
            var trips = await _tripRepository.GetUpcomingTripsAsync();
            var isUsed = trips.Any(t => t.TrainId == id);
            
            if (isUsed)
            {
                TempData["ErrorMessage"] = "لا يمكن حذف هذا القطار لأنه مستخدم في رحلات موجودة. يرجى حذف الرحلات أولاً.";
                return RedirectToAction(nameof(Trains));
            }
            
            try
            {
                await _trainRepository.DeleteAsync(id);
                TempData["SuccessMessage"] = "تم حذف القطار بنجاح";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء حذف القطار: " + ex.Message;
            }
            return RedirectToAction(nameof(Trains));
        }

        // --- Stations Management ---
        public async Task<IActionResult> Stations()
        {
            var stations = await _stationRepository.GetAllAsync();
            return View(stations);
        }

        public IActionResult CreateStation()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateStation(Station station)
        {
            if (ModelState.IsValid)
            {
                await _stationRepository.AddAsync(station);
                return RedirectToAction(nameof(Stations));
            }
            return View(station);
        }

        public async Task<IActionResult> EditStation(Guid id)
        {
            var station = await _stationRepository.GetByIdAsync(id);
            if (station == null) return NotFound();
            return View("CreateStation", station);
        }

        [HttpPost]
        public async Task<IActionResult> EditStation(Station station)
        {
            if (ModelState.IsValid)
            {
                await _stationRepository.UpdateAsync(station);
                return RedirectToAction(nameof(Stations));
            }
            return View("CreateStation", station);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStation(Guid id)
        {
            // Check if station is used in any trips
            var trips = await _tripRepository.GetUpcomingTripsAsync();
            var isUsed = trips.Any(t => t.FromStationId == id || t.ToStationId == id);
            
            if (isUsed)
            {
                TempData["ErrorMessage"] = "لا يمكن حذف هذه المحطة لأنها مستخدمة في رحلات موجودة. يرجى حذف الرحلات أولاً.";
                return RedirectToAction(nameof(Stations));
            }
            
            try
            {
                await _stationRepository.DeleteAsync(id);
                TempData["SuccessMessage"] = "تم حذف المحطة بنجاح";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء حذف المحطة: " + ex.Message;
            }
            return RedirectToAction(nameof(Stations));
        }

        public async Task<IActionResult> Simulator()
        {
            var trips = await _tripRepository.GetUpcomingTripsAsync();
            return View(trips);
        }

        public async Task<IActionResult> Notifications()
        {
            var notifications = await _notificationRepository.GetAllAsync();
            return View(notifications);
        }

        public async Task<IActionResult> AllBookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Trip)
                .ThenInclude(t => t.FromStation)
                .Include(b => b.Trip)
                .ThenInclude(t => t.ToStation)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        private async Task PurgeStaleTrips()
        {
            try
            {
                var now = _dateTimeService.Now;

                // Fetch trips and filter in-memory to bypass SQLite's DateTimeOffset string comparison quirks
                var trips = await _context.Trips.ToListAsync();
                var staleTrips = trips.Where(t => t.DepartureTime < now.AddMinutes(-30)).ToList();

                if (staleTrips.Any())
                {
                    var staleIds = staleTrips.Select(t => t.Id).ToList();
                    
                    // Related bookings
                    var bookings = await _context.Bookings.Where(b => staleIds.Contains(b.TripId)).ToListAsync();
                    _context.Bookings.RemoveRange(bookings);

                    // Related notifications
                    var notifications = await _context.Notifications.Where(n => n.TripId.HasValue && staleIds.Contains(n.TripId.Value)).ToListAsync();
                    _context.Notifications.RemoveRange(notifications);

                    _context.Trips.RemoveRange(staleTrips);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[PURGE]: Successfully deleted {staleTrips.Count} stale trips.");
                }
            }
            catch (Exception ex)
            {
                // In a real app, use logger. For now, we'll just print to console for debugging
                Console.WriteLine($"[PURGE ERROR]: {ex.Message}");
            }
        }
    }
}
