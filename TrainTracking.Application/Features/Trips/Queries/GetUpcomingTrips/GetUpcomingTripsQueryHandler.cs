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

            // إنشاء قائمة جديدة للنتائج بعد التصفية
            var filteredDtos = new List<TripDto>();

            foreach (var dto in dtos)
            {
                var trip = trips.Find(t => t.Id == dto.Id);
                if (trip == null) continue;

                // التحقق: إذا كانت المحطة المطلوبة موجودة في SkippedStationIds، تجاهل هذه الرحلة
                bool shouldSkip = false;

                if (fromStation != null && trip.SkippedStationIds.Contains(fromStation.Id))
                {
                    // المحطة المطلوبة للانطلاق متخطاة - لا تعرض الرحلة
                    shouldSkip = true;
                }

                if (toStation != null && trip.SkippedStationIds.Contains(toStation.Id))
                {
                    // المحطة المطلوبة للوصول متخطاة - لا تعرض الرحلة
                    shouldSkip = true;
                }

                if (shouldSkip)
                {
                    continue; // تجاهل هذه الرحلة تماماً
                }

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

                filteredDtos.Add(dto);
            }

            return filteredDtos;
        }

        return dtos;
    }
}
