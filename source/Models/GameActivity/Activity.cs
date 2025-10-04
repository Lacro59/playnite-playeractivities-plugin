using System;

namespace PlayerActivities.Models.GameActivity
{
    public class GaActivity
    {
        public DateTime DateSession { get; set; }

        public ulong ElapsedSeconds { get; set; } = 0;
    }
}