@echo off
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
mkdir Trajectories
cd Trajectories

mkdir Plugin
xcopy /y ..\..\..\Plugin\bin\Release\Trajectories.dll Plugin

mkdir Textures
xcopy /y ..\..\..\Textures\icon.png Textures

"%scriptPath%7z.exe" a ../../../Trajectories.zip ../../GameData
cd "%scriptPath%.."
rd /s /q package

cd %initialWD%

pause
