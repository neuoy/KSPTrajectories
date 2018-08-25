rem Generate the zip Release package.
rem For information on how to setup your environment.
rem see https://github.com/neuoy/KSPTrajectories/blob/master/CONTRIBUTING.md

@echo off

rem get parameters that are passed by visual studio post build event
SET TargetName=%1
SET Dllversion=%~n2
SET KSPversion=%3
SET Versioning=%~n3
SET Versioning=%Versioning:*KSP=%

rem make sure the initial working directory is the one containing the current script
SET scriptPath=%~dp0
SET rootPath=%scriptPath%..\..\
SET initialWD=%CD%

echo Generating %TargetName% for %KSPversion% Release Package...
cd "%rootPath%"

IF EXIST package\ rd /s /q package
mkdir package
cd package

mkdir GameData
cd GameData
xcopy /y /e "..\..\3rdParty\%Versioning%\*" .

mkdir "%TargetName%"
cd "%TargetName%"
xcopy /y ..\..\..\COPYING.md .
xcopy /y ..\..\..\LICENSE.md .
xcopy /y "..\..\..\%TargetName%.cfg" .
xcopy /y ..\..\..\CONTRIBUTING.md .
xcopy /y ..\..\..\README.md .
xcopy /y ..\..\..\CHANGELOG.md .

xcopy /y /e "..\..\..\buildscripts\Versioning\%Versioning%\*" .

mkdir Localization
xcopy /y ..\..\..\Localization\TrajectoriesLocalization.cfg Localization

mkdir Plugin
xcopy /y "%initialWD%\%TargetName%.dll" Plugin

mkdir Textures
xcopy /y ..\..\..\Textures\icon.png Textures
xcopy /y ..\..\..\Textures\iconActive.png Textures
xcopy /y ..\..\..\Textures\iconAuto.png Textures
xcopy /y ..\..\..\Textures\icon-blizzy.png Textures

echo.
echo Compressing %TargetName% for %KSPversion% Release Package...
IF EXIST "%rootPath%%TargetName%*_For_%KSPversion%.zip" del "%rootPath%%TargetName%*_For_%KSPversion%.zip"
"%scriptPath%7za.exe" a "..\..\..\%TargetName%%Dllversion%_For_%KSPversion%.zip" ..\..\GameData

cd "%rootPath%"
rd /s /q package

cd "%initialWD%"
