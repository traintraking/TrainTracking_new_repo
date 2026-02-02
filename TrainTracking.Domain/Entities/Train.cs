using System;
using System.ComponentModel.DataAnnotations;
namespace TrainTracking.Domain.Entities;
public class Train
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TrainNumber { get; set; } = null!;

    [Range(1, 500, ErrorMessage = "سرعة القطار يجب أن تكون بين 1 و 500 كم/س")]
    public int speed { get; set; }
    public string Type { get; set; } = null!;

    [Required(ErrorMessage = "عدد المقاعد مطلوب")]
    [Range(20, 1000, ErrorMessage = "عدد المقاعد يجب أن يكون بين 20 و 1000")]
    public int TotalSeats { get; set; }
}
