# KSPTrajectories, Visual Studio and Unity
### INTRODUCTION
A set of instructions and files to help set up KSPTrajectories for:
  - Building and debugging with Visual Studio and the UnityVS extension.
  - Unity Editor profiler and Monodevelop debugging.
  - A Trajectories.zip packager.
  
  Uses Visual Studio's Post Build Event script handling to run scripts to create *.mdb* files for UnityVS and Monodevelop debugging. For Release building, packages the *Trajectories.dll* and also any required files into a *Trajectories.zip* file.
#
### SETUP
For building and/or debugging KSPTrajectories with Visual Studio or Unity Editor you will need to download and install the exact version of Unity Editor that was used to build the version of KSP you are working with.
  - The Unity Editor for **KSP v1.3.0** is **Unity v5.4.0.p4** and can be downloaded here: [UnitySetup64-5.4.0p4.exe](http://beta.unity3d.com/download/b15b5ae035b7/Windows64EditorInstaller/UnitySetup64-5.4.0p4.exe)
  
  If you want to debug with Visual Studio then you will need the **UnityVS Extension** which you can download and install by using the **Tools->Extensions and Updates** window in Visual Studio.
  
  If you don't have a KSP dev install then make one by copying a clean install of KSP into a new folder and then install any needed mods into it. If you have copied from a steam install then you will want to copy the [steam_appid.txt](https://github.com/neuoy/KSPTrajectories/tree/master/misc/VisualStudio/steam_appid.txt) file into your KSP dev install folder.
  
  For debugging you will have to copy a couple of files, mainly the Unity debug player and its symbol file into your KSP development install.

  - First is to copy the *"player_win.exe"* and *"player_win_development_x64.pdb"* files into your KSP development install folder (copy them next to the KSP.exe file).  they can be found in your Unity install folder *"Unity\Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\win64_development_mono"*.
  - Rename the copied file *"player_win.exe"* now in your KSP dev install folder to *"KSP_x64_Dbg.exe"*.
  - Create a junction in your KSP dev install folder named *"KSP_x64_Dbg_Data"* linking to your KSP dev *"KSP_x64_Data"* folder.
    - You can create the junction by opening a command prompt in your KSP dev install folder and entering *"mklink /J KSP_x64_Dbg_Data KSP_x64_Data"*.
  - Download the [PlayerConnectionConfigFile](https://www.sarbian.com/sarbian/PlayerConnectionConfigFile) file and put it into your KSP dev *"KSP_x64_Data"* folder.
#### Visual Studio and UnityVS extension:
  
  You will first have to Open your Visual Studio solution file in your working repository and configure its properties page.
  - Open the *"Trajectories.sln"* file with Visual Studio and then open the **Properties** page and goto the **Build Events** tab.
  - Enter the code below into the **Post-build event command line** box.
  ```
if $(ConfigurationName)==Debug (call "$(ProjectDir)..\misc\VisualStudio\buildscripts\UnityDebug\prepare-debug.bat" $(TargetName))
if $(ConfigurationName)==Release (call "$(ProjectDir)..\misc\VisualStudio\buildscripts\ZipPack\build-package.bat" $(TargetName) -v@(VersionNumber))
  ```
  - Change to the **Reference Paths** tab and add a path to your KSP dev install *"\KSP_x64_Data\Managed"* folder.
  - Goto **Debug Menu->Options** and clear the **Enable Edit and Continue** checkbox.
#### Here we set the Visual Studio **Debug** Configuration:
  - Make sure the **Build** tab is selected and the **Configuration** is set to **Debug**.
  - Enter into the **Conditional compilation symbols** field *DEVELOPMENT;ENABLE_PROFILER*.
  - Set the **Define DEBUG constant** and **Define TRACE constant** checkboxes to checked.
  - Set the **Output path** field to point to the *"GameData\Trajectories\Plugin"* folder in your KSP dev install folder.
  - Click the **Advanced** button and set **Debug Info** to *full*.
  - Clear the **Check for arithmetic overflow/underflow** checkbox, close the **Advanced Build Settings** window.
  - Now you need to change to the **Debug** tab and set the **Start Action** to **Start external program** and enter your KSP dev install debug executable file *"KSP_x64_Dbg.exe"* as the start-up program.
  - Change the **Working directory** field to point to your KSP dev install folder.
  
#### Here we set the Visual Studio **Release** Configuration:
  - Make sure the **Build** tab is selected and the **Configuration** is set to **Release**.
  - Remove from the **Conditional compilation symbols** field *DEVELOPMENT;ENABLE_PROFILER*.
  - Clear the **Define DEBUG constant** and **Define TRACE constant** checkboxes.
  - Set the **Output path** field to point to the *"GameData\Trajectories\Plugin"* folder in your KSP dev install folder.
  - Click the **Advanced** button and set **Debug Info** to *none*, close the **Advanced Build Settings** window.
  - Now you need to change to the **Debug** tab and set the **Start Action** to **Start external program** and enter your KSP dev install executable file *"KSP_x64.exe"* as the start-up program.
  - Change the **Working directory** field to point to your KSP dev install folder.
#### Unity Editor setup:
  - Open Unity Editor (must be the correct version) and create a new blank project, you can name it what you like. Then close Unity Editor.
#
### USING
**Note:** Before you can attach to your KSP dev install executable you have to make sure that KSP's **Background Simulation** is *ON* by going to **KSP Main Menu->Settings->General->Simulate In Background** and setting it to *ON*.

**Debugging with Visual Studio and the UnityVS extension.**
  - Start a debugging session as you would with any other Visual Studio Project, this will then launch KSP in *Development Build* mode which will be displayed in the bottom right corner of KSP's window. I advise debugging KSP in a window rather than fullscreen.
  - You will now have to attach the KSP process called *"WindowsPlayer"* with the **Attach Unity Debugger** option in the **Debug Menu** to allow Visual Studio to have control of KSP for breakpoints and program stepping etc.

**Unity Editor profiler and Monodevelop debugging.**
  - You can use the Unity Editor profiler by starting the Unity Editor, opening a blank project (or any project for that matter) and then use the **Window Menu->Profiler** option to open the Profiler Window. Now you can start your KSP dev install debug executable either standalone or with Visual Studio.
    - By default you will only see the MonoBehavior methods (Update, FixedUpdate, etc...) but you can add calls in your code to profile anything you like. To do this, add to your code pairs of `Profiler.BeginSample("MyLabel");` and `Profiler.EndSample();`. Be aware that if a frame takes too long to execute the profiler will skip it.
    ##### Here's an example applied inside the *Trajectories.MapOverlay.Render* method:
    ```
    setDisplayEnabled(true);
    Profiler.BeginSample("MapOverlay.Render_refreshMesh");
    refreshMesh();
    Profiler.EndSample();
    ```
    **Note:** There is a simple 'frame-based' profiler included in the KSPTrajectories code base [here](https://github.com/neuoy/KSPTrajectories/tree/master/Plugin/Utility/Profiler.cs), that is appropriate for performance measurements.
    
  - For Monodevelop debugging you need the .mdb files and will have to attach to the KSP dev install debug executable, to do this start Monodevelop and then start your KSP dev install debug executable, now use Monodevelop's **Run Menu->Attach to Process** option to open the process attach window. *Unity Debugger* should be selected in the lower left selection box, now you can select KSP's process called *"WindowsPlayer"* and click OK to attach to it. Monodevelop should now switch into debugging mode.

**The Trajectories.zip packager.**
  - The Trajectories.zip file is created when you build the Release version with Visual Studio and will be found in your repository's *"KSPTrajectories"* folder. The Release build also copies the Trajectories.dll to your KSP dev install *"GameData\Trajectories\Plugin"* folder ready for testing.
#

For extra information see the KSP Forum thread [KSP Plugin debugging and profiling for Visual Studio and Monodevelop on all OS](http://forum.kerbalspaceprogram.com/index.php?/topic/102909-ksp-plugin-debugging-and-profiling-for-visual-studio-and-monodevelop-on-all-os/&page=1).

##### Readme, Updated VS Project files and Overhauled Scripts by [PiezPiedPy](https://github.com/PiezPiedPy)
