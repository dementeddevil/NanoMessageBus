@echo off
set path=%path%;C:/Windows/Microsoft.NET/Framework/v4.0.30319;
set EnableNuGetPackageRestore=true

echo Building project...
msbuild src/NanoMessageBus.sln /nologo /v:q /p:Configuration=Release /t:Clean
msbuild src/NanoMessageBus.sln /nologo /v:q /p:Configuration=Release /clp:ErrorsOnly

echo Merging assemblies...
if exist "src\proj\NanoMessageBus.JsonSerializer\bin\Release\merged" rmdir /s /q "src\proj\NanoMessageBus.JsonSerializer\bin\Release\merged"
mkdir src\proj\NanoMessageBus.JsonSerializer\bin\Release\merged
bin\ilmerge\ILMerge.exe /keyfile:src\NanoMessageBus.snk /internalize /wildcards /target:library ^
 /targetplatform:"v4,C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0" ^
 /out:"src\proj\NanoMessageBus.JsonSerializer\bin\Release\merged\NanoMessageBus.JsonSerializer.dll" ^
 "src/proj/NanoMessageBus.JsonSerializer/bin/Release/NanoMessageBus.JsonSerializer.dll" ^
 "src/proj/NanoMessageBus.JsonSerializer/bin/Release/Newtonsoft.Json.dll"
 
echo Creating NuGet packages...
for /r %%i in (src\packages\NanoMessageBus*.nuspec) do src\.nuget\nuget.exe pack %%i -symbols

echo Done.