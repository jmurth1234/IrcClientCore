# IrcClientCore

Currently still mirrored from my GitLab

## What is this?

This is a .NET Library that can be used as the basis of an irc client or other 
application utilising the irc protocol

It supports .NET Standard 1.4 and 2.0, so it runs on all versions of Windows 10
and modern versions of .NET Core

## What's included in this repo?

This repo includes two projects, the library itself and a very basic Console
client that can be used for testing. 

## Building

Either open in your favourite C# IDE (VS / VS Code, Jetbrains Rider) or use the
standard .NET build tools: 

`$ dotnet restore`

`$ dotnet run --project .\ConsoleIrcClient\`

## Contributing

Whilst the repository is still mirrored, free to open a PR, I'll figure out a way 
to merge it to the GitLab instance.

Once I sort out another CI platform all development will be on GitHub

Like WinIRC there's no real code guidelines, although I did run this project 
through the Rider code formatting at one point this isn't strictly followed
