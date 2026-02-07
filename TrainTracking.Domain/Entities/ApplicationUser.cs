using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace TrainTracking.Domain.Entities
{
    // نستخدم ApplicationUser ليكون هو نفسه الـ IdentityUser مضافاً إليه بيانات البروفايل
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(14, MinimumLength = 14, ErrorMessage = "الرقم القومي يجب أن يتكون من 14 رقم")]
        public string? NationalId { get; set; }

        public string? ProfilePicturePath { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(100)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? Country { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;
        
        public int Points { get; set; } = 0;
    }
}
