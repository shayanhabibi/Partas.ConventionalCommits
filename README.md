# Partas.ConventionalCommits

[Conventional Commit] parser using FSharps XParsec. This implies potential compatability with Fable, and improved speed and resource usage over Regex designs.

Made with consumability from Dotnet in mind.

## Install

```bash
dotnet add package Partas.Tools.ConventionalCommits
```

```bash
paket install Partas.Tools.ConventionalCommits
```

> [IMPORTANT!]
> The namespace is Partas.ConventionalCommits.
> 
> The nuget package name is historical.

## Tests

Tests check for correctness according [to the spec][Conventional Commit]. Ambiguity in the spec has left some ambiguity that I describe below:

> [NOTE!]
> Conventional Commit spec does not put any requirement for the content of a footer value, other than it HAS to follow a footer key by a valid delimiter: ": "  or " #". You can technically EOF right after, or just provide no value, and that is still a footer.

[Conventional Commit]: https://www.conventionalcommits.org/en/v1.0.0/#specification
