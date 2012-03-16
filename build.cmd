@echo off
set path=%path%;C:\Windows\Microsoft.NET\Framework\v4.0.30319;

msbuild src\NanoMessageBus.sln /nologo /v:q /p:Configuration=Release /t:Clean
msbuild src\NanoMessageBus.sln /nologo /v:q /p:Configuration=Release

::ILMerge JSON.NET

::Create NuGet Packages