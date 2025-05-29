using System;

namespace PlayerActivities.Models.GameActivity
{
    public class GaActivity
    {
        public DateTime DateSession 
        {
            get => DateSession.ToLocalTime();
            set => DateSession = value.ToUniversalTime();
        }

        public ulong ElapsedSeconds { get; set; } = 0;
    }
}