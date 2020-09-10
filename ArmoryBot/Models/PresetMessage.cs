using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord.Rest;

namespace ArmoryBot.Models
{
    public class PresetMessage
    {
        public static readonly string MessagePrefix = "These are our favourite items:";
        
        public List<string> Items { get; set; }
        public RestUserMessage Message { get; set; }

        public PresetMessage(RestUserMessage message)
        {
            Message = message;
            Items = Message.Content
                .Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)
                .Where(i => char.IsDigit(i.First()))
                .Select(l => l.Split('.', StringSplitOptions.RemoveEmptyEntries))
                .Where(i => i.Length == 2)
                .Select(l => l.Last())
                .ToList();
        }

        public PresetMessage(string items)
        {
            Items = new List<string>();
            foreach (var item in items.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()))
            {
                Items.Add(item);
            }
        }

        public void AddItems(string items)
        {
            foreach (var item in items.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()))
            {
                Items.Add(item);
            }
        }

        public string GetMessageContent()
        {
            var sb = new StringBuilder(MessagePrefix);
            sb.AppendLine("");
            var index = 1;
            foreach (var item in Items)
            {
                sb.AppendLine($"{index}. {item}");
                index++;
            }

            return sb.ToString();
        }
    }
}
