using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models.HowLongToBeat
{
    public class HltbUserStats
    {
        public List<TitleList> TitlesList { get; set; } = new List<TitleList>();
    }

    public class TitleList
    {
        public int Id { get; set; }
        public string UserGameId { get; set; }

        public DateTime? Completion { get; set; }

        public List<GameStatus> GameStatuses { get; set; } = new List<GameStatus>();
    }


    public class GameStatus
    {
        public StatusType Status { get; set; }
        public long Time { get; set; }
    }


    public enum StatusType
    {
        Playing,
        Backlog,
        Replays,
        CustomTab,
        Completed,
        Retired
    }
}
