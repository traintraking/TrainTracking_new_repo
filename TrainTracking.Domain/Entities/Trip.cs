using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrainTracking.Domain.Entities;

public class Trip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TrainId { get; set; }
    public Train? Train { get; set; }
    public Guid FromStationId { get; set; }
    public Station? FromStation { get; set; }
    public Guid ToStationId { get; set; }

    [Display(Name = "سعر الرحلة")]
    [Column(TypeName = "decimal(18, 3)")] 
    public decimal Price { get; set; }

    public Station? ToStation { get; set; }
    public DateTimeOffset DepartureTime { get; set; }
    public DateTimeOffset ArrivalTime { get; set; }
    public Domain.Enums.TripStatus Status { get; set; } = Domain.Enums.TripStatus.Scheduled;
    public int? DelayMinutes { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? PathPolyline { get; set; }

    // Skipped stations (stored as JSON for SQLite compatibility)
    public string? SkippedStationIdsJson { get; set; }

    [NotMapped]
    public List<Guid> SkippedStationIds
    {
        get => string.IsNullOrEmpty(SkippedStationIdsJson)
            ? new List<Guid>()
            : System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(SkippedStationIdsJson) ?? new List<Guid>();
        set => SkippedStationIdsJson = System.Text.Json.JsonSerializer.Serialize(value);
    }
}
