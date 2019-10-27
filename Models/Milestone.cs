using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Models
{
    public class Milestone
    {
        public ulong ID { get; set; }

        public int Days { get; set; }

        public string? Name { get; set; }
    }
}