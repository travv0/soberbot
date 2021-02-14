module Models

open Microsoft.EntityFrameworkCore
open System

[<CLIMutable>]
type Ban =
    { ID: uint64
      ServerID: uint64
      UserID: uint64
      Message: string }

[<CLIMutable>]
type Config =
    { ID: uint64
      ServerID: uint64
      PruneDays: int
      MilestoneChannelID: uint64 }

[<CLIMutable>]
type Milestone = { ID: uint64; Days: int; Name: string }

[<CLIMutable>]
type Sobriety =
    { ID: uint64
      UserID: uint64
      UserName: string
      ServerID: uint64
      SobrietyDate: DateTime
      ActiveDate: DateTime
      LastMilestoneDays: int
      MilestonesEnabled: bool
      Type: string }

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
