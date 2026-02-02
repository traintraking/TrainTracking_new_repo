using System;
using System.Threading.Tasks;
using TrainTracking.Domain.Entities;

namespace TrainTracking.Application.Interfaces
{
    public interface IBookingRepository
    {
        Task CreateAsync(Booking booking);
        Task<int> GetConfirmedSeatsCountAsync(Guid tripId);
        Task<bool> IsSeatTakenAsync(Guid tripId, int seatNumber, Guid fromStationId, Guid toStationId);
        Task<Booking?> GetByIdAsync(Guid id);
        Task<List<Booking>> GetBookingsByUserIdAsync(string userId);
        Task<List<Booking>> GetBookingsByTripIdAsync(Guid tripId);
        Task<List<int>> GetTakenSeatsAsync(Guid tripId, Guid fromStationId, Guid toStationId);
        Task UpdateAsync(Booking booking);
        Task DeleteAsync(Guid id);

        // Point Redemption
        Task CreateRedemptionAsync(PointRedemption redemption);
        Task<int> GetRedeemedPointsAsync(string userId);
    }
}
