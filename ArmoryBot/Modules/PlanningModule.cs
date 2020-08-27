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
        private static readonly IEmoji YesReaction = new LocalCustomEmoji(new Snowflake(748592107270439033), "tickyes");
        private static readonly IEmoji MaybeReaction = new LocalCustomEmoji(new Snowflake(748596111668936744), "wavemaybe");
        private static readonly IEmoji NoReaction = new LocalCustomEmoji(new Snowflake(748592107094147124), "crossno");
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
                $"{planMessagePrefix} {items.Count()} items! Please respond with the appropriate reactions. (Yes/Maybe/No, and the days on which you are available, starting today)");
            foreach (var item in items)
            {
                var reply = await ReplyAsync(item);
                await AddReactions(reply, DateTime.Today.DayOfWeek);

            }

            await Context.Message.DeleteAsync();
        }

        [Command("add")]
        [Description("Adds one or more items to the last planning entry")]
        public async Task AddAsync([Remainder] string itemString)
        {
            if (string.IsNullOrWhiteSpace(itemString))
            {
                await ReplyAsync("Please provide a comma-separated list of items to add");
                return;
            }
            var items = itemString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).ToList();

            var planning = await GetPlanningResults();
            if (planning.First().Message.CreatedAt < DateTimeOffset.Now.AddDays(-7))
            {
                await ReplyAsync("This planning was made more than a week ago, please create a new planning");
            }
            var day = planning.First().DayUserNames.Keys.First();


            foreach (var item in items)
            {
                var reply = await ReplyAsync(item);
                await AddReactions(reply, day);
            }

            await Context.Message.DeleteAsync();
        }

        [Command("result")]
        [Description("Gets the results of the last planning")]
        public async Task ResultAsync()
        {
            var planning = await GetPlanningResults();
            var result = BuildResultMessage(planning);
            await ReplyAsync(result);
            
            await Context.Message.DeleteAsync();
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

            await Context.Message.DeleteAsync();
        }
        
        private string BuildResultMessage(List<PlanningResult> results)
        {
            var sb = new StringBuilder("These are the results of the last planning:\n");
            foreach (var result in results.Where(r => r.YesUserNames.Any() || r.MaybeUserNames.Any()).OrderBy(r => r.YesUserNames.Count))
            {
                sb.AppendLine($"**{result.Message.Content}**");
                if (result.YesUserNames.Any())
                    sb.AppendLine($"{YesReaction.MessageFormat} Wants to play: {string.Join(", ", result.YesUserNames)}");
                if (result.MaybeUserNames.Any())
                    sb.AppendLine($"{MaybeReaction.MessageFormat} Maybe wants to play: {string.Join(", ", result.MaybeUserNames)}");
                if (result.NoUserNames.Any())
                    sb.AppendLine($"{NoReaction.MessageFormat} Does not want to play: {string.Join(", ", result.NoUserNames)}");
            }

            return sb.ToString();
        }

        private string BuildTodayMessage(List<PlanningResult> results, DateTimeOffset date)
        {
            var day = date.DayOfWeek;
            var sb = new StringBuilder($"These are the results for today, {DayReactions[day].MessageFormat}:\n");
            foreach (var result in results.Where(r => r.DayUserNames[day].Any()).OrderBy(r => r.DayUserNames[day].Count))
            {
                sb.AppendLine($"**{result.Message.Content}**");
                var names = result.DayUserNames[day];
                sb.AppendLine($"Wants to play: {string.Join(", ", names)}");
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
            var maybeReactions = await message.GetReactionsAsync(MaybeReaction);
            var noReactions = await message.GetReactionsAsync(NoReaction);

            var result = new PlanningResult
            {
                Message = message,
                YesUserNames = SelectUserName(yesReactions),
                MaybeUserNames = SelectUserName(maybeReactions),
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

        private async Task AddReactions(RestUserMessage reply, DayOfWeek startDay)
        {
            var reactions = new List<IEmoji> {YesReaction, MaybeReaction, NoReaction};
            reactions.AddRange(GetDayReactions(startDay));

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
