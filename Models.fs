module Models

open Microsoft.EntityFrameworkCore
open System

type Ban =
    { ID: uint64
      ServerID: uint64
      UserID: uint64
      Message: string }

type Config =
    { ID: uint64
      ServerID: uint64
      PruneDays: int
      MilestoneChannelID: uint64 }

type Milestone = { ID: uint64; Days: int; Name: string }

type Sobriety =
    { ID: uint64
      UserID: uint64
      UserName: string
      ServerID: uint64
      SobrietyDate: DateTime
      ActiveDate: DateTime
      LastMilestoneDays: int
      MilestonesEnabled: bool }

type SoberContext() =
    inherit DbContext()

    override _.OnConfiguring(optionsBuilder) =
        optionsBuilder.UseSqlite("Data Source=soberbot.db")
        |> ignore

    member val public Sobrieties: DbSet<Sobriety> = null with get, set
    member val public Config: DbSet<Config> = null with get, set
    member val public Bans: DbSet<Ban> = null with get, set
    member val public Milestones: DbSet<Milestone> = null with get, set
