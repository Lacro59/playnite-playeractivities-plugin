using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models.Gog
{
    public class ProfileGames
    {
        public int page { get; set; }
        public int limit { get; set; }
        public int pages { get; set; }
        public int total { get; set; }
        public Links _links { get; set; }
        public Embedded _embedded { get; set; }
    }

    public class Self
    {
        public string href { get; set; }
    }

    public class First
    {
        public string href { get; set; }
    }

    public class Last
    {
        public string href { get; set; }
    }

    public class Next
    {
        public string href { get; set; }
    }

    public class Links
    {
        public Self self { get; set; }
        public First first { get; set; }
        public Last last { get; set; }
        public Next next { get; set; }
    }

    public class Game
    {
        public string id { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public bool achievementSupport { get; set; }
        public string image { get; set; }
    }

    public class Item
    {
        public Game game { get; set; }
        public object stats { get; set; }
    }

    public class Embedded
    {
        public List<Item> items { get; set; }
    }
}
