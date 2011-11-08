@ECHO OFF
SET FRAMEWORK_PATH=C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319
SET PATH=%PATH%;%FRAMEWORK_PATH%;

:target_config
SET TARGET_CONFIG=Release
IF x==%1x GOTO framework_version
SET TARGET_CONFIG=%1

:framework_version
SET FRAMEWORK_VERSION=v4.0
SET ILMERGE_VERSION=v4,%FRAMEWORK_PATH%

:build
if exist publish ( rmdir /s /q publish )
mkdir publish

echo Compiling / Target: %FRAMEWORK_VERSION% / Config: %TARGET_CONFIG%
msbuild /nologo /verbosity:quiet src\NanoMessageBus.sln /p:Configuration=%TARGET_CONFIG% /t:Clean
msbuild /nologo /verbosity:quiet src\NanoMessageBus.sln /p:Configuration=%TARGET_CONFIG% /property:TargetFrameworkVersion=%FRAMEWORK_VERSION%

echo Merging
mkdir publish\bin

echo Marging Primary Assembly
SET FILES_TO_MERGE=
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\NanoMessageBus.Core\bin\%TARGET_CONFIG%\NanoMessageBus*.dll"
(echo.|set /p =NanoMessageBus.*)>exclude.txt
bin\ILMerge\ILMerge.exe /keyfile:src/NanoMessageBus.snk /internalize:"exclude.txt" /xmldocs /wildcards /targetplatform:%ILMERGE_VERSION% /out:publish/bin/NanoMessageBus.dll %FILES_TO_MERGE%
del exclude.txt

echo Cleaning
msbuild /nologo /verbosity:quiet src/NanoMessageBus.sln /p:Configuration=%TARGET_CONFIG% /t:Clean

echo Done