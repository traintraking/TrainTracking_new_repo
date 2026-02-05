using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrainTracking.Application.DTOs
{
    public class ForgetPasswordDTO
    {
        public int Id { get; set; }
        [Required]
        public string UserNameOREmail { get; set; } = string.Empty;
    }
}
