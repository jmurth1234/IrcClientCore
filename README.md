# IrcClientCore

A .NET Core IRC library, designed for clients.

[![Build Status](https://dev.azure.com/supermarioryan/supermarioryan/_apis/build/status/rymate1234.IrcClientCore?branchName=master)](https://dev.azure.com/supermarioryan/supermarioryan/_build/latest?definitionId=1&branchName=master)

## What is this?

This is a .NET Library that can be used as the basis of an irc client or other 
application utilising the irc protocol

It supports .NET Standard 1.4 and 2.0, so it runs on all versions of Windows 10
and modern versions of .NET Core.

All pushes to master are built by Azure Pipelines and uploaded to NuGet, 
currently in the form 1.0.xx-yyyymmdd.x-dev-branch. Stable releases will 
eventually be in the form yy.mm.dd or something, or possibly SemVer

## What's included in this repo?

This repo includes two projects, the library itself and a very basic Console
client that can be used for testing. 

## Building

Either open in your favourite C# IDE (VS / VS Code, Jetbrains Rider) or use the
standard .NET build tools: 

```
$ dotnet restore
$ dotnet run --project .\ConsoleIrcClient\
```

## Contributing

Contributions are welcome, as long as it adds useful functionality to the 
project, including:

 - adding commands
 - further support for the irc protocol (ideally targeting IRCv3)

Like WinIRC there's no real code guidelines, although I did run this project 
through the Rider code formatting at one point this isn't strictly followed

