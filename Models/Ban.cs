using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Models
{
    public class Ban
    {
        public ulong ID { get; set; }

        public ulong ServerID { get; set; }

        public ulong UserID { get; set; }

        public string? Message { get; set; }
    }
}