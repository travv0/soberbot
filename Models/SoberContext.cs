using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Models
{
    public class SoberContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=soberbot.db");
        }

        public DbSet<Sobriety> Sobrieties { get; set; }
        public DbSet<Config> Config { get; set; }
    }
}