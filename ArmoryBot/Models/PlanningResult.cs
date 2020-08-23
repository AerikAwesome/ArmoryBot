using System;
using System.Collections.Generic;
using System.Text;
using Disqord.Rest;

namespace ArmoryBot.Models
{
    public class PlanningResult
    {
        public RestMessage Message { get; set; }
        public List<string> YesUserNames { get; set; }
        public List<string> NoUserNames { get; set; }
        public Dictionary<DayOfWeek, List<string>> DayUserNames { get; set; }
    }
}
