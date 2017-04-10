@echo off

rem make sure the initial working directory is the one containing the current script
SET scriptPath=%~dp0

rem generate the MDB file needed by Monodevelop and UnityVS for debugging
rem see http://forum.kerbalspaceprogram.com/index.php?/topic/102909-ksp-plugin-debugging-for-visual-studio-and-monodevelop-on-all-os/&page=1 for information on how to setup your debugging environment
echo "Trajectories.dll -> Trajectories.dll.mdb"
"%scriptPath%\tools\pdb2mdb\pdb2mdb.exe" Trajectories.dll

