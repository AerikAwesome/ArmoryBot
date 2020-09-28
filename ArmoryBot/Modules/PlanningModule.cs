using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArmoryBot.Extensions;
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

            List<string> items;
            if (itemString.Equals("preset", StringComparison.InvariantCultureIgnoreCase))
            {
                var presetMessage = await GetPresetMessage();
                if (presetMessage == null)
                {
                    await ReplyAsync("Preset does not exist, use \"plan preset add new\" to create a new preset");
                    return;
                }

                items = presetMessage.Items;
            }
            else
            {
                items = itemString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).ToList();
            }

            var mainMessage = await ReplyAsync($"{planMessagePrefix} {items.Count()} items! Please respond with the appropriate reactions. (Yes/Maybe/No, and the days on which you are available, starting today)");

            var itemMessages = new List<RestUserMessage>();

            foreach (var item in items)
            {
                itemMessages.Add(await ReplyAsync(item));
            }

            await AddDayReactions(mainMessage, DateTime.Today.DayOfWeek);
            foreach (var message in itemMessages)
            {
                await AddConfirmReactions(message);
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

            var planning = await GetPlanningResult();
            if (planning.Message.CreatedAt < DateTimeOffset.Now.AddDays(-7))
            {
                await ReplyAsync("This planning was made more than a week ago, please create a new planning");
            }


            var itemMessages = new List<RestUserMessage>();

            foreach (var item in items)
            {
                itemMessages.Add(await ReplyAsync(item));
            }

            foreach (var message in itemMessages)
            {
                await AddConfirmReactions(message);
            }

            await Context.Message.DeleteAsync();
        }

        [Command("result")]
        [Description("Gets the results of the last planning")]
        public async Task ResultAsync()
        {
            var planning = await GetPlanningResult();
            var result = BuildResultMessage(planning);

            foreach (var message in result)
            {
                await ReplyAsync(message);
            }
            
            await Context.Message.DeleteAsync();
        }

        [Command("today")]
        [Description("Gets the results of the last planning for today")]
        public async Task TodayAsync()
        {
            var planning = await GetPlanningResult();
            if (planning.Message.CreatedAt < DateTimeOffset.Now.AddDays(-7))
            {
                await ReplyAsync("This planning was made more than a week ago, please create a new planning");
            }

            var result = BuildTodayMessage(planning, DateTimeOffset.Now);
            await ReplyAsync(result);

            await Context.Message.DeleteAsync();
        }

        [Command("preset")]
        [Description("Sets or adds presets for planning")]
        public async Task PresetAsync(string command, [Remainder] string items)
        {
            if (command.Equals("new", StringComparison.InvariantCultureIgnoreCase))
            {
                await PresetNew(items);
            }
            else if (command.Equals("add", StringComparison.InvariantCultureIgnoreCase))
            {
                await PresetAdd(items);
            }
            else if (command.Equals("remove", StringComparison.InvariantCultureIgnoreCase))
            {
                await PresetRemove(items);
            }
            else
            {
                await ReplyAsync(
                    "Invalid command, use \"plan preset new\" to create a new preset message, or \"plan preset add\" to add to an existing one. \"plan preset remove {index}\" can be used to remove items");
            }
            await Context.Message.DeleteAsync();
        }

        private async Task PresetNew(string items)
        {
            if (string.IsNullOrWhiteSpace(items))
            {
                await ReplyAsync("Please provide a comma-separated list of items to plan");
                return;
            }
            
            var oldMessage = await GetPresetMessage();
            if (oldMessage != null)
            {
                await oldMessage.Message.UnpinAsync();
                await oldMessage.Message.DeleteAsync();
            }

            var newMessage = new PresetMessage(items);
            newMessage.Message = await ReplyAsync(newMessage.GetMessageContent());

            await newMessage.Message.PinAsync();
        }

        private async Task PresetAdd(string items)
        {
            if (string.IsNullOrWhiteSpace(items))
            {
                await ReplyAsync("Please provide a comma-separated list of items to plan");
                return;
            }

            var message = await GetPresetMessage();
            if (message == null)
            {
                await ReplyAsync("No existing message found, create a new one using \"plan preset new\"");
            }
            
            message.AddItems(items);
            await message.Message.ModifyAsync(m => m.Content = message.GetMessageContent());
        }

        private async Task PresetRemove(string index)
        {
            var indexNumber = int.Parse(index);

            var message = await GetPresetMessage();
            if (message == null)
            {
                await ReplyAsync("No existing message found, create a new one using \"plan preset new\"");
            }
            message.Items.RemoveAt(indexNumber - 1);
            await message.Message.ModifyAsync(m => m.Content = message.GetMessageContent());
        }

        private async Task<PresetMessage> GetPresetMessage()
        {
            var messages = await Context.Channel.GetPinnedMessagesAsync();
            var ownMessages = messages.Where(m => m.Author.Id == Context.Bot.CurrentUser.Id).ToList();
            var message = ownMessages.FirstOrDefault(m => m.Content.StartsWith(PresetMessage.MessagePrefix));
            return message == null ? null : new PresetMessage(message);
        }

        private IEnumerable<string> BuildResultMessage(PlanningResult result)
        {
            var resultMessages = new List<string>();
            var sb = new StringBuilder("These are the results of the last planning:\n");
            foreach (var resultItem in result.Items.Where(r => r.YesUserNames.Any() || r.MaybeUserNames.Any()).OrderByDescending(r => r.YesUserNames.Count))
            {
                var itemStringBuilder = new StringBuilder($"**{resultItem.Message.Content}**\n");
                if (resultItem.YesUserNames.Any())
                    itemStringBuilder.AppendLine($"    {YesReaction.MessageFormat} {string.Join(", ", resultItem.YesUserNames)}");
                if (resultItem.MaybeUserNames.Any())
                    itemStringBuilder.AppendLine($"    {MaybeReaction.MessageFormat} {string.Join(", ", resultItem.MaybeUserNames)}");
                /*if (resultItem.NoUserNames.Any())
                    itemStringBuilder.AppendLine($"    {NoReaction.MessageFormat} {string.Join(", ", resultItem.NoUserNames)}");*/

                if (sb.Length + itemStringBuilder.Length > 2000)
                {
                    resultMessages.Add(" \n" + sb);
                    sb = itemStringBuilder;
                }
                else
                {
                    sb.Append(itemStringBuilder);
                }
            }
            resultMessages.Add(sb.ToString());

            return resultMessages;
        }

        private string BuildTodayMessage(PlanningResult result, DateTimeOffset date)
        {
            var day = date.DayOfWeek;
            var sb = new StringBuilder($"These are the results for today, {DayReactions[day].MessageFormat}:\n");

            sb.AppendLine("Available today:");
            foreach (var userName in result.DayUserNames[day])
            {
                sb.AppendLine($"- {userName}");
            }

            sb.AppendLine("");
            sb.AppendLine("Most voted items:");
            foreach (var item in result.Items.Where(i => i.YesUserNames.Count > 0).OrderByDescending(i => i.YesUserNames.Intersect(result.DayUserNames[day]).Count() + (i.MaybeUserNames.Intersect(result.DayUserNames[day]).Count() * 0.5)).Take(3))
            {
                var userString = $"({string.Join(", ", item.YesUserNames)})";
                if (item.MaybeUserNames.Any())
                    userString += $" ~ *({string.Join(", ", item.MaybeUserNames)})*";
                sb.AppendLine($"**{item.Message.Content}** {userString}");
            }
            
            return sb.ToString();
        }

        private async Task<PlanningResult> GetPlanningResult()
        {
            var messages = await Context.Channel.GetMessagesAsync(250);
            var ownMessages = messages.Where(m => m.Author.Id == Context.Bot.CurrentUser.Id).ToList();
            var planMessages = ownMessages.TakeUntilIncluding(m => !m.Content.StartsWith(planMessagePrefix));

            var result = new PlanningResult
            {
                Message = planMessages.Single(m => m.Content.StartsWith(planMessagePrefix))
            };
            
            foreach (var dayReaction in result.Message.Reactions.Keys.Where(r => DayReactions.ContainsValue(r)))
            {
                var dictEntry = DayReactions.Single(e => e.Value.Equals(dayReaction));
                var dayReactions = await result.Message.GetReactionsAsync(dayReaction);
                result.DayUserNames.Add(dictEntry.Key, SelectUserName(dayReactions));
            }

            foreach (var message in planMessages.Where(m => m.Reactions.Keys.Contains(YesReaction)).Reverse())
            {
                var messageResult = await GetPlanningResult(message);
                result.Items.Add(messageResult);
            }

            return result;
        }
        private async Task<ItemResult> GetPlanningResult(RestMessage message)
        {
            var yesReactions = await message.GetReactionsAsync(YesReaction);
            var maybeReactions = await message.GetReactionsAsync(MaybeReaction);
            var noReactions = await message.GetReactionsAsync(NoReaction);

            var result = new ItemResult()
            {
                Message = message,
                YesUserNames = SelectUserName(yesReactions),
                MaybeUserNames = SelectUserName(maybeReactions),
                NoUserNames = SelectUserName(noReactions),
            };
            return result;
        }

        private static List<string> SelectUserName(IReadOnlyList<RestUser> users)
        {
            return users.Where(r => !r.IsBot).Select(r => r.Name).ToList();
        }

        private async Task AddDayReactions(RestUserMessage reply, DayOfWeek startDay)
        {
            foreach (var reaction in GetDayReactions(startDay))
            {
                await reply.AddReactionAsync(reaction);
            }
        }
        private async Task AddConfirmReactions(RestUserMessage reply)
        {
            var reactions = new List<IEmoji> { YesReaction, MaybeReaction, NoReaction };
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
