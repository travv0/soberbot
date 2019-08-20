using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Models
{
    public class Sobriety
    {
        public ulong ID { get; set; }

        public ulong UserID { get; set; }

        public string UserName { get; set; }

        public ulong ServerID { get; set; }

        public DateTime SobrietyDate { get; set; }
    }
}
