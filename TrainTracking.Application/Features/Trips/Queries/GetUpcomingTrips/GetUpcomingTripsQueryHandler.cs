using AutoMapper;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TrainTracking.Application.DTOs;
using TrainTracking.Application.Interfaces;

namespace TrainTracking.Application.Features.Trips.Queries.GetUpcomingTrips;

public class GetUpcomingTripsQueryHandler : IRequestHandler<GetUpcomingTripsQuery, List<TripDto>>
{
    private readonly ITripRepository _tripRepository;
    private readonly IMapper _mapper;
    private readonly IVirtualSegmentService _virtualSegmentService;
    private readonly IStationRepository _stationRepository;

    public GetUpcomingTripsQueryHandler(
        ITripRepository tripRepository,
        IMapper mapper,
        IVirtualSegmentService virtualSegmentService,
        IStationRepository stationRepository)
    {
        _tripRepository = tripRepository;
        _mapper = mapper;
        _virtualSegmentService = virtualSegmentService;
        _stationRepository = stationRepository;
    }

    public async Task<List<TripDto>> Handle(GetUpcomingTripsQuery request, CancellationToken cancellationToken)
    {
        var trips = await _tripRepository.GetUpcomingTripsAsync(
            request.FromStationId,
            request.ToStationId,
            request.Date);

        var dtos = _mapper.Map<List<TripDto>>(trips);

        if (request.FromStationId.HasValue || request.ToStationId.HasValue)
        {
            var fromStation = request.FromStationId.HasValue ? await _stationRepository.GetByIdAsync(request.FromStationId.Value) : null;
            var toStation = request.ToStationId.HasValue ? await _stationRepository.GetByIdAsync(request.ToStationId.Value) : null;

            foreach (var dto in dtos)
            {
                var trip = trips.Find(t => t.Id == dto.Id);
                if (trip == null) continue;

                // إذا طلب المستخدم محطة بداية معينة
                if (fromStation != null)
                {
                    dto.FromStationName = fromStation.Name;
                    dto.FromStationLatitude = fromStation.Latitude;
                    dto.FromStationLongitude = fromStation.Longitude;
                    dto.DepartureTime = await _virtualSegmentService.CalculateDepartureTimeAsync(trip, fromStation);
                }

                // إذا طلب المستخدم محطة نهاية معينة
                if (toStation != null)
                {
                    dto.ToStationName = toStation.Name;
                    dto.ToStationLatitude = toStation.Latitude;
                    dto.ToStationLongitude = toStation.Longitude;
                    dto.ArrivalTime = await _virtualSegmentService.CalculateArrivalTimeAsync(trip, toStation);
                }

                // حساب السعر بناءً على المقطع (لو المقطع محدد بالكامل)
                if (fromStation != null && toStation != null)
                {
                    dto.Price = await _virtualSegmentService.CalculatePriceAsync(trip, fromStation, toStation);
                }
            }
        }

        return dtos;
    }
}
