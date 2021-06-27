rem Generate the zip Release package.
rem For information on how to setup your environment.
rem see https://github.com/neuoy/KSPTrajectories/blob/master/CONTRIBUTING.md

@echo off

rem get parameters that are passed by visual studio post build event
SET TargetName=%1
SET Dllversion=%~n2

rem make sure the initial working directory is the one containing the current script
SET scriptPath=%~dp0
SET rootPath=%scriptPath%..\..\
SET initialWD=%CD%

echo Generating %TargetName% Release Package...
cd /d "%rootPath%"
rem copy dll from build directory to GameData
xcopy /y "%initialWD%\%TargetName%.dll" "GameData\%TargetName%\Plugin\*" > nul

IF EXIST package\ rd /s /q package
mkdir package
cd /d package

mkdir GameData
cd /d GameData

mkdir "%TargetName%"
cd /d "%TargetName%"
xcopy /y /e "%rootPath%\GameData\%TargetName%\*" .
xcopy /y "%rootPath%\CHANGELOG.md" .
xcopy /y "%rootPath%\LICENSE.md" .
xcopy /y "%rootPath%\COPYRIGHTS.md" .
xcopy /y "%rootPath%\CONTRIBUTING.md" .
xcopy /y "%rootPath%\README.md" .

echo.
echo Compressing %TargetName% Release Package...
IF EXIST "%rootPath%%TargetName%*.zip" del "%rootPath%%TargetName%*.zip"
"%scriptPath%7za.exe" a "..\..\..\%TargetName%%Dllversion%.zip" ..\..\..\package\GameData

rem check dll file exists
cd /d "%rootPath%"
IF NOT EXIST "package\GameData\%TargetName%\Plugin\%TargetName%*.dll" echo **WARNING** %TargetName% dll is missing

rem remove temp files
rd /s /q package

cd /d "%initialWD%"
