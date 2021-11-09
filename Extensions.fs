module Extensions

open Discord
open Discord.Commands
open System.Runtime.CompilerServices

type ModuleBase<'T when 'T: not struct and 'T :> ICommandContext> with
    [<Extension>]
    member this.Reply(?message, ?isTTS, ?embed: Embed, ?options: RequestOptions) =
        this
            .Context
            .Channel
            .SendMessageAsync(
                Option.toObj message,
                defaultArg isTTS false,
                Option.toObj embed,
                Option.toObj options
            )
            .Result
        |> ignore
