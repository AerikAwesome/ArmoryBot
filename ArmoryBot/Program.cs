using System;
using System.Threading.Tasks;
using ArmoryBot.Factories;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Prefixes;
using Microsoft.Extensions.Configuration;

namespace ArmoryBot
{
    public class Program
    {
        public static IConfigurationRoot Configuration { get; set; }
        static async Task Main(string[] args)
        {
            Configuration = BuildConfiguration();

            await using var bot = new DiscordBotFactory().CreateBot(Configuration["Discord:BotToken"]);
            await bot.RunAsync();
        }

        private static IConfigurationRoot BuildConfiguration()
        {
            var devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");

            var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) ||
                                devEnvironmentVariable.ToLower() == "development";

            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("appsettings.json");

            if (isDevelopment)
            {
                builder.AddUserSecrets<Program>();
            }

            return builder.Build();
        }
    }
}
