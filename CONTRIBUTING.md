# How to Contribute

## Where to Start

Coming soon. For now, please ask on the [Forum thread](http://forum.kerbalspaceprogram.com/index.php?/topic/94370-13) :smile:

## Code Style

Coming soon.

## Using Git and GitHub

Coming soon.


## Debugging with Visual Studio and Unity

This section will guide you through setting up your development environment so that it's suitable for the Development of KSP mods.

### Installation and Environment Setup

#### Unity
For building and/or debugging KSPTrajectories with Visual Studio or Unity Editor you will need to download and install the exact version of Unity Editor that was used to build the version of KSP you are working with.
You can find out which  Unity version your current KSP install is using by looking at the first line of `KSP_Data/output_log.txt` (or `KSP_x64_Data/output_log.txt`). It should read something like this:

    Initialize engine version: 5.4.0p4 (b15b5ae035b7)

In this case, the Unity version for your KSP version is 5.4.0p4.

The Unity Editor for **KSP v1.3.0** is **Unity v5.4.0.p4** and can be downloaded here: [UnitySetup64-5.4.0p4.exe](http://beta.unity3d.com/download/b15b5ae035b7/Windows64EditorInstaller/UnitySetup64-5.4.0p4.exe)

#### Visual Studio

It is recommended to use **Visual Studio 2017**. Any version should work (including the free *Community* version).
To save on disk space and installation time, you should only select the "Game development with Unity" component. In the right hand side, uncheck the "Unity 5.6-Editor" component, since this is the wrong version of the Editor anyway.
 

##### Visual Studio Tools for Unity

If you want to debug with Visual Studio then you will need the **Visual Studio Tools for Unity** Extension.
If you selected the "Game development with Unity" component above, this should already be installed.
If it is not, you download and install by using the **Tools->Extensions and Updates** window in Visual Studio, selecting the "Online" tab on the left hand column and then searching for "Unity" in the search bar in the upper right corner.

##### Editor Settings (only for Visual Studio 2015 and earlier)

Coming soon.

#### Kerbal Space Program Install

You should create a KSP install just for Development that is separate from your install that you use for gaming.

To do that, you follow these steps:

  - Copy your game install to another location
  - Remove everything but the `Squad` directory from `GameData`
  - Find your Unity install, and go into the subdirectory `Unity\Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\win64_development_mono`.
    Copy the file `player_win.exe` into your KSP main directory
  - Delete or rename `KSP_x64.exe` in your KSP main directory
  - Rename the `player_win.exe` to `KSP_x64.exe`
  - Download the [PlayerConnectionConfigFile](https://www.sarbian.com/sarbian/PlayerConnectionConfigFile) file and put it into your KSP dev `KSP_x64_Data` folder.

This will turn your KSP install into a Development version only. If you want to use this install as a regular (non-Development) install as well, then instead of deleting or renaming `KSP_x64.exe`, you can do the following:

  - Rename the copied file `player_win.exe` now in your KSP dev install folder to `KSP_x64_Dbg.exe`.
  - Create a junction in your KSP dev install folder named `KSP_x64_Dbg_Data` linking to your KSP dev `KSP_x64_Data` folder.
    This is done by opening a command prompt in your KSP dev install folder and running the following command:

        mklink /J KSP_x64_Dbg_Data KSP_x64_Data

Now you can choose between the Development version (launch `KSP_x64_Dbg.exe`) and the regular non-Development version (run `KSP_x64_Dbg.exe`).

#### System Environment Variables

To make your life a little easier, the Trajectories Visual Studio Project respects an environment variable called `KSPDIR`.
If you set its value to the path of your KSP development install, the reference and debugging paths inside the project should be set automatically.
If it is not set, your reference paths and the Debugging paths have to be set manually.

To set the variable, follow the instructions in this link, before starting a Visual Studio instance:

https://superuser.com/a/949577


### Development and Debugging

#### Project Setup

Before you can build Trajectories, your Visual Studio has to know where the Unity and KSP assemblies are that it references.
If you set your `KSPDIR` variable as mentioned [above](#system-environment-variables), then this should already be set. If not, then please:

  - Double-Click the "Properties" page in the Solution Explorer in Visual Studio
  - Change to the **Reference Paths** tab and select the `\KSP_x64_Data\Managed` subdirectory of your KSP dev install
  - Click "Add" to actually add the selected path
 
To be able to quicklaunch KSP using F5 (or Ctrl-F5), you have to set which external program should start. This should already be set if you set your `KSPDIR` environment variable. If not,
  
- Double-Click the "Properties" page in the Solution Explorer in Visual Studio
  - Change to the **Debug** tab, select "Start External Program" and select the KSP executable that you want to start.
  - In the Working Directory, select the KSP root directory

### Using
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


## Building Releases

**The Trajectories.zip packager.**
  - The Trajectories.zip file is created when you build the Release version with Visual Studio and will be found in your repository's *"KSPTrajectories"* folder. The Release build also copies the Trajectories.dll to your KSP dev install *"GameData\Trajectories\Plugin"* folder ready for testing.


For extra information see the KSP Forum thread [KSP Plugin debugging and profiling for Visual Studio and Monodevelop on all OS](http://forum.kerbalspaceprogram.com/index.php?/topic/102909-ksp-plugin-debugging-and-profiling-for-visual-studio-and-monodevelop-on-all-os/&page=1).

##### Readme, Updated VS Project files and Overhauled Scripts by [PiezPiedPy](https://github.com/PiezPiedPy)
