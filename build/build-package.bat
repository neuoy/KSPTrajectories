@echo off

SET MODMANAGER_VERSION=2.7.1

rem make sure the initial working directory is the one containing the current script
SET scriptPath=%~dp0
SET initialWD=%CD%
cd %scriptPath%

cd ..

rd /s /q package
mkdir package
cd package
mkdir GameData
cd GameData

xcopy /y ..\..\3rdParty\ModuleManager.%MODMANAGER_VERSION%.dll .
xcopy /y ..\..\3rdParty\ModuleManager-License.txt .

mkdir Trajectories
cd Trajectories
xcopy /y ..\..\..\License.txt .
xcopy /y ..\..\..\Trajectories.cfg .
xcopy /y ..\..\..\Trajectories.version .

mkdir Plugin
xcopy /y ..\..\..\Plugin\bin\Release\Trajectories.dll Plugin

mkdir Textures
xcopy /y ..\..\..\Textures\icon.png Textures
xcopy /y ..\..\..\Textures\icon-blizzy1.png Textures

IF EXIST ..\..\..\Trajectories.zip del ..\..\..\Trajectories.zip
"%scriptPath%7z.exe" a ../../../Trajectories.zip ../../GameData
cd "%scriptPath%.."
rd /s /q package

cd %initialWD%

pause
