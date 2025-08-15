namespace Partas.ConventionalCommits

open System
open System.Runtime.CompilerServices

type IFooter =
    abstract member Key: string
    abstract member Value: string

type ConventionalCommitException(commit: string) =
    inherit Exception($"This is not a conventional commit:\n{commit}")

type IConventionalCommit =
    abstract member Type: string
    abstract member Scope: string voption
    abstract member Subject: string
    abstract member Message: string voption
    abstract member Footers: IFooter seq

type ICommit =
    inherit IConventionalCommit
    abstract member IsConventional: bool
    abstract member IsBreaking: bool
    
module Spec =
    let [<Literal>] BreakingChangeKey = "BREAKING-CHANGE"

type Footer =
    | Footer of key: string * value: string
    | BreakingChange of value: string
    interface IFooter with
        member this.Key =
            match this with
            | Footer(key,_) -> key
            | BreakingChange _ -> Spec.BreakingChangeKey
        member this.Value =
            match this with
            | Footer(_,value) | BreakingChange value -> value

type ConventionalCommit =
    {
        Type: string
        Scope: string voption
        Subject: string
        Message: string voption
        Footers: Footer list
    }
    interface IConventionalCommit with
        member this.Type = this.Type
        member this.Scope = this.Scope
        member this.Subject = this.Subject
        member this.Message = this.Message
        member this.Footers = unbox<IFooter seq> this.Footers

type ParsedCommit =
    | Conventional of ConventionalCommit
    | Breaking of ConventionalCommit
    | Unconventional of string
    member this.IsConventionalCommit = this.IsUnconventional |> not
    member this.ToConventionalCommit =
        match this with
        | Conventional commit | Breaking commit -> commit
        | Unconventional value -> raise (ConventionalCommitException(value))
    /// <summary>
    /// Returns <c>ValueNone</c> on failure for FSharp.
    /// </summary>
    member this.TryToConventionalCommit =
        match this with
        | Conventional commit | Breaking commit -> ValueSome commit
        | Unconventional _ -> ValueNone
    interface ICommit with
        member this.IsConventional = this.IsUnconventional |> not
        member this.IsBreaking = this.IsBreaking
        member this.Footers = unbox<IFooter seq> this.ToConventionalCommit.Footers
        member this.Message = this.ToConventionalCommit.Message
        member this.Scope = this.ToConventionalCommit.Scope
        member this.Subject = this.ToConventionalCommit.Subject
        member this.Type = this.ToConventionalCommit.Type

[<AutoOpen; Extension>]
type ConventionalCommitExtensions =
    [<Extension>]
    static member GetFooterValue(this: #IFooter seq, key: string) =
        this
        |> Seq.find (_.Key >> (=) key)
        |> _.Value
    [<Extension>]
    static member TryGetFooterValue(this: #IFooter seq, key: string) =
        this
        |> Seq.tryFind (_.Key >> (=) key)
        |> Option.map _.Value
    [<Extension>]
    static member ContainsFooter(this: #IFooter seq, key: string) =
        this |> Seq.exists (_.Key >> (=) key)
    [<Extension>]
    static member GetFooterValueOrDefault(this: #IFooter seq, key: string, ?defaultValue: string) =
        let defaultValue = defaultArg defaultValue Unchecked.defaultof<string>
        this.TryGetFooterValue key |> Option.defaultValue defaultValue
    
    [<Extension>]
    static member GetFooterValue(this: #IConventionalCommit, key: string) = this.Footers.GetFooterValue key
    [<Extension>]
    static member TryGetFooterValue(this: IConventionalCommit, key: string) =
        this.Footers.TryGetFooterValue key
    [<Extension>]
    static member ContainsFooter(this: #IConventionalCommit, key: string) = this.Footers.ContainsFooter key
    [<Extension>]
    static member GetFooterValueOrDefault(this: #IConventionalCommit, key: string, ?defaultValue: string) =
        this.Footers.GetFooterValueOrDefault(key, ?defaultValue = defaultValue)
