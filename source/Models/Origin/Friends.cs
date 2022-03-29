using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models.Origin
{
    public class Friends
    {
        public PagingInfo pagingInfo { get; set; }
        public List<Entry> entries { get; set; }
    }

    public class PagingInfo
    {
        public int totalSize { get; set; }
        public int size { get; set; }
        public int offset { get; set; }
    }

    public class Entry
    {
        public string displayName { get; set; }
        public long timestamp { get; set; }
        public string friendType { get; set; }
        public DateTime dateTime { get; set; }
        public long userId { get; set; }
        public long personaId { get; set; }
        public bool favorite { get; set; }
        public string nickName { get; set; }
        public string userType { get; set; }
    }
}
