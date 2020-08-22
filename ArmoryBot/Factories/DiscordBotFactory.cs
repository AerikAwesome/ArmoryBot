﻿using System;
using System.Collections.Generic;
using System.Text;
using ArmoryBot.Modules;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Prefixes;

namespace ArmoryBot.Factories
{
    public class DiscordBotFactory
    {
        public DiscordBot CreateBot(string token)
        {
            var prefixProvider = new DefaultPrefixProvider()
                .AddPrefix('*')
                .AddMentionPrefix();

            var bot = new DiscordBot(TokenType.Bot, token, prefixProvider);

            bot.AddModule<PingModule>();

            return bot;
        }
    }
}
