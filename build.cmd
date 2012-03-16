@echo off
set path=%path%;C:/Windows/Microsoft.NET/Framework/v4.0.30319;

echo Building project...
msbuild src/NanoMessageBus.sln /nologo /v:q /p:Configuration=Release /t:Clean
msbuild src/NanoMessageBus.sln /nologo /v:q /p:Configuration=Release /clp:ErrorsOnly

echo Merging assemblies...
del src\proj\NanoMessageBus.JsonSerializer\bin\Release\NanoMessageBus.*
del src\proj\NanoMessageBus.JsonSerializer\bin\Release\*.xml
if exist "src\proj\NanoMessageBus.JsonSerializer\bin\Release\merged" rmdir /s /q "src\proj\NanoMessageBus.JsonSerializer\bin\Release\merged"
mkdir src\proj\NanoMessageBus.JsonSerializer\bin\Release\merged
bin\ilmerge\ILMerge.exe /keyfile:src\NanoMessageBus.snk /internalize /wildcards /target:library ^
 /targetplatform:"v4,C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319" ^
 /out:"src\proj\NanoMessageBus.JsonSerializer\bin\Release\merged\NanoMessageBus.JsonSerializer.dll" ^
 "src/proj/NanoMessageBus.JsonSerializer/bin/Release/*.dll"
 
echo Creating NuGet packages...

echo Done.