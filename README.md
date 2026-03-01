# IrcClientCore

A .NET Core IRC library, designed for clients.

[![CI](https://github.com/rymate1234/IrcClientCore/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/rymate1234/IrcClientCore/actions/workflows/ci.yml)

## What is this?

This is a .NET Library that can be used as the basis of an irc client or other 
application utilising the irc protocol

It supports .NET Standard 1.4 and 2.0, so it runs on all versions of Windows 10
and modern versions of .NET Core.

## CI/CD

This repository uses GitHub Actions (`.github/workflows/ci.yml`) for CI/CD.

- Triggered on push to `master`, pull requests targeting `master`, and manual runs.
- Builds and tests with .NET SDK 5.0 in a Linux container (`mcr.microsoft.com/dotnet/sdk:5.0`).
- Publishes TRX test results as:
  - a downloadable workflow artifact (`test-results`)
  - a parsed test report in GitHub Checks (including failed-test annotations)
- Publishes NuGet packages only for direct pushes to `master` (never on pull requests).

### NuGet publish setup

Set the repository secret `NUGET_API_KEY` in GitHub before enabling package publish.

## What's included in this repo?

This repo includes two projects, the library itself and a very basic Console
client that can be used for testing. 

## Building

Either open in your favourite C# IDE (VS / VS Code, Jetbrains Rider) or use the
standard .NET build tools: 

```
$ dotnet restore
$ dotnet build IrcClientCore.sln
$ dotnet test IrcClientCore.Tests/IrcClientCore.Tests.csproj
$ dotnet run --project ConsoleIrcClient
```

## Contributing

Contributions are welcome, as long as it adds useful functionality to the 
project, including:

 - adding commands
 - further support for the irc protocol (ideally targeting IRCv3)

Like WinIRC there's no real code guidelines, although I did run this project 
through the Rider code formatting at one point this isn't strictly followed
