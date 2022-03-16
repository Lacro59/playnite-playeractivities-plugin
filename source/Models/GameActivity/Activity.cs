using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models.GameActivity
{
    public class GaActivity
    {
        public DateTime DateSession { get; set; }
        public ulong ElapsedSeconds { get; set; } = 0;
    }
}
