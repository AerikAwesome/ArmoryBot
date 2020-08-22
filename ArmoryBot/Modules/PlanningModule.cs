using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Qmmands;

namespace ArmoryBot.Modules
{
    [Group("plan")]
    [Description("Manages planning games with groups of people using reactions")]
    public class PlanningModule : DiscordModuleBase
    {
        private static Dictionary<DayOfWeek, IEmoji> _dayReactions = new Dictionary<DayOfWeek, IEmoji>
        {
            {DayOfWeek.Sunday, new LocalCustomEmoji(new Snowflake(735837655320887316), "sunday")},
            {DayOfWeek.Monday, new LocalCustomEmoji(new Snowflake(735837655211966565), "monday")},
            {DayOfWeek.Tuesday, new LocalCustomEmoji(new Snowflake(735837655090200719), "tuesday")},
            {DayOfWeek.Wednesday, new LocalCustomEmoji(new Snowflake(735837655195189289), "wednesday")},
            {DayOfWeek.Thursday, new LocalCustomEmoji(new Snowflake(735837655073423440), "thursday")},
            {DayOfWeek.Friday, new LocalCustomEmoji(new Snowflake(735837654695936073), "friday")},
            {DayOfWeek.Saturday, new LocalCustomEmoji(new Snowflake(735837655161634866), "saturday")}
        };

        private static IEmoji _yesReaction = new LocalEmoji("✅");
        private static IEmoji _noReaction = new LocalEmoji("❌");
        
        public PlanningModule()
        {
        }

        [Command("new")]
        [Description("Adds a new planning entry")]
        public async Task PlanAsync([Remainder] string itemString)
        {
            if (string.IsNullOrWhiteSpace(itemString))
            {
                await ReplyAsync("Please provide a comma-separated list of items to plan");
                return;
            }
            
            var items = itemString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).ToList();

            await ReplyAsync(
                $"We would like to plan {items.Count()} items! Please respond with the appropriate reactions. (Yes/No, and the days on which you are available, starting today)");
            foreach (var item in items)
            {
                var reply = await ReplyAsync(item);
                await AddReactions(reply);

            }
        }

        private async Task AddReactions(RestUserMessage reply)
        {
            var currentDayOfWeek = DateTime.Today.DayOfWeek;
            var reactions = new List<IEmoji> {_yesReaction, _noReaction};
            reactions.AddRange(GetDayReactions(currentDayOfWeek));

            foreach (var reaction in reactions)
            {
                await reply.AddReactionAsync(reaction);
            }
        }
        
        private IEnumerable<IEmoji> GetDayReactions(DayOfWeek currentDayOfWeek)
        {
            var result = _dayReactions
                .SkipWhile(r => r.Key < currentDayOfWeek)
                .Select(r => r.Value)
                .ToList();
            result
                .AddRange(_dayReactions
                .TakeWhile(r => r.Key < currentDayOfWeek)
                .Select(r => r.Value));

            return result;
        }
    }
}
