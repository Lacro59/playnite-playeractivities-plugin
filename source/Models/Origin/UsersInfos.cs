﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models.Origin
{
    public class UsersInfos
    {
        public List<UserData> users { get; set; }
    }

    public class UserData
    {
        public string userId { get; set; }
        public string email { get; set; }
        public string personaId { get; set; }
        public string eaId { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public bool underageUser { get; set; }
        public bool isDiscoverableEmail { get; set; }
    }
}
