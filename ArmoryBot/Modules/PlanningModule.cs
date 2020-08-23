using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArmoryBot.Models;
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
        private static readonly Dictionary<DayOfWeek, IEmoji> DayReactions = new Dictionary<DayOfWeek, IEmoji>
        {
            {DayOfWeek.Sunday, new LocalCustomEmoji(new Snowflake(735837655320887316), "sunday")},
            {DayOfWeek.Monday, new LocalCustomEmoji(new Snowflake(735837655211966565), "monday")},
            {DayOfWeek.Tuesday, new LocalCustomEmoji(new Snowflake(735837655090200719), "tuesday")},
            {DayOfWeek.Wednesday, new LocalCustomEmoji(new Snowflake(735837655195189289), "wednesday")},
            {DayOfWeek.Thursday, new LocalCustomEmoji(new Snowflake(735837655073423440), "thursday")},
            {DayOfWeek.Friday, new LocalCustomEmoji(new Snowflake(735837654695936073), "friday")},
            {DayOfWeek.Saturday, new LocalCustomEmoji(new Snowflake(735837655161634866), "saturday")}
        };
        private static readonly IEmoji YesReaction = new LocalEmoji("✅");
        private static readonly IEmoji NoReaction = new LocalEmoji("❌");
        private static readonly string planMessagePrefix = "We would like to plan";
        
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
                $"{planMessagePrefix} {items.Count()} items! Please respond with the appropriate reactions. (Yes/No, and the days on which you are available, starting today)");
            foreach (var item in items)
            {
                var reply = await ReplyAsync(item);
                await AddReactions(reply);

            }
        }

        [Command("result")]
        [Description("Gets the results of the last planning")]
        public async Task ResultAsync()
        {
            var planning = await GetPlanningResults();
            var result = BuildResultMessage(planning);
            await ReplyAsync(result);
        }

        [Command("today")]
        [Description("Gets the results of the last planning for today")]
        public async Task TodayAsync()
        {
            var planning = await GetPlanningResults();
            if (planning.First().Message.CreatedAt < DateTimeOffset.Now.AddDays(-7))
            {
                await ReplyAsync("This planning was made more than a week ago, please create a new planning");
            }

            var result = BuildTodayMessage(planning, DateTimeOffset.Now);
            await ReplyAsync(result);
        }

        private string BuildResultMessage(List<PlanningResult> results)
        {
            var sb = new StringBuilder("These are the results of the last planning:\n");
            foreach (var result in results)
            {
                sb.AppendLine($"**{result.Message.Content}**");
                sb.AppendLine($"{YesReaction.MessageFormat} Wants to play: {string.Join(", ", result.YesUserNames)}");
                sb.AppendLine($"{NoReaction.MessageFormat} Does not want to play: {string.Join(", ", result.NoUserNames)}");
            }

            return sb.ToString();
        }

        private string BuildTodayMessage(List<PlanningResult> results, DateTimeOffset date)
        {
            var day = date.DayOfWeek;
            var sb = new StringBuilder($"These are the results for today, {DayReactions[day].MessageFormat}:\n");
            foreach (var result in results)
            {
                sb.AppendLine($"**{result.Message.Content}**");
                var names = result.DayUserNames[day];
                sb.AppendLine(names.Any()
                    ? $"Wants to play: {string.Join(", ", names)}"
                    : $"Nobody wants to play this today");
            }

            return sb.ToString();
        }

        private async Task<List<PlanningResult>> GetPlanningResults()
        {
            var messages = await Context.Channel.GetMessagesAsync(250);
            var ownMessages = messages.Where(m => m.Author.Id == Context.Bot.CurrentUser.Id).ToList();
            var planMessages = ownMessages.TakeWhile(m => !m.Content.StartsWith(planMessagePrefix)).Where(m => m.Reactions.Keys.Contains(YesReaction));

            var result = new List<PlanningResult>();
            foreach (var message in planMessages.Reverse())
            {
                var messageResult = await GetPlanningResult(message);
                result.Add(messageResult);
            }

            return result;
        }
        private async Task<PlanningResult> GetPlanningResult(RestMessage message)
        {
            var yesReactions = await message.GetReactionsAsync(YesReaction);
            var noReactions = await message.GetReactionsAsync(NoReaction);

            var result = new PlanningResult
            {
                Message = message,
                YesUserNames = SelectUserName(yesReactions),
                NoUserNames = SelectUserName(noReactions),
                DayUserNames = new Dictionary<DayOfWeek, List<string>>()
            };

            foreach (var dayReaction in message.Reactions.Keys.Where(r => DayReactions.ContainsValue(r)))
            {
                var dictEntry = DayReactions.Single(e => e.Value.Equals(dayReaction));
                var dayReactions = await message.GetReactionsAsync(dayReaction);
                result.DayUserNames.Add(dictEntry.Key, SelectUserName(dayReactions));
            }

            return result;
        }

        private static List<string> SelectUserName(IReadOnlyList<RestUser> users)
        {
            return users.Where(r => !r.IsBot).Select(r => r.Name).ToList();
        }

        private async Task AddReactions(RestUserMessage reply)
        {
            var currentDayOfWeek = DateTime.Today.DayOfWeek;
            var reactions = new List<IEmoji> {YesReaction, NoReaction};
            reactions.AddRange(GetDayReactions(currentDayOfWeek));

            foreach (var reaction in reactions)
            {
                await reply.AddReactionAsync(reaction);
            }
        }
        
        private IEnumerable<IEmoji> GetDayReactions(DayOfWeek currentDayOfWeek)
        {
            var result = DayReactions
                .SkipWhile(r => r.Key < currentDayOfWeek)
                .Select(r => r.Value)
                .ToList();
            result
                .AddRange(DayReactions
                .TakeWhile(r => r.Key < currentDayOfWeek)
                .Select(r => r.Value));

            return result;
        }
    }
}
