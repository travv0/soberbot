﻿// <auto-generated />
using System;
using DiscordBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DiscordBot.Migrations
{
    [DbContext(typeof(SoberContext))]
    [Migration("20191013184628_MilestonesToggle")]
    partial class MilestonesToggle
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity("DiscordBot.Models.Ban", b =>
                {
                    b.Property<ulong>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Message");

                    b.Property<ulong>("ServerID");

                    b.Property<ulong>("UserID");

                    b.HasKey("ID");

                    b.ToTable("Bans");
                });

            modelBuilder.Entity("DiscordBot.Models.Config", b =>
                {
                    b.Property<ulong>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("MilestoneChannelID");

                    b.Property<int>("PruneDays");

                    b.Property<ulong>("ServerID");

                    b.HasKey("ID");

                    b.ToTable("Config");
                });

            modelBuilder.Entity("DiscordBot.Models.Milestone", b =>
                {
                    b.Property<ulong>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Days");

                    b.Property<string>("Name");

                    b.HasKey("ID");

                    b.ToTable("Milestones");
                });

            modelBuilder.Entity("DiscordBot.Models.Sobriety", b =>
                {
                    b.Property<ulong>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("ActiveDate");

                    b.Property<int>("LastMilestoneDays");

                    b.Property<bool>("MilestonesEnabled");

                    b.Property<ulong>("ServerID");

                    b.Property<DateTime>("SobrietyDate");

                    b.Property<ulong>("UserID");

                    b.Property<string>("UserName");

                    b.HasKey("ID");

                    b.ToTable("Sobrieties");
                });
#pragma warning restore 612, 618
        }
    }
}