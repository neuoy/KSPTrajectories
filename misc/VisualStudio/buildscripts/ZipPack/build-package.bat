rem Generate the zip Release package.
rem For information on how to setup your environment.
rem see https://github.com/neuoy/KSPTrajectories/tree/master/misc/VisualStudio/Readme.md

@echo off

rem get parameters that are passed by visual studio post build event
SET TargetName=%1
SET Dllversion=%~n2

rem make sure the initial working directory is the one containing the current script
SET scriptPath=%~dp0
SET rootPath=%scriptPath%..\..\..\..\
SET initialWD=%CD%

rem current module manager version in misc\3rdParty\
SET MODMANAGER_VERSION=3.0.6

echo Generating %TargetName% Release Package...
cd "%rootPath%"

IF EXIST package\ rd /s /q package
mkdir package
cd package

mkdir GameData
cd GameData
xcopy /y ..\..\3rdParty\ModuleManager.%MODMANAGER_VERSION%.dll .
xcopy /y ..\..\3rdParty\README.md .

mkdir %TargetName%
cd %TargetName%
xcopy /y ..\..\..\COPYING.md .
xcopy /y ..\..\..\LICENSE.md .
xcopy /y ..\..\..\%TargetName%.cfg .
xcopy /y ..\..\..\%TargetName%.version .
xcopy /y ..\..\..\README.md .
xcopy /y ..\..\..\CHANGELOG.md .

mkdir Localization
xcopy /y ..\..\..\Localization\TrajectoriesLocalization.cfg Localization

mkdir Plugin
xcopy /y %initialWD%\%TargetName%.dll Plugin

mkdir Textures
xcopy /y ..\..\..\Textures\icon.png Textures
xcopy /y ..\..\..\Textures\iconActive.png Textures
xcopy /y ..\..\..\Textures\iconAuto.png Textures
xcopy /y ..\..\..\Textures\icon-blizzy.png Textures

echo.
echo Compressing %TargetName% Release Package...
IF EXIST "%rootPath%%TargetName%*.zip" del "%rootPath%%TargetName%*.zip"
"%scriptPath%7za.exe" a "..\..\..\%TargetName%%Dllversion%.zip" ..\..\GameData

cd "%rootPath%"
rd /s /q package

cd "%initialWD%"
