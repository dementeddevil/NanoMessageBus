echo off
:: run this when 2.0.49 is obsolete
src\.nuget\NuGet.exe delete NanoMessageBus 2.0.49 -NonInteractive
src\.nuget\NuGet.exe delete NanoMessageBus.RabbitMQ 2.0.49 -NonInteractive
src\.nuget\NuGet.exe delete NanoMessageBus.Autofac 2.0.49 -NonInteractive
src\.nuget\NuGet.exe delete NanoMessageBus.Json.NET 2.0.49 -NonInteractive
src\.nuget\NuGet.exe delete NanoMessageBus.Log4Net 2.0.49 -NonInteractive
src\.nuget\NuGet.exe delete NanoMessageBus.NLog 2.0.49 -NonInteractive
