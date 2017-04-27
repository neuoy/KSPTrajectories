rem Generate the zip Release package.
rem For information on how to setup your environment.
rem see https://github.com/neuoy/KSPTrajectories/tree/master/misc/VisualStudio/Readme.md

@echo off

rem get parameters that are passed by visual studio post build event
SET TargetName=%1
SET Dllversion=%2

rem make sure the initial working directory is the one containing the current script
SET scriptPath=%~dp0
SET rootPath=%scriptPath%..\..\..\..\
SET initialWD=%CD%

rem current module manager version in misc\3rdParty\
SET MODMANAGER_VERSION=2.7.5

echo Generating %TargetName% Release Package...
cd "%rootPath%"

IF EXIST package\ rd /s /q package
mkdir package
cd package

mkdir GameData
cd GameData
xcopy /y ..\..\3rdParty\ModuleManager.%MODMANAGER_VERSION%.dll .
xcopy /y ..\..\3rdParty\ModuleManager-License.txt .

mkdir %TargetName%
cd %TargetName%
xcopy /y ..\..\..\License.txt .
xcopy /y ..\..\..\%TargetName%.cfg .
xcopy /y ..\..\..\%TargetName%.version .
xcopy /y ..\..\..\README.md .

mkdir Plugin
xcopy /y %initialWD%\%TargetName%.dll Plugin

mkdir Textures
xcopy /y ..\..\..\Textures\icon.png Textures
xcopy /y ..\..\..\Textures\icon-blizzy1.png Textures

echo.
echo Compressing %TargetName% Release Package...
IF EXIST "%rootPath%%TargetName%*.zip" del "%rootPath%%TargetName%*.zip"
"%scriptPath%7za.exe" a "..\..\..\%TargetName%%Dllversion%.zip" ..\..\GameData

cd "%rootPath%"
rd /s /q package

cd "%initialWD%"
