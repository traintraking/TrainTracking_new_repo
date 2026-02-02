using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrainTracking.Domain.Enums
{
    public enum TripStatus
    {
        Scheduled,
        OnTime,
        Delayed,
        Cancelled,
        Completed
    }
}
