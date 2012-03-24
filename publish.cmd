@echo off
call build.cmd
for /r %%i in (NanoMessageBus*.?.?.??.nupkg) do src\.nuget\nuget.exe Push %%i