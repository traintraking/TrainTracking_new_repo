using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrainTracking.Application.Interfaces;
using TrainTracking.Domain.Entities;
using TrainTracking.Domain.Enums;
using TrainTracking.Infrastructure.Persistence;

namespace TrainTracking.Infrastructure.Repositories;

public class TripRepository : ITripRepository
{
    private readonly TrainTrackingDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public TripRepository(TrainTrackingDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<List<Trip>> GetUpcomingTripsAsync(Guid? fromStationId = null, Guid? toStationId = null, DateTime? date = null)
    {
        var query = _context.Trips
            .Include(t => t.Train)
            .Include(t => t.FromStation)
            .Include(t => t.ToStation)
            .AsQueryable();

        if (fromStationId.HasValue && toStationId.HasValue)
        {
            var fromStation = await _context.Stations.FindAsync(fromStationId.Value);
            var toStation = await _context.Stations.FindAsync(toStationId.Value);

            if (fromStation != null && toStation != null)
            {
                // البحث الذكي مع مراعاة الاتجاه:
                // 1. إذا كان المستخدم يطلب رحلة للأمام (محطة القيام أصغر من الوصول)
                if (fromStation.Order < toStation.Order)
                {
                    query = query.Where(t =>
                        t.FromStation!.Order <= fromStation.Order &&
                        t.ToStation!.Order >= toStation.Order &&
                        t.FromStation.Order < t.ToStation.Order); // الرحلة أصلاً للأمام
                }
                // 2. إذا كان المستخدم يطلب رحلة للخلف
                else if (fromStation.Order > toStation.Order)
                {
                    query = query.Where(t =>
                        t.FromStation!.Order >= fromStation.Order &&
                        t.ToStation!.Order <= toStation.Order &&
                        t.FromStation.Order > t.ToStation.Order); // الرحلة أصلاً للخلف
                }
            }
        }
        else if (fromStationId.HasValue)
        {
            var fromStation = await _context.Stations.FindAsync(fromStationId.Value);
            if (fromStation != null)
            {
                // الرحلات التي تمر بهذه المحطة ولديها محطة تالية في أي اتجاه
                query = query.Where(t =>
                    (t.FromStation!.Order <= fromStation.Order && t.ToStation!.Order > fromStation.Order) || // للأمام
                    (t.FromStation!.Order >= fromStation.Order && t.ToStation!.Order < fromStation.Order)    // للخلف
                );
            }
        }
        else if (toStationId.HasValue)
        {
            var toStation = await _context.Stations.FindAsync(toStationId.Value);
            if (toStation != null)
            {
                // الرحلات التي تمر بهذه المحطة ولديها محطة سابقة في أي اتجاه
                query = query.Where(t =>
                    (t.ToStation!.Order >= toStation.Order && t.FromStation!.Order < toStation.Order) || // للأمام
                    (t.ToStation!.Order <= toStation.Order && t.FromStation!.Order > toStation.Order)    // للخلف
                );
            }
        }

        var now = _dateTimeService.Now;
        var oneHourAgo = now.AddHours(-1);

        // Fetch and filter in-memory as a fail-safe for SQLite DateTimeOffset string comparison inconsistencies
        var allTrips = await query.ToListAsync();

        if (date.HasValue)
        {
            var targetDate = date.Value.Date;
            var start = new DateTimeOffset(targetDate, _dateTimeService.Now.Offset);
            var end = start.AddDays(1);

            return allTrips.Where(t =>
                (t.DepartureTime >= start && t.DepartureTime < end && t.DepartureTime >= now && t.Status != TripStatus.Completed) ||
                (t.Status == TripStatus.Cancelled && t.CancelledAt >= oneHourAgo && t.CancelledAt >= start && t.CancelledAt < end)
            ).OrderBy(t => t.DepartureTime).ToList();
        }
        else
        {
            return allTrips.Where(t =>
                (t.DepartureTime >= now && t.Status != TripStatus.Completed) ||
                (t.Status == TripStatus.Cancelled && t.CancelledAt >= oneHourAgo)
            ).OrderBy(t => t.DepartureTime).ToList();
        }
    }

    public async Task<Trip?> GetTripWithStationsAsync(Guid id)
    {
        return await _context.Trips
            .Include(t => t.Train)
            .Include(t => t.FromStation)
            .Include(t => t.ToStation)
            .FirstOrDefaultAsync(t => t.Id == id);
    }


    public async Task<Trip?> GetByIdAsync(Guid id)
    {
        return await _context.Trips.FindAsync(id);
    }


    public async Task CompleteFinishedTripsAsync()
    {
        var now = _dateTimeService.Now;

        var tripsToComplete = await _context.Trips
            .Where(t =>
                t.Status != TripStatus.Completed &&
                t.Status != TripStatus.Cancelled &&
                t.ArrivalTime <= now
            )
            .ToListAsync();

        if (!tripsToComplete.Any())
            return;

        foreach (var trip in tripsToComplete)
        {
            trip.Status = TripStatus.Completed;
        }
        Console.WriteLine($"[TripService] Completed trips check at {_dateTimeService.Now}");
        await _context.SaveChangesAsync();
    }



    public async Task AddAsync(Trip trip)
    {
        _context.Trips.Add(trip);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Trip trip)
    {
        _context.Trips.Update(trip);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var trip = await _context.Trips.FindAsync(id);
        if (trip != null)
        {
            // 1. Delete notifications related to this trip
            var tripNotifications = await _context.Notifications
                .Where(n => n.TripId == id)
                .ToListAsync();
            if (tripNotifications.Any())
            {
                _context.Notifications.RemoveRange(tripNotifications);
            }

            // 2. Delete bookings related to this trip (and their notifications)
            var tripBookings = await _context.Bookings
                .Where(b => b.TripId == id)
                .ToListAsync();

            if (tripBookings.Any())
            {
                var bookingIds = tripBookings.Select(b => b.Id).ToList();
                var bookingNotifications = await _context.Notifications
                    .Where(n => n.BookingId.HasValue && bookingIds.Contains(n.BookingId.Value))
                    .ToListAsync();

                if (bookingNotifications.Any())
                {
                    _context.Notifications.RemoveRange(bookingNotifications);
                }

                _context.Bookings.RemoveRange(tripBookings);
            }

            // 3. Finally delete the trip
            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();
        }
    }
}
