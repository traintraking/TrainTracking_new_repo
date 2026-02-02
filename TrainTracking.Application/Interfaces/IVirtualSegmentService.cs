using System;
using TrainTracking.Domain.Entities;

namespace TrainTracking.Application.Interfaces
{
    public interface IVirtualSegmentService
    {
        Task<decimal> CalculatePriceAsync(Trip trip, Station fromStation, Station toStation);
        Task<DateTimeOffset> CalculateDepartureTimeAsync(Trip trip, Station fromStation);
        Task<DateTimeOffset> CalculateArrivalTimeAsync(Trip trip, Station toStation);
    }
}
