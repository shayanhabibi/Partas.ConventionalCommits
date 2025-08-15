namespace Partas.ConventionalCommits

open System
open System.Collections.Immutable
open XParsec
open XParsec.CharParsers
open XParsec.Parsers


module internal Parsers =
    let makeFooter struct (key, value) =
        match key with
        | "BREAKING-CHANGE" | "BREAKING CHANGE" ->
            Footer.BreakingChange(value)
        | _ -> Footer.Footer(key,value)
    
    /// <summary>
    /// Skips white string: spaces, new lines, returns etc
    /// </summary>
    let ws = skipMany (anyOf [ ' '; '\n'; '\r'; '\t' ])
    
    /// Parses a footer key
    let pFooterKey =
        let breakingChange =
            pstring "BREAKING-CHANGE" <|> pstring "BREAKING CHANGE"
        newline
        >>. (breakingChange .>> setUserState true) <|>
        (many1Chars2 asciiLetter (asciiLetter <|> digit <|> pchar '-') |>> _.ToLower())
        .>> (
            let followedByImpl c = satisfyL ((=) c) "Footers keys must be followed by \": \" or \" #\" "
            let followedBy (c1,c2) = followedByImpl c1 >>. followedByImpl c2
            followedBy (':', ' ') <|> followedBy (' ', '#')
            )
    
    /// Parses a footer value
    let pFooterValue =
        ws
        >>. manyCharsTill anyChar (lookAhead pFooterKey <|> (eof >>% ""))
        |>> fst

    /// Parses a footer section
    let pFooters =
        newline
        >>. many (tuple2 pFooterKey pFooterValue |>> makeFooter)
        <|> (eof >>% [||].ToImmutableArray())
            
    /// Prases '!' Breaking indication in type
    let pBreakingType =
        (pstring "!: " >>. setUserState true) <|> (pstring ": " >>% ())
    
    /// Parses optional scope
    let pScope =
        let success =
            pitem '(' >>. many1Chars asciiLetter .>> pitem ')' |>> _.ToLower() |>> ValueSome
        let failure =
            lookAhead (pitem ':' <|> pitem '!') >>% ValueNone
        success <|> failure
    
    /// Parses the type
    let pType = many1Chars asciiLetter |>> _.ToLower()
    
    /// Implements the type and scope parser
    let pTypeAndScope =
        tuple3 pType pScope pBreakingType
    
    /// Parses the description/subject
    let pDescription =
        let contentParser = noneOf ['\n'; '\r']
        let endCondition =
            (skipNewline >>. (skipNewline <|> eof) <|> eof)
        manyCharsTill contentParser endCondition |>> fst
    
    /// Parses the body/content
    let pBody =
        let contentParser = anyChar
        let endCondition =
            (lookAhead (skipNewline >>. pFooterKey) <|> (eof >>% Unchecked.defaultof<_>))
        manyCharsTill contentParser endCondition |>> fst
    let parseString input =
        let reader = Reader.ofString input false
        reader |> parser {
            let! struct (typ,scope,_) = pTypeAndScope
            let! description = ws >>. pDescription
            let! body = pBody
            let! footers = pFooters
            let! isBreaking = getUserState
            return {
                Type = typ
                Scope = scope
                Subject = description.TrimEnd()
                Message =
                    if String.IsNullOrEmpty body
                    then ValueNone
                    else ValueSome <| body.Trim()
                Footers = footers |> Seq.toList
            }
            |> if isBreaking then ParsedCommit.Breaking else ParsedCommit.Conventional
        }

[<AutoOpen>]
module AutoOpenParser =
    type ConventionalCommit with
        static member Parse(input: string) =
            Parsers.parseString input
            |> function
                | Ok { Parsed = commit } -> commit
                | Error _ -> ParsedCommit.Unconventional input
        static member ParseOrFail(input: string) =
            Parsers.parseString input
            |> function
                | Ok { Parsed = commit } ->
                    commit
                | Error error -> raise (ConventionalCommitException(ErrorFormatting.formatStringError input error))
