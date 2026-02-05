using System;
using System.ComponentModel.DataAnnotations;

namespace TrainTracking.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "اسم المستخدم مطلوب")]
        [StringLength(50)]
        public string Username { get; set; }

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [StringLength(255)]
        public string Password { get; set; }

        // الحاجات الاختيارية (لاحظ علامة ?)
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
    }
}