using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace TaskbarGroupTool.Models
{
    public class UsageStatistics
    {
        public string ApplicationPath { get; set; }
        public string ApplicationName { get; set; }
        public int LaunchCount { get; set; }
        public DateTime LastUsed { get; set; }
        public List<DateTime> UsageHistory { get; set; } = new List<DateTime>();
    }

    public class GroupUsageStatistics
    {
        public string GroupName { get; set; }
        public int LaunchCount { get; set; }
        public DateTime LastUsed { get; set; }
        public List<DateTime> UsageHistory { get; set; } = new List<DateTime>();
    }
}
