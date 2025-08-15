module Partas.ConventionalCommits.FSharp

open XParsec


module ConventionalCommit =
    /// <summary>
    /// This will return an error whenever it fails to parse a conventional commit.
    /// </summary>
    let parseOrError input =
        Parsers.parseString input
        |> function
            | Ok { Parsed = commit } -> commit |> Ok
            | Error error ->
                ErrorFormatting.formatStringError input error
                |> Error

    let parse input =
        if isNull input then Unconventional ""
        else
        Parsers.parseString input
        |> function
            | Ok { Parsed = commit } -> commit
            | Error _ ->
                Unconventional input

module Debug =
    /// <summary>
    /// In debug, this will print an error whenever it fails to parse a conventional commit. No change
    /// when run in release mode.
    /// </summary>
    let parse input =
        if isNull input then Unconventional ""
        else
        Parsers.parseString input
        |> function
            | Ok { Parsed = commit } -> commit
            | Error error ->
                #if DEBUG
                let previous = System.Console.ForegroundColor
                System.Console.ForegroundColor <- System.ConsoleColor.Yellow
                "Failed to parse commit:"
                |> System.Console.WriteLine
                System.Console.ResetColor()
                ErrorFormatting.formatStringError input error
                |> System.Console.WriteLine
                System.Console.ForegroundColor <- previous
                #endif
                Unconventional input
