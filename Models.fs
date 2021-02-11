module Models

open Microsoft.EntityFrameworkCore
open System
open System.ComponentModel.DataAnnotations

type Ban() =
    [<DefaultValue>]
    val mutable id: uint64

    member this.ID
        with get () = this.id
        and set id = this.id <- id

    [<DefaultValue>]
    val mutable ServerID: uint64

    [<DefaultValue>]
    val mutable UserID: uint64

    [<DefaultValue>]
    val mutable Message: string

type Config() =
    [<DefaultValue>]
    val mutable id: uint64

    member this.ID
        with get () = this.id
        and set id = this.id <- id

    [<DefaultValue>]
    val mutable ServerID: uint64

    [<DefaultValue>]
    val mutable PruneDays: int

    [<DefaultValue>]
    val mutable MilestoneChannelID: uint64

type Milestone() =
    [<DefaultValue>]
    val mutable id: uint64

    member this.ID
        with get () = this.id
        and set id = this.id <- id

    [<DefaultValue>]
    val mutable Days: int

    [<DefaultValue>]
    val mutable Name: string

[<CLIMutable>]
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

    override __.OnConfiguring(optionsBuilder) =
        optionsBuilder.UseSqlite("Data Source=soberbot.db")
        |> ignore

    [<DefaultValue>]
    val mutable sobrieties: DbSet<Sobriety>

    member this.Sobrieties
        with get () = this.sobrieties
        and set s = this.sobrieties <- s

    [<DefaultValue>]
    val mutable config: DbSet<Config>

    member this.Config
        with get () = this.config
        and set c = this.config <- c

    [<DefaultValue>]
    val mutable bans: DbSet<Ban>

    member this.Bans
        with get () = this.bans
        and set b = this.bans <- b

    [<DefaultValue>]
    val mutable milestones: DbSet<Milestone>

    member this.Milestones
        with get () = this.milestones
        and set m = this.milestones <- m
