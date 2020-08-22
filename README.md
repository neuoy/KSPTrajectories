Trajectories
============

A Kerbal Space Program mod to display trajectory predictions, accounting for atmospheric drag, lift, etc.

This mod will display a new trajectory on the map and in flight, which should match the KSP regular trajectory, unless your spacecraft encounters an atmosphere. Drag and lift are predicted and the resulting modified trajectory is displayed. It is compatible with the aerodynamic model of the original game, but also with Ferram Aerospace Research (automatic selection at game startup).

This mod is designed to help with aerobraking maneuvers (trajectory after aerobrake is displayed), and to help reach a precise point on the ground (for example to land exactly at the Kerbal Space Center).

It is possible to configure a descent profile for space planes, so that you can tweak how you intend to fly at different altitudes. When you actually follow the trajectory, indicators will be displayed on the nav ball to show how the descent profile is configured for your current altitude, and also how you should correct your descent angle to reach your target ground impact.

The mod can also help reaching a precise location on any celestial body. Even when there is no atmosphere, the original game does not display where you'll arrive according to the body rotation. This mod adds a small cross on the body showing you're estimated impact location.

----

Features
--------

- Display atmospheric trajectories
- Display crash/landing location (accounting for body rotation)
- Display trajectory after an aerobraking maneuver
- Display fixed-body trajectory (i.e. trajectory in the closest body rotating frame)

----

Supported Aerodynamic models
----------------------------

- Stock Kerbal Space Program aerodynamic model
- Ferram Aerospace Research

----

Faq
---

- Reportedly works with -
  - **Deadly Reentry** 
  - **Real Solar System** 
  - **Rescaled Kerbin** 
  - **Outer Planets Mod**
  - **kOS** 
- Compatible with **Blizzy's toolbar**. 
- If you see weird spirals or other crazy lines everywhere, double-check you didn't enable *"fixed-body"* mode by mistake. 
- If the predicted trajectory seems inaccurate, check that you set the correct orientation in the Descent profile (or checked Prograde or Retrograde), and that you keep that orientation all the time. 
- It's not possible to predict a trajectory for a future stage. We know this is a highly requested feature, but unless we duplicate big parts of the KSP-internal code, we are limited to simulating the current state of the vessel. 
- Parachutes are not simulated (that's usually not a problem if you open it near the ground). 

----

Requirements
------------

- KSP 1.8+
- ModuleManager 3.0.0+

- For KSP 1.3.1 to 1.7.2, Trajectories can be built from the *backport* branch click [here](CONTRIBUTING.md#backports-for-ksp-1.3.1+) for more information.
----

Installation
------------

- Download Trajectories.zip from the latest release (https://github.com/neuoy/KSPTrajectories/releases)
- Unzip the contents into your GameData folder.
- Don't forget that Trajectories requires ModuleManager to also be installed.

----

Reporting bugs, feature requests and contributing 
--------------

*Before posting feature requests or bug reports, please read the FAQ.*

- Want to report a bug? click [here](CONTRIBUTING.md#how-to-report-bugs).
- Do you have a new feature request? Click [here](CONTRIBUTING.md#how-to-suggest-features).
- Or do you want to contribute to Trajectories? click [here](CONTRIBUTING.md#how-to-contribute).

----

License
-------
Trajectories is available under the terms of GPL-3.0-or-later.  
See the [COPYRIGHTS.md](COPYRIGHTS.md) file for details.
