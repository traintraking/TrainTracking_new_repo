using System;
using System.Linq;
using System.Threading.Tasks;
using TrainTracking.Application.Interfaces;
using TrainTracking.Domain.Entities;

namespace TrainTracking.Application.Services
{
    public class TripService : ITripService
    {
        private readonly IStationRepository _stationRepository;
        private readonly ITrainRepository _trainRepository;


        public TripService(IStationRepository stationRepository, ITrainRepository trainRepository)
        {
            _stationRepository = stationRepository;
            _trainRepository = trainRepository;
        }

        public async Task<DateTimeOffset> CalculateArrivalTimeAsync(Guid fromStationId, Guid toStationId, DateTimeOffset departureTime)
        {
            var fromStation = await _stationRepository.GetByIdAsync(fromStationId);
            var toStation = await _stationRepository.GetByIdAsync(toStationId);
            var trains = await _trainRepository.GetAllAsync();
            var train = trains.FirstOrDefault();
            int TrainSpeedKmh = (train != null && train.speed > 0) ? train.speed : 300;

            if (fromStation == null || toStation == null)
                return departureTime.AddHours(1);

            double distanceKm = CalculateDistance(fromStation.Latitude, fromStation.Longitude, toStation.Latitude, toStation.Longitude);

            // Time in hours = Distance / Speed
            double travelTimeHours = distanceKm / TrainSpeedKmh;
            double travelTimeMinutes = travelTimeHours * 60;

            // Calculate intermediate stations for stop time (10 mins each)
            int stopCount = await GetIntermediateStationCountAsync(fromStationId, toStationId);
            travelTimeMinutes += (stopCount * 10);

            // Safety check for invalid numbers
            if (double.IsNaN(travelTimeMinutes) || double.IsInfinity(travelTimeMinutes))
            {
                return departureTime.AddHours(1); // Default fallback
            }

            return departureTime.AddMinutes(Math.Ceiling(travelTimeMinutes));
        }

        private async Task<int> GetIntermediateStationCountAsync(Guid fromId, Guid toId)
        {
            var fromStation = await _stationRepository.GetByIdAsync(fromId);
            var toStation = await _stationRepository.GetByIdAsync(toId);

            if (fromStation == null || toStation == null) return 0;

            var stations = await _stationRepository.GetAllAsync();
            var minOrder = Math.Min(fromStation.Order, toStation.Order);
            var maxOrder = Math.Max(fromStation.Order, toStation.Order);

            // Count stations strictly between from and to based on their Order
            int count = stations.Count(s => s.Order > minOrder && s.Order < maxOrder);
            return count;
        }

        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var r = 6371; // Radius of the earth in km
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = r * c; // Distance in km
            return d;
        }

        private double ToRadians(double deg)
        {
            return deg * (Math.PI / 180);
        }
    }
}
