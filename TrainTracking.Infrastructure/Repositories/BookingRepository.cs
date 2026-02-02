using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TrainTracking.Application.Interfaces;
using TrainTracking.Domain.Entities;
using TrainTracking.Domain.Enums;
using TrainTracking.Infrastructure.Persistence;

namespace TrainTracking.Infrastructure.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly TrainTrackingDbContext _context;

        public BookingRepository(TrainTrackingDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Booking booking)
        {
            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();
        }
        public async Task<int> GetConfirmedSeatsCountAsync(Guid tripId)
        {
            return await _context.Bookings
                .CountAsync(b => b.TripId == tripId
                              && b.Status == BookingStatus.Confirmed);
        }

        public async Task<bool> IsSeatTakenAsync(Guid tripId, int seatNumber, Guid fromStationId, Guid toStationId)
        {
            var fromStation = await _context.Stations.FindAsync(fromStationId);
            var toStation = await _context.Stations.FindAsync(toStationId);

            if (fromStation == null || toStation == null) return true;

            return await _context.Bookings
                .Include(b => b.FromStation)
                .Include(b => b.ToStation)
                .AnyAsync(b =>
                    b.TripId == tripId &&
                    b.SeatNumber == seatNumber &&
                    b.Status != BookingStatus.Cancelled &&
                    // شرط التداخل: (بداية الحجز الحالي < نهاية الحجز المطلوب) و (نهاية الحجز الحالي > بداية الحجز المطلوب)
                    b.FromStation.Order < toStation.Order &&
                    b.ToStation.Order > fromStation.Order);
        }

        public async Task<Booking?> GetByIdAsync(Guid id)
        {
            return await _context.Bookings
                .Include(b => b.Trip)
                    .ThenInclude(t => t.Train)
                .Include(b => b.Trip)
                    .ThenInclude(t => t.FromStation)
                .Include(b => b.Trip)
                    .ThenInclude(t => t.ToStation)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<List<Booking>> GetBookingsByUserIdAsync(string userId)
        {
            return await _context.Bookings
                .Include(b => b.Trip)
                    .ThenInclude(t => t.Train)
                .Include(b => b.Trip)
                    .ThenInclude(t => t.FromStation)
                .Include(b => b.Trip)
                    .ThenInclude(t => t.ToStation)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetBookingsByTripIdAsync(Guid tripId)
        {
            return await _context.Bookings
                .Where(b => b.TripId == tripId && b.Status != BookingStatus.Cancelled)
                .ToListAsync();
        }

        public async Task<List<int>> GetTakenSeatsAsync(Guid tripId, Guid fromStationId, Guid toStationId)
        {
            var fromStation = await _context.Stations.FindAsync(fromStationId);
            var toStation = await _context.Stations.FindAsync(toStationId);

            if (fromStation == null || toStation == null) return new List<int>();

            var expirationTime = DateTimeOffset.Now.AddMinutes(-1);

            return await _context.Bookings
                .Include(b => b.FromStation)
                .Include(b => b.ToStation)
                .Where(b => b.TripId == tripId && (
                    b.Status == BookingStatus.Confirmed ||
                    (b.Status == BookingStatus.PendingPayment && b.BookingDate > expirationTime)
                ))
                .Where(b => b.FromStation.Order < toStation.Order && b.ToStation.Order > fromStation.Order)
                .Select(b => b.SeatNumber)
                .ToListAsync();
        }

        public async Task UpdateAsync(Booking booking)
        {
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                // Delete related notifications first to avoid FK constraint
                var relatedNotifications = await _context.Notifications
                    .Where(n => n.BookingId == id)
                    .ToListAsync();
                
                if (relatedNotifications.Any())
                {
                    _context.Notifications.RemoveRange(relatedNotifications);
                }

                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
            }
        }

        public async Task CreateRedemptionAsync(PointRedemption redemption)
        {
            await _context.PointRedemptions.AddAsync(redemption);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetRedeemedPointsAsync(string userId)
        {
            return await _context.PointRedemptions
                .Where(pr => pr.UserId == userId)
                .SumAsync(pr => pr.PointsRedeemed);
        }
    }
}
