@echo off
call build.cmd
for /r %%i in (*.nupkg) do src\.nuget\nuget.exe Push %%i