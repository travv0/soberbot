module Models

open Microsoft.EntityFrameworkCore
open System

type Ban() =
    [<DefaultValue>]
    val mutable ID: uint64

    [<DefaultValue>]
    val mutable ServerID: uint64

    [<DefaultValue>]
    val mutable UserID: uint64

    [<DefaultValue>]
    val mutable Message: string

type Config() =
    [<DefaultValue>]
    val mutable ID: uint64

    [<DefaultValue>]
    val mutable ServerID: uint64

    [<DefaultValue>]
    val mutable PruneDays: int

    [<DefaultValue>]
    val mutable MilestoneChannelID: uint64

type Milestone() =
    [<DefaultValue>]
    val mutable ID: uint64

    [<DefaultValue>]
    val mutable Days: int

    [<DefaultValue>]
    val mutable Name: string

type Sobriety() =
    [<DefaultValue>]
    val mutable ID: uint64

    [<DefaultValue>]
    val mutable UserID: uint64

    [<DefaultValue>]
    val mutable UserName: string

    [<DefaultValue>]
    val mutable ServerID: uint64

    [<DefaultValue>]
    val mutable SobrietyDate: DateTime

    [<DefaultValue>]
    val mutable ActiveDate: DateTime

    [<DefaultValue>]
    val mutable LastMilestoneDays: int

    [<DefaultValue>]
    val mutable MilestonesEnabled: bool

type SoberContext() =
    inherit DbContext()

    override _.OnConfiguring(optionsBuilder) =
        optionsBuilder.UseSqlite("Data Source=soberbot.db")
        |> ignore

    [<DefaultValue>]
    val mutable Sobrieties: DbSet<Sobriety>

    [<DefaultValue>]
    val mutable Config: DbSet<Config>

    [<DefaultValue>]
    val mutable Bans: DbSet<Ban>

    [<DefaultValue>]
    val mutable Milestones: DbSet<Milestone>
