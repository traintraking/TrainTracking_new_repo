using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrainTracking.Domain.Entities
{
    public class ApplicationUserOTP
    { 
            public string Id { get; set; }
            public string OTP { get; set; }

            public DateTime CreateAt { get; set; }
            public DateTime ValidTo { get; set; }
            public bool IsValid { get; set; }

            public string ApplicationUserId { get; set; }
            public IdentityUser ApplicationUser { get; set; }
        
    }
}
