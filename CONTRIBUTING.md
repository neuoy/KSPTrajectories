Are you here because you want to:

 * Report a Bug?  Click [here](#how-to-report-bugs).
 * Request a feature? Click [here](#how-to-suggest-features).
 * Jump in and contribute? Click [here](#how-to-contribute).

Table of Contents
=================

   1. [How to Report Bugs](#how-to-report-bugs)
      1. [Removing Mods for Debugging](#removing-mods-for-debugging)
   1. [How to Suggest Features](#how-to-suggest-features)
      1. [Stay Polite!](#stay-polite)
      1. [Consider the workload](#consider-the-workload)
      1. [Gather feedback from the community](#gather-feedback-from-the-community)
         1. [Make sure that the feature is wanted](#make-sure-that-the-feature-is-wanted)
      1. [Consider the Impact](#consider-the-impact)
         1. ["Why can't you just add a toggle?"](#why-cant-you-just-add-a-toggle)
      1. [What not to suggest](#what-not-to-suggest)
      1. [Where to suggest a feature](#where-to-suggest-a-feature)
      1. [Learn to accept a No](#learn-to-accept-a-no)
   1. [How to Contribute](#how-to-contribute)
      1. [Where to Start](#where-to-start)
      1. [Using Git and GitHub](#using-git-and-github)
         1. [Workflow for the Trajectories mod](#workflow-for-the-trajectories-mod)
         1. [How to submit pull requests](#how-to-submit-pull-requests)
      1. [Code Style](#code-style)
      1. [Debugging with Visual Studio and Unity](#debugging-with-visual-studio-and-unity)
         1. [Installation and Environment Setup](#installation-and-environment-setup)
            1. [Unity](#unity)
            1. [Visual Studio](#visual-studio)
               1. [Visual Studio Tools for Unity](#visual-studio-tools-for-unity)
               1. [Editor Settings](#editor-settings)
            1. [Kerbal Space Program Install](#kerbal-space-program-install)
            1. [System Environment Variables](#system-environment-variables)
         1. [Development and Debugging](#development-and-debugging)
            1. [Project Setup](#project-setup)
            1. [Building](#building)
            1. [Debugging](#debugging)
               1. [Visual Studio](#visual-studio-1)
               1. [MonoDevelop](#monodevelop)
            1. [Telemetry](#telemetry)
            1. [Profiling](#profiling)
               1. [Unity Profiler](#unity-profiler)
               1. [Internal Profiler](#internal-profiler)
      1. [Development on MacOS](#development-on-macos)
      1. [Building Releases](#building-releases)
         1. [Release Checklist](#release-checklist)
         1. [Versioning](#versioning)

# How to Report Bugs

Bug reports are a very valuable contribution to the development of this mod - but only if done right.

"Trajectories doesn't work" is not a good bug report, because it sure works well for me. If it doesn't work for you, it is your responsibility to make clear which conditions lead to stuff going wrong.
Good bug reports are effort and require time - but putting in that effort helps immensely to not waste developer time and get the bug fixed quickly.

All bugs should be reported on GitHub, under this URL: https://github.com/neuoy/KSPTrajectories/issues

You are free to mention any issues on the forum thread, but in the end, all issues must be registered on GitHub. This is because we use GitHub as a workflow management tool and the issues list as a checklist, and all the information and discussions for one specific issue are in one place as opposed to getting buried under posts in the forums.

*All* bug reports *must* contain the following information:

 - Which version of trajectories are you using? If in doubt, check out the `Trajectories.version` file in `GameData\Trajectories`
 - Which version of KSP are you using? Which language are you using (check out the `buildID.txt` or `buildID64.txt`)? Are you using the 64-Bit version (`KSP_x64.exe`) or the 32-Bit version (`KSP.exe`)?
 - Which mods are you running, and which versions do they have? Please note that when reporting a bug, you should only ever have the minimal amount of mods installed that are necessary to reproduce the bug (see [below](removing-mods-for-debugging) for tips on removing mods).
 - Which aerodynamic model are you using? FAR? Stock with cubes? Stock spherical? (The default is cubes)
 - The `output_log.txt` file, from the `KSP_x64_Data`or `KSP_Data` directory. You can either zip it and directly attach it to the bug report or upload to a pastebin service like https://gist.github.com/

If you encounter problems with specific vessels, please attach the craft file and short instructions on how to use it. Remove parts from Mods so we can load them, unless the parts from Mods are crucial for reproducing the problem.

It is recommended to attach Quicksave files that describe the situation. Again, make sure that we can load them and remove all non-Stock crafts (unless they are crucial for the reproduction).

Make clear which conditions have led to the bug: what exactly did you do? What makes the bug go away (workaround), what makes it appear? Some experimentation on your part is very appreciated, so that we can find the problem more easily.

When dealing with precision issues, some there are some additional considerations:
 
 - Try turning off the cache in the settings. This will kill performance, but might increase precision.
 - Are your settings correct? Did you set it to prograde/retrograde or set a descent profile? Do you follow your set profile accurately during flight?
 - How big is the deviation and in which direction does it go? "Set target to impact" under the "Target" tab is very useful for this: set the target to impact before entering the atmosphere, then fly your entry and let the impact happen. Include the distance (and direction) to target in your bug report
 - Craft files are mandatory except for the simplest test crafts. If you have a simple test craft, post a screenshot.
 - Describe our entry situation, as detailed as possible (Body, periapsis, apoapsis, orientation). Preferrably, add a quicksave that leads to imprecise predictions.

More on reporting bugs for KSP mods can be found in this forum topic: http://forum.kerbalspaceprogram.com/index.php?/topic/83212


## Removing Mods for Debugging

When reporting issues for any mod, your first action should be to cull the list of your mods to the minimal amount of mods where the issue still occurs. Most of the time, this means there will be **no** mods except Trajectories.

The very first thing you should do is to **back up your save file**!!! We am not responsible for lost save files. To do this, go into the "saves" folder, right-click on your save with the right name, click Send To -> Zip Compressed folder. Don't do anything before you did that.

Next, create a backup of your GameData folder (Excluding "Squad"), and delete all mods in there except for Trajectories and ModuleManager (and Squad obviously).

Create a new Savegame, build a craft with Stock parts that triggers the problem and launch it. Don't be afraid to use the Cheat menu to get the craft into the situation that you need (Set orbit, rendezvous, infinite fuel...). In the animal kingdom, Alpacas of the "dirty cheating" variety are the most successful when it comes to debugging.

Most of the time the bug will occur with *only* trajectories installed.

Sometimes, you will require other mods to reproduce it, such as FAR or RSS. In that case, try to delete all mods that are uninvolved and state clearly which mods (and which versions of them) are required.

If you are sure that the problem doesn't happen with only Trajectories but don't know which is responsible, you have to search for it.
The simplest method is to add mods one by one. If you have few installed, you should do that.

However, "binary search" is more effective: Add half your mods and see if the problem occurs. If not, add the other half. If yes, split the first half again and add the first quarter. If that doesn't cause the problem, remove it again and add the second quarter.
Keep halving the number of mods that you are testing until you arrived at one single mod. This might sound complicated, but if you have a big number of mods, you will find the culprit a lot faster than by linear search.

# How to Suggest Features

While we value all input and suggestions for improvement, a few things should be considered to make sure that your suggestion is understood
and to increase the chances of adoption.

## Stay Polite!

Never forget that all contributors and maintainers are doing this Mod for **fun**, and do **not get paid**. If you want a feature, it is
in *your* best interest to remain on good terms with the contributors and keep them motivated to work on the project.

Therefor, it is also in your best interest to not demand features, to not pressure us to implement a feature and not to insult any previous work - 
that includes not  crapping on the current state of affairs, no matter how frustrated you might be.

In German there is a saying:

> Der Ton macht die Musik

Which is literally translated to "the tone makes the music" and means that the tone of conversation is paramount to reach a good result.
A demanding or bitter tone is a sure way to shut down even the best intentioned request. Stay polite and keep your tone positive.

## Consider the workload

As mentioned, none of us get paid to work on this project. Please consider this, when you try to suggest features that take a lot of work.
Even the best idea in the world might not be implemented, simply because there is nobody around with the time to implement it.

This shouldn't prevent you from making it in the first place, but try to asses how much work a feature would be, state that in your suggestion, and don't be too disappointed if it's doesn't get implemented.

What would be best, of course, is to implement the feature yourself and submit a pull request! We have a guide for new contributors [right here](#how-to-contribute).


## Gather feedback from the community

When you are requesting feature, even when it is for "personal use", it can still be good to bring in the community.
Suggest the feature on the Forums, ask for Feedback from other users, create an issue on GitHub. Be responsive, and accept feedback and suggestions to make your suggestion even better ;)


### Make sure that the feature is wanted

An important part of gathering feedback is: **is your feature even necessary**? Sometimes when you think that you can't live without a certain feature or change,
it is good to ask the community: is there a way to achieve the desired result with current tools?

Ask other users if they also believe that this feature is necessary, and gather support. When many users claim that they want something, the chances of adoption increase
(but there's still no guarantee!).


## Consider the Impact

Whenever a new feature is introduced, no matter how big the benefits, there are some drawbacks as well. This includes (but is not limited to) the following:

  - Performance: every calculation takes time, needs a processor to execute it and consumes energy. Consider that your feature will potentially run on many users machines, for multiple hours, for around 30 times per second.
    You don't want to be the guy who's responsible for melting our planets ice caps, are you? ;)
    Sometimes this is negligible. Sometimes, it is not. Think about which it is when you suggest a feature.
  - Changing workloads: People are creatures of habit, and so are this mods users. When you introduce a change in "the way they do things",
    be aware that many people (who your are currently unaware of, because they are content and happy) will suddenly have to change their behavior and that they
	might not be as happy about the change as you are.

When you submit a feature request, please make sure to think about the drawbacks that your feature could have and specify and explain them clearly.

### "Why can't you just add a toggle?"

If your feature request would introduce a change that makes the Mod do something differently, you might be tempted to add toggles and switches
to revert back to the old behavior, or when your feature is rejected because of the impact it has, you might exclaim "why can't you just add a toggle?".
No, generally we can't. Here is why:

While it might make perfect sense from an individual point of view,
where you consider one single feature and one single switch, changing the perspective to that of the maintainer makes things more complicated.

A maintainer has to consider the *entirety* of all switches, options and toggles. What is "just one more switch" for you might be the twenty first switch to the maintainer.
While there is undoubtedly value in a "configurable" product, it also incurs a cost!

  - It's harder to learn how the mod works, harder to write manuals and therefor produces more support requests.
  - Adding switches requires an increased amount of testing. If we had a testing matrix, adding switches increases the workload quadratically! (But we don't, so that point is a bit moot.)
  - Adding switches increases the complexity of code, adds more corner situations, more potential for bugs and makes debugging harder (not least because it's yet another thing that users have to specify)
  - A program full of toggle buttons, half of which are in the same state for most users for most of the time anyway is plain ugly.


Last but not least, we have to consider the overall "usability" of the mod, the "feel" of the mod and the "user stories". When adding a feature, we should not take
the cheap and easy way out ("just add a toggle"), but think about how people use the mod and how to make their lives better. Sometimes it might be better to change
the user experience completely or even remove features to avoid confusion.

## What not to suggest

Some features have been suggested many times, but cannot be included for technical reasons.
Below is a list of these features and the reasons why they will probably never make it into Trajectories.

If you disagree with this assessment, the best way to convince me otherwise is to provide a working implementation that fits well with the rest of the mod.

  - Predicting the trajectory after future staging events. We need the DragCubeList of a vessel to correctly calculate the drag it will produce. Currently, we get this DragCubeList from KSP as the one that is returned for the current vessel in the now. There is no way to create a "vessel after staging" or retrieve the "DragCubeList after staging". That means that we would have to duplicate a good deal of the KSP-internal staging and DragCube code, which is just not a good idea.


## Where to suggest a feature

Please suggest features by posting both in the [development Forum thread](http://forum.kerbalspaceprogram.com/index.php?/topic/94370-13) (so that users can also participate in the discussion) and create a GitHub issue on [our tracker](https://github.com/neuoy/KSPTrajectories/issues).

Make sure to explain the purpose of this feature, the advantages and the potential [impact](#consider-the-impact) on the mod.

Please try to also think of a way *how* to implement the feature in a way that makes sense both from the users and the developers perspective.



## Learn to accept a No

Sometimes, requests cannot be met - either because they don't match the Mods core idea or because their [impact](#consider-the-impact) is too high or simply because the contributors don't have enough time.

Consider forking the project and implementing the feature into your private builds, and playing with it in your private game. With more experience and testing and even "finished" code,
your case for adoption into the main mod is a lot stronger! Even if that doesn't work, you'll have the feature for yourself ;)

If after all has been said and discussed and weighed the community or the maintainers have come to the conclusion that your suggestion doesn't fit the bill,
please accept that and move on. Do not start to repeat yourself - this only serves to poison the discussion.

This is not a reason to be discouraged, your suggestion is still considered valuable!

# How to Contribute

Are you a pro and know all the things already? Here's are the links to the info specific to Trajectories:

 - [Workflow for the Trajectories mod](#workflow-for-the-trajectories-mod)
 - [How to submit pull requests](#how-to-submit-pull-requests)
 - [Code Style](#code-style)

## Where to Start

If you like using the Trajectories mod, please consider contributing!

There are many ways to contribute to the mod:

  - Write documentation! We could use a good manual, better code comments, an API documentation inside the code.
  - Report bugs - but do it well. Well-written bug reports are very appreciated. Make sure to follow the guidelines [above](#how-to-report-bugs).
  - Help testing, reproducing and narrowing down existing bugs
  - Help with code-cleanup, make the code more readable, reduce inefficiencies (but make sure you don't break anything!)
  - Write new features! We have a few much-requested features on our issue tracker that you could implement. Or, just solve your pet problem!

Especially before writing new features, communication is paramount! Please visit our
[development thread](http://forum.kerbalspaceprogram.com/index.php?/topic/94370-13), let other people know what you are working on,
ask for Feedback, talk about the mod. This is crucial to avoid merge conflicts, duplicated or unnecessary work.

## Using Git and GitHub

Git is a distributed version control system used by a myriad of projects, including big players like the Linux Kernel, Android or even Windows!
While Git in its simplest form is "only" a version control system, it also enables collaboration in a way that no other version control system ever could.

To aid with this collaboration GitHub was created. GitHub is first and foremost a hoster for Git repositories. But it also includes tools for bug tracking,
documentation, and even some light project management.

If you want to contribute to the Trajectories Mod, it is highly recommended to familiarize yourself with Git and GitHub. Luckily, there are many good guides out there.
GitHub itself provides a good entry point for Git and GitHub novices:

 * https://guides.github.com/activities/hello-world/
 * https://help.github.com/articles/set-up-git/
 * https://guides.github.com/activities/forking/

More information to specific question can be found here:

 * https://help.github.com/

If you want to get into the depths of Git, this (online) book is recommended:

 * https://git-scm.com/book/

*Note*: Git itself is a command line tool and has many many commands and options.
This can be a bit daunting, so tools were created to help with that.
You can use [GitHub Desktop](https://desktop.github.com/) or the GitHub web interface to do many tasks that can be done on the command line.

Personally, I find that these tools don't offer the fine-grained control that I came to expect from the command line tools.
In the future, you should consider learning the intricacies of Git in its entirety. It'll probably give you bonus points in your next interview ;)

### Workflow for the Trajectories mod

To start contributing, you have to first set up your Trajectories repository by forking it.

Before doing any work, you should create and switch to a branch. The name of the branch should reflect the "topic" of your work (these are often called "topic branches").
If you want to work on multiple areas/topics at the same time, it is better to create multiple branches! Don't just mush all of your work into one single branch.

Commit your work early and often! It's better to have too many commits than to lose work.

  * For Git pro users: Before making a pull request, please Squash your commits into more-or-less consistent, self-contained chunks that can be reverted if necessary.
  * For Git novices: Please do **not** squash your commits! The risk of losing your work is too high, so please just make your pull request and we'll take it from there.

When you are finished with your work, it is possible that work on the master branch has moved on. To simplify the merge process, you should make sure that your branch is merge-able.
Try merging your topic branch into the most up-to-date master branch (from the main repository, not from your fork!).
Make sure that you actually build and test the mod, don't just assume that "it's fine".

If you get merge conflicts, please [resolve them](https://help.github.com/articles/addressing-merge-conflicts/) before submitting a pull request. If you are having trouble with that, speak up!
Other contributers can help you with the merge process, and it's no shame especially since resolving merge conflicts is tricky.

  * For Git pro users: Please rebase your pull request branches on the latest master branch.
  * For Git novices: Please do **not** rebase anything! The risk of losing your work is too high, so please just make your pull request and we'll take it from there.

### How to submit pull requests

A pull request is a request from a contributor (you) to the maintainer (me) for inclusion of the contributors changes into the master branch of the main repository.
While your time spent on contributions is valuable and much appreciated, please consider that the time spent by the maintainer on reviewing, testing and merging your changes is also valuable.

To ensure smooth sailing with pull requests, please follow these guidelines:

  - Communicate early on what you are working. Let other people know what you are up to, explain what you want to do and why. Gather Feedback!
    This is crucial to avoid merge conflicts, duplicated or unnecessary work. The mod [development thread](http://forum.kerbalspaceprogram.com/index.php?/topic/94370-13) is a good place for that.
  - Don't submit your master branch, only submit named feature branches!
  - Keep the code compilable and working! 
  - Do sufficient testing to make sure that no new bugs are introduced. Since we neither have a QA department nor a test suite, testing will be manual.
    It is recommended to use your version of the Mod in your main KSP save for a while, before submitting a pull request.
  - Be considerate of our users KSP installs! We are most likely not gonna be the only mod in KSP's memory space, so make sure your changes don't deteriorate performance, use too much memory and don't cause crashes!
  - In your pull request, please explain your changes, why they are necessary and how they make Trajectories better.
  - If you are adding a feature, please provide a little section of text that could go into a manual (if we had one).
  - Please follow the [code style guidelines](#code-style)
  - Please submit readable, high-quality code! 

Once you followed the guidelines above, please submit your pull request to the **[main repository](https://github.com/neuoy/KSPTrajectories/pulls)**.
Please select "allow edits from maintainers" so that the maintainer can help you with your pull request.

Adding commits after you submitted the pull request is not forbidden - after all, if you gotta change something, you gotta change something.
However, please make sure that your pull request is complete and high-quality *before* submitting it. If you find an oversight after submitting it,
please make abundantly clear what you changed after you add commits.


## Code Style

Please observe some basic style rules for your contributions:

  - Follow the naming guidelines [put out by Microsoft](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines)
  - Per indentation level, use 4 spaces and no tabs
  - Use Carriage-Return & Line Feed (CRLF) line endings in all C# and Visual Studio source code files.
    Source files or config files for tools that do not work on Windows and/or can't handle CRLF line endings are exempt.
  - Avoid trailing whitespace
  - Please document your code briefly with inline-comments. If you add new functions, provide summaries that explain parameters, return values and side effects.


## Debugging with Visual Studio and Unity

This section will guide you through setting up your development environment so that it's suitable for the Development of KSP mods.

### Installation and Environment Setup

#### Unity
For building and/or debugging KSP Kerbalism with Visual Studio or Unity Editor you will need to download and install the exact version of Unity Editor that was used to build the version of KSP you are working with.
For KSP1.4+ You can find out which Unity version your current KSP install is using by looking at the first line of `C:\Users\YOURUSERNAME\AppData\LocalLow\Squad\Kerbal Space Program/output_log.txt`. It should read something like this:

    Initialize engine version: 2017.1.3p1 (02d73f71d3bd)

In this case, the Unity version for your KSP version is 2017.1.3p1.

And for KSP1.3.1 you can find out which Unity version your current KSP install is using by looking at the first line of `KSP_Data/output_log.txt` (or `KSP_x64_Data/output_log.txt`). It should read something like this:

    Initialize engine version: 5.4.0p4 (b15b5ae035b7)

In this case, the Unity version for your KSP version is 5.4.0p4.


The Unity Editor from **KSP v1.8.0 to latest** is **Unity v2019.2.2f1** and can be downloaded here: [UnityDownloadAssistant-2019.2.2f1.exe](https://download.unity3d.com/download_unity/ab112815d860/UnityDownloadAssistant-2019.2.2f1.exe )

The Unity Editor for **KSP v1.4.2 to 1.7.3** is **Unity v2017.1.3p1** and can be downloaded here: [UnityDownloadAssistant-2017.1.3p1.exe](https://beta.unity3d.com/download/02d73f71d3bd/UnityDownloadAssistant-2017.1.3p1.exe )

The Unity Editor for **KSP v1.3.1** is **Unity v5.4.0.p4** and can be downloaded here: [UnityDownloadAssistant-5.4.0p4.exe](https://beta.unity3d.com/download/b15b5ae035b7/UnityDownloadAssistant-5.4.0p4.exe )

#### Visual Studio

It is recommended to use **Visual Studio 2019**. Any version should work (including the free *Community* version).
To save on disk space and installation time, you should only select the "Game development with Unity" component. In the right hand side, uncheck the "Unity 5.6-Editor" component, since this is the wrong version of the Editor anyway.
 

##### Visual Studio Tools for Unity

If you want to debug with Visual Studio then you will need the **Visual Studio Tools for Unity** Extension.

If you selected the "Game development with Unity" component above, this should already be installed.
If it is not, you download and install by using the **Tools->Extensions and Updates** window in Visual Studio, selecting the "Online" tab on the left hand column and then searching for "Unity" in the search bar in the upper right corner.

##### Editor Settings

Please install the [Trailing Whitespace Visualizer Plugin](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.TrailingWhitespaceVisualizer)
and make sure that you don't add trailing whitespace.

The Trajectories repository contains a file called `.editorconfig` which should configure your editor automatically if you use Visual Studio 2017 and up.
If you use Visual Studio 2015 or older, please set the following options in Visual Studio:

Under Tools -> Options -> Text Editor -> C# -> Tabs:
  - Indenting: Smart
  - Tab Size: 4
  - Indent Size: 4
  - Insert Spaces


#### Kerbal Space Program Install

You should create a KSP install just for Development that is separate from your install that you use for gaming.

##### For KSP Versions from KSP 1.8+ :
  - Copy your game install to another location, this location will now be your KSP dev directory and the location the following instructions are to be performed on.
  - Remove everything but the `Squad` directory from the `GameData` directory.
    - NB: Keep `SquadExpansion` if you're using or testing with the official Squad Expansion Packs.
  - Find your Unity install, and go into the subdirectory `Unity\Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\win64_development_mono`.  
    Copy the following files into the root of your KSP dev directory:  
      - `UnityPlayer.dll`
      - `WinPixEventRuntime.dll`
  - Edit `<KSP Directory>\KSP_x64_Data\boot.config` and add the following line:
    `player-connection-debug=1`

This will turn your new KSP install into a Development version only. If you want to use this install as a regular (non-Development) install as well (which is recommmended), then instead of overwriting `UnityPlayer.dll`, rename it to `UnityPlayer_release.dll` and copy the new one to `UnityPlayer_debug.dll`.

To switch between debug and release mode you need to copy or rename the relevant dll before launching `KSP_x64.exe`. A simple batch script can help to facilitate this.

##### For KSP Versions before KSP 1.8.x :
To do that, you follow these steps:

  - Copy your game install to another location, this location will now be your KSP dev directory and the location the following instructions are to be performed on.
  - Remove everything but the `Squad` directory from `GameData`
  - Find your Unity install, and go into the subdirectory `Unity\Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\win64_development_mono`.  
    Copy the file `player_win.exe` into the root of your KSP dev directory.
  - Delete or rename `KSP_x64.exe` in the root directory.
  - Rename the `player_win.exe` file to `KSP_x64.exe`
  - Copy the `PlayerConnectionConfigFile` file from the `buildscripts\UnityDebug` directory and put it into the `KSP_x64_Data` directory.

This will turn your new KSP install into a Development version only. If you want to use this install as a regular (non-Development) install as well (which is recommmended), then instead of deleting or renaming `KSP_x64.exe`, you can do the following:

  - Dont delete or rename `KSP_x64.exe` in the root directory, use this to start the non development version of KSP.
  - Rename the copied unity file `player_win.exe` to `KSP_x64_Dbg.exe`, use this to start the development version of KSP.
  - Create a junction named `KSP_x64_Dbg_Data` linking to the `KSP_x64_Data` directory.
    This is done by opening a command prompt in the root directory and running the following command:

        mklink /J KSP_x64_Dbg_Data KSP_x64_Data

Now you can choose between the Development version (launch `KSP_x64_Dbg.exe`) and the regular non-Development version (run `KSP_x64.exe`).

#### System Environment Variables

To make your life a little easier, the Trajectories Visual Studio Project respects three environment variables called `KSPBACKPORTDIR`, `KSPBACKPORTDIR17` and `KSPDEVDIR`
If you set their values to the paths of the relevant versions of your KSP development installs, the reference and debugging paths inside the project should be set automatically. `KSPDEVDIR` should point to an install of the latest KSP version currently released, `KSPBACKPORTDIR17` to your KSP1.7.2 install  and `KSPBACKPORTDIR` to your KSP1.3.1 install. 
If it is not set, your reference paths and the Debugging paths have to be set manually.
Please note that you don't need to use more than one install if you plan on making private builds but if you plan on making pull requests then it will be appreciated if you make sure your code works for not only the latest released version but also versions 1.3.1 and 1.7.2. You can also use the compiler directives `KSP13` and `KSP17` in the source to switch relevant code specific to a KSP version.

To set the variable, follow the instructions in this link, before starting a Visual Studio instance:

https://superuser.com/a/949577


### Development and Debugging

#### Project Setup

Before you can build Trajectories, your Visual Studio has to know where the Unity and KSP assemblies are that it references.
If you set your `KSPDEVDIR`, `KSPBACKPORTDIR17` and `KSPBACKPORTDIR` variables as mentioned [above](#system-environment-variables), then this should already be set. If not, then please:

  - Double-Click the "Properties" page in the Solution Explorer in Visual Studio
  - Change to the **Reference Paths** tab and select the `\KSP_x64_Data\Managed` subdirectory of your KSP dev install
  - Click "Add" to actually add the selected path
 
To be able to quicklaunch KSP using F5 (or Ctrl-F5), you have to set which external program should start. This should already be set if you set your environment variables. If not,
  
 - Double-Click the "Properties" page in the Solution Explorer in Visual Studio
 - Change to the **Debug** tab, select "Start External Program" and select the KSP executable that you want to start.
 - In the Working Directory, select the KSP root directory

#### Building

If your reference paths are set up correctly, then building the project should be as simple as Clicking Build -> Build Solution.
If your env variables are set, then the output path will be the `\GameData\Trajectories\Plugin\` subdirectory of the relative KSP install. If not, you have to configure the output path yourself in Properties -> Build -> Output Path.

You can use the configuration menu to switch between `Debug` and `Release` for KSP1.8.x up, `Debug 1.7` and `Release 1.7` for KSP1.4.0 to 1.7.2 and also `Debug 1.3` and `Release 1.3` for KSP1.3.1

When building in debug mode, an additional file is created containing the debug symbols, which is necessary for Unity debugging.
  - For KSP 1.8 and later, this file has the ending `.pdb`.
  - For earlier KSP versions, this file has the ending `.mdb`.

#### Debugging

To debug KSP, you have to enable the "Background Simulation" option inside the game, by going to KSP Main Menu -> Settings -> General -> Simulate In Background and setting it to **ON**.
It is recommended to debug KSP in a window rather than fullscreen, so turn off full screen by going to KSP Main Menu -> Settings -> Graphics and unchecking "Full screen".
To save startup time, seconds of our life and the environment, it is recommended to set the Graphics options way down. For that, go to KSP Main Menu -> Settings -> Graphics and set:

  - Render Quality: Fastest
  - Texture Quality: Eighth Res
  - Aerodynamic FX Quality: Minimal
  - Anti-Aliasing: Disabled
  - V-Sync: Don't sync
  - Frame-Limit: Whatever you're comfortable with (I use 60 FPS)
  - Pixel Light Count: 0
  - Shadow Cascades: 0

Before building Trajectories, consider turning on a few conditional compilation symbols, that may or may not aid you in development and debugging:

  - `DEBUG_FASTSTART`: Turn on DebugFastStart module that quickloads you into the first Vessel of a save named "default" right after Game Start (currently does not work).
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

You should now be able to create breakpoints, step through the execution and inspect variables.
If that doesn't happen (the debugger just doesn't halt where you want it to), make sure that the debugging symbols (`.mdb` and/or `.pdb`) are available in the `GameData` directory, along with the `Trajectories.dll` file.

Note that while you are halting at a breakpoint, the KSP will become unresponsive. If you try to open it while halted, Windows will suggest to kill it. This is not what you want when debugging ;)


##### MonoDevelop

For Monodevelop debugging you need the .mdb files and will have to attach to the KSP dev install debug executable, to do this start Monodevelop and then start your KSP dev install debug executable, now use Monodevelop's **Run Menu->Attach to Process** option to open the process attach window. *Unity Debugger* should be selected in the lower left selection box, now you can select KSP's process called *"WindowsPlayer"* and click OK to attach to it. Monodevelop should now switch into debugging mode.


#### Telemetry

Trajectories "core business" is the calculation of the trajectory itself.
Since this happens within a numeric simulation, this can be rather hard to debug: usually there are no compiler errors or crashes, and the only information you have is that the prediction is "wrong".

To aid debugging such "numeric" problems, the Telemetry module was created. It records certain specified numeric values in a Tab-Separated file that can be read by other tools such as Jupyter, MATLAB, R or even EXCEL.

Here are the steps on how to use the telemetry module:
  - Download the `Telemetry.dll` assembly from here: https://github.com/fat-lobyte/KSPTelemetry/releases
  - Place the assembly somewhere in the `GameData` folder of your KSP install
  - Enable the `DEBUG_TELEMETRY` compilation symbol inside the Trajectories project
  - Within the Trajectories source code, find or create an `Awake()` method of a KSPAddon class, and set up the data channel:

    `Telemetry.AddChannel<double>("yourvalue");`

  - Within your code, call this method to update your value

    `Telemetry.Send("yourvalue", the_actual_value);`

  - Start your debugging session. As soon as you are in the flight scene, a file called "Trajectories.csv" should appear in the same location where you put the `Telemetry.dll` file.
    This file will contain the values of "yourvalue" over the course of time.

  - The Internal Profiler and Watcher (see below) can also output to the telemetry module, use by enabling the `PROFILER_TELEMETRY` and/or `WATCHER_TELEMETRY` compilation symbols.


#### Internal Watcher

There is a simple variables watcher included in the KSPTrajectories code base [here](https://github.com/neuoy/KSPTrajectories/tree/master/Plugin/Utilities/Watcher.cs), that is handy for displaying runtime values in a Gui.

Use by enabling the `DEBUG_WATCHER` compilation symbol.

In the code, place the variable you want to watch in a `Trajectories.Watcher.Watch("MyLabel", MyVariable)` method call, variables may have to be cast to a double, as of yet only doubles can be watched.

In-game, start it by pressing Ctrl-W. It shows each watch entry belonging to one label as one line. The columns show:

  * NAME: The Label for the variable being watched.
  * LAST: The value held for the variable being watched in the last executed frame.

You can reset these watches to zero with the Reset button.

The Internal Watcher can also output to the telemetry module, use by enabling the `WATCHER_TELEMETRY` compilation symbol.
  Note: You will have to re-load the scene usually twice before the Telemetry module receives a list of the entries and creates a valid Telemetry.csv file.


#### Profiling


##### Unity Profiler

You can use the Unity Editor profiler by starting the Unity Editor, opening a blank project (or any project for that matter) and then use the **Window Menu->Profiler** option to open the Profiler Window. Now you can start your KSP dev install debug executable either standalone or with Visual Studio.

By default you will only see the MonoBehavior methods (Update, FixedUpdate, etc...) but you can add calls in your code to profile anything you like. To do this, add to your code pairs of `Profiler.BeginSample("MyLabel");` and `Profiler.EndSample();`. Be aware that if a frame takes too long to execute the profiler will skip it.

Here's an example applied inside the *Trajectories.MapOverlay.Render* method:
    ```
    setDisplayEnabled(true);
    Profiler.BeginSample("MapOverlay.Render_refreshMesh");
    refreshMesh();
    Profiler.EndSample();
    ```


For more information see the KSP Forum thread [KSP Plugin debugging and profiling for Visual Studio and Monodevelop on all OS](http://forum.kerbalspaceprogram.com/index.php?/topic/102909-ksp-plugin-debugging-and-profiling-for-visual-studio-and-monodevelop-on-all-os/&page=1).


##### Internal Profiler

In addition, there is a simple "frame-based" profiler included in the KSPTrajectories code base [here](https://github.com/neuoy/KSPTrajectories/tree/master/Plugin/Utilities/Profiler.cs), that is appropriate for performance measurements.

Use by enabling the `DEBUG_PROFILER` compilation symbol.

In the code, wrap the code you want in pairs of `Trajectories.Profiler.Start("MyLabel")` and `Trajectories.Profiler.Stop("MyLabel")`.

In-game, start it by pressing Ctrl-P. It shows each code entry belonging to one label as one line. The columns show:

  * NAME: The Label for the code being profiled.
  * LAST: Code runtime for a call in the last executed frame.
  * AVG: Code runtime averaged over all the calls since start or reset.
  * CALLS: Number of calls in the last executed frame.
  * AVG: Number of calls divided by the number of frames since start or reset.

You can reset these counters with the Reset button.
You can enable/disable the display of any calls not called in the last frame with the Show zero calls button.

The Internal Profiler can also output to the telemetry module, use by enabling the `PROFILER_TELEMETRY` compilation symbol.
  Note: You will have to re-load the scene usually twice before the Telemetry module receives a list of the entries and creates a valid Telemetry.csv file.


### Development on MacOS

To get a working development environment on mac, things are a little different. This little guide will help you get going if you want to develop Trajectories on MacOS. Follow these steps:

- Download and install Visual Studio, the community edition is free of charge. Bear in mind that it will also require you to have XCode installed - so you could easily end up downloading about 2 GB.
- Assuming you want to use your default KSP installation for development (on MacOS it is in `/Applications/KSP_osx`), execute the script `setup_mac.sh` from the `buildscripts` folder in this repository. This will set up the environment variable `KSPDEVDIR` and create a few symbolic links in your KSP installation folder that will make it compatible with the Visual Studio Project.
- Start visual studio from the console: `open /Applications/Visual\ Studio.app`. By starting it from the console you will have the environment variable `KSPDEVDIR` set for the time it runs, Visual Studio depends on this variable to find a couple of libraries in your KSP folder. If you have a better way to set that variable when you start Visual Studio from your launchpad, please adapt this guide.
- Within Visual Studio, open the 'Trajectories.sln' project. You should be able to create a release build now.

I didn't try to run KSP in a debugger yet. If you know how to do this, please update this guide. It might work with using the Launcher.app in your KSP folder (which is already used to build Trajectories), so maybe you won't even have to download Unity.


## Building Releases

Making releases is as simple as updating two version files and the clicking build. Below is a small checklist for releases.

However, before creating a release, make sure that *you are actually authorized* to make one.
While the code is open source and you could theoretically do what you want, it would be very, very, **very** appreciated that you don't create new releases unless the current maintainer has either stepped back or has gone missing for a long time and doesn't reply to requests.

If you want to distribute your own version for testing, please do so by making it very clear to everyone that it's not an official release, and **CHANGE THE VERSION NUMBER** according to the versioning scheme below.


### Release Checklist

  - Complete the `CHANGELOG.md` file, and fill out the release date field. Make sure to credit all contributors.
  - Adjust the compatible KSP version numbers in `Trajectories.version`. Actually test if they work in all the KSP version claimed compatible.
  - Bump the version number in `Trajectories.version` and `Plugin\Properties\AssemblyInfo.cs` according the the versioning rules [below](#versioning).
  - Check with Git that your working directory is clean. No Changes are allowed, everything must be committed.
  - Build the Project in release mode
  - Clean out the `GameData` folder of your KSP install, only the ´Squad` and `SquadExpansion` folders should remain.
  - Extract the `Trajectories-<version>.zip` that was created during the Release build into your GameData folder
  - Launch KSP in the non-Developer mode and verify the functionality. Check the `output_log.txt` for errors.
  - Create a Tag with Git, push all commits and the tag to GitHub.
  - On GitHub, draft a new release selecting the newly created tag
  - Upload the `Trajectories-<version>.zip` file to GitHub, paste the changelog there
  - On SpaceDock, draft a new release and upload the `Trajectories-<version>.zip`.
  - On the KSP forums, create a new post with the changelog and links to both GitHub and SpaceDock.
  - On http://ksp-avc.cybutek.net, update the version and compatibility numbers according to the AVC version file

### Versioning

The rules for versioning are rather lax, except for the main most important #1 rule:

**DO NOT RELEASE DIFFERENT PRODUCTS UNDER THE SAME VERSION NUMBER**

While the rules below are guidelines and can be ignored rather arbitrarily by the maintainer, the rule above is THE LAW.
If you let a build slip out that differs from another build by as little as one bit but has the same version number, kittens will die and Krakens shall eat your ship.
Increment the version number even if the change is minuscule.

If the Version is MAJOR.MINOR.PATCH, then

  - MAJOR is the major version number, to be incremented when a major code restructuring and/or change in functionality has taken place.
  - MINOR is incremented when there are notable and visible changes and/or additions to functionality
  - PATCH is incremented for smaller and/or invisible changes

I believe that 3 version numbers are precise enough, so even when creating bugfix releases with tiny changes,
don't add another version number - increment the PATCH number instead.

<!--
##### Readme, Updated VS Project files and Overhauled Scripts by [PiezPiedPy](https://github.com/PiezPiedPy)
-->
