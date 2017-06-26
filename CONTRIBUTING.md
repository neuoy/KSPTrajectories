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

#### Building

If your reference paths are set up correctly, then building the project should be as simple as Clicking Build -> Build Solution.
If `KSPDIR` is set, then the output path will be the `\GameData\Trajectories\Plugin\` subdirectory of your KSP install. If not, you have to configure the output path yourself in Properties -> Build -> Output Path.

When you are building in Debug mode, one additional file with the ending `.mdb` is created. This file is required for unity debugging.

#### Debugging

To debug KSP, you have to enable the "Background Simulation" option inside the game, by going to KSP Main Menu -> Settings -> General -> Simulate In Background and setting it to **ON**.
It is recommended to debug KSP in a window rather than fullscreen, so turn off full screen by going to KSP Main Menu -> Settings -> Graphics and unchecking "Full screen".
To save startup time, seconds of our life and the environment, it is recommended to set the Graphics options way down. For that, go to KSP Main Menu -> Settings -> Graphics and set:

  - Render Quality: Fastest
  - Texture Quality: Eigth Res
  - Aerodynamic FX Quality: Minimal
  - Anti-Aliasing: Disabled
  - V-Sync: Don't sync
  - Frame-Limit: Whatever you're comfortable with (I use 60 FPS)
  - Pixel Light Count: 0
  - Shadow Cascades: 0

Before building Trajectories, consider turning on a few conditional compilation symbols, that may or may not aid you in development and debuggin:

  - `DEBUG_FASTSTART`: Turn on DebugFastStart module that quickloads you into the first Vessel of a save named "default" right after Game Start.
  - `DEBUG_TELEMETRY`: Turn on "Telemetry" (see [below](#telemetry))
  - `DEBUG_PROFILER`: Turn on Trajectories internal profiler (see [below](#profiling))

##### Visual Studio

For debugging, switch to the debug configuration and build the project. Then, you can start KSP in the regular way using the Debug executable.

You can also directly build and start the project with the "Start Without Debugging" hotkey Ctrl-F5.
The reason you should use Ctrl-F5 over F5 ("Start Debugging") is that in the latter case, Visual Studio attaches to the KSP process - *but in the wrong way*.
We need to attach the KSP process using the Visual Studio tools for Unity.

We can do this by Selecting "Debug -> Attach Unity Debugger" from the Visual Studio menu, and then selecting the *"WindowsPlayer"* process.
If the *"WindowsPlayer"* process doesn't show up in this menu, check that
  - Both KSP and Visual Studio are allowed to communicate through the local firewall
  - That you created or downloaded the PlayerConnectionConfigFile described [above](#kerbal-space-program-install)

You should now be able to create breakpoits, step through the execution and inspect variables.
If that doesn't happen (the debugger just doesn't halt where you want it to), make sure that the debugging symbols (.mdb and .pdb) are available in the GameData directory, along with the Trajectories.dll file.

Note that while you are halting at a breakpoint, the KSP will become unresponsive. If you try to open it while halted, Windows will suggest to kill it. This is not what you want when debugging ;)


##### MonoDevelop

For Monodevelop debugging you need the .mdb files and will have to attach to the KSP dev install debug executable, to do this start Monodevelop and then start your KSP dev install debug executable, now use Monodevelop's **Run Menu->Attach to Process** option to open the process attach window. *Unity Debugger* should be selected in the lower left selection box, now you can select KSP's process called *"WindowsPlayer"* and click OK to attach to it. Monodevelop should now switch into debugging mode.


#### Telemetry

Trajectories "core business" is the calculation of the trajectory itself.
Since this happens within a numeric simulation, this can be rather hard to debug: usually there are no compiler errors or crashes, and the only information you have is that the prediction is "wrong".

To aid debugging such "numeric" problems, the Telemetry module was created. It records certain specified numeric values in a Tab-Separated file that can be read by other tools such as Jupyter, MATLAB, R or even EXCEL.

Here are the steps on how to use the telemetry module:
  - Download the Telemetry.dll assembly from here: https://github.com/fat-lobyte/KSPTelemetry/releases
  - Place the assembly somewhere in the GameData folder of your KSP install
  - Enable the `DEBUG_TELEMETRY` compilation symbol inside the Trajectories project
  - Within the Trajectories source code, find or create an `Awake()` method of a KSPAddon class, and set up the data channel:

    `Telemetry.AddChannel<double>("yourvalue");`

  - Within your code, call this method to update your value

    `Telemetry.Send("yourvalue", the_actual_value);`

  - Start your debugging session. As soon as you are in the flight scene, a file called "Trajectories.csv" should appear in the same location where you put the Telemetry.dll file.
    This file will contain the values of "yourvalue" over the course of time.


#### Profiling

You can use the Unity Editor profiler by starting the Unity Editor, opening a blank project (or any project for that matter) and then use the **Window Menu->Profiler** option to open the Profiler Window. Now you can start your KSP dev install debug executable either standalone or with Visual Studio.

By default you will only see the MonoBehavior methods (Update, FixedUpdate, etc...) but you can add calls in your code to profile anything you like. To do this, add to your code pairs of `Profiler.BeginSample("MyLabel");` and `Profiler.EndSample();`. Be aware that if a frame takes too long to execute the profiler will skip it.

Here's an example applied inside the *Trajectories.MapOverlay.Render* method:
    ```
    setDisplayEnabled(true);
    Profiler.BeginSample("MapOverlay.Render_refreshMesh");
    refreshMesh();
    Profiler.EndSample();
    ```

In addition, there is a simple "frame-based" profiler included in the KSPTrajectories code base [here](https://github.com/neuoy/KSPTrajectories/tree/master/Plugin/Utility/Profiler.cs), that is appropriate for performance measurements.

For more information see the KSP Forum thread [KSP Plugin debugging and profiling for Visual Studio and Monodevelop on all OS](http://forum.kerbalspaceprogram.com/index.php?/topic/102909-ksp-plugin-debugging-and-profiling-for-visual-studio-and-monodevelop-on-all-os/&page=1).


## Building Releases

Making releases is as simple as updating two version files and the clicking build. Below is a small checklist for releases.

However, before creating a release, make sure that *you are actually authorized* to make one.
While the code is open source and you could theoretically do what you want, it would be very, very, **very** apreciated that you don't create new releases unless the current maintainer has either stepped back or has gone missing for a long time and doesn't reply to requests.

If you want to distribute your own version for testing, please do so by making it very clear to everyone that it's not an official release, and **CHANGE THE VERSION NUMBER** according to the versioning scheme below.


### Release Checklist

  - Complete the `CHANGELOG.md` file, and fill out the release date field. Make sure to credit all contributors.
  - Adjust the compatible KSP version numbers in `Trajectories.version`. Actually test if they work in all the KSP version claimed compatible.
  - Bump the version number in `Trajectories.version` and `Plugin\Properties\AssemblyInfo.cs` according the the versioning rules [below](#versioning).
  - Check with Git that your working directory is clean. No Changes are allowed, everything must be commited.
  - Build the Project in release mode
  - Clean out the `GameData` folder of your KSP install
  - Extract the `Trajectories-<version>.zip` that was created during the Release build into your GameData folder
  - Launch KSP in the non-Developer mode and verify the functionality. Check the `output_log.txt` for errors.
  - Create a Tag with Git, push all commits and the tag to GitHub.
  - On GitHub, draft a new release selecting the newly created tag
  - Upload the `Trajectories-<version>.zip` file to GitHub, paste the changelog there
  - On SpaceDock, draft a new release and upload the `Trajectories-<version>.zip`.
  - On the KSP forums, create a new post with the changelog and links to both GitHub and SpaceDock.

### Versioning

The rules for versioning are rather lax, except for the main most important #1 rule:

**DO NOT RELEASE DIFFERENT PRODUCTS UNDER THE SAME VERSION NUMBER**

While the rules below are guidelines and can be ignored rather arbitrarily by the maintainer, the rule above is THE LAW.
If you let a build slip out that differes from another build by as little as one bit but has the same version number, kittens will die and Krakens shall eat your ship.
Increment the version number even if the change is miniscule.

If the Version is MAJOR.MINOR.PATCH, then

  - MAJOR is the major version number, to be incremented when a major code restructuring and/or change in functionality has taken place.
  - MINOR is incremented when there are notable and visible changes and/or additions to functionality
  - PATCH is incremented for smaller and/or invisible changes

Even when creating bugfix releases with tiny changes, don't add another version number - increment the PATCH number instead.

<!--
##### Readme, Updated VS Project files and Overhauled Scripts by [PiezPiedPy](https://github.com/PiezPiedPy)
-->
