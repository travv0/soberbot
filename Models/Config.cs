﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Models
{
    public class Config
    {
        public ulong ID { get; set; }

        public ulong ServerID { get; set; }

        public int PruneDays { get; set; }

        public ulong MilestoneChannelID { get; set; }
    }
}