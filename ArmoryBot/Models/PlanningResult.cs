using System;
using System.Collections.Generic;
using System.Text;
using Disqord.Rest;

namespace ArmoryBot.Models
{
    public class PlanningResult
    {
        public RestMessage Message { get; set; }
        public Dictionary<DayOfWeek, List<string>> DayUserNames { get; set; }
        public List<ItemResult> Items { get; set; }

        public PlanningResult()
        {
            DayUserNames = new Dictionary<DayOfWeek, List<string>>();
            Items = new List<ItemResult>();
        }
    }

    public class ItemResult
    {
        public RestMessage Message { get; set; }
        public List<string> YesUserNames { get; set; }
        public List<string> MaybeUserNames { get; set; }
        public List<string> NoUserNames { get; set; }
    }
}
