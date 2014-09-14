Trajectories
============

A Kerbal Space Program mod to display trajectory predictions, accounting for atmospheric drag, lift, etc.

This mod will display a new trajectory on the map, which should match the KSP regular trajectory, excepted if your spacecraft encounters an atmosphere. Drag and lift are predicted and the resulting modified trajectory is displayed. It is compatible with the aerodynamic model of the original game, but also with Ferram Aerospace Research (automatic selection at game startup).

This mod is designed to help with aerobraking manoeuvers (trajectory after aerobrake is displayed), and to help reach a precise point on the ground (for example to land exactly at the Kerbal Space Center).

It is possible to configure a descent profile for space planes, so that you can tweak how you intend to fly at different altitudes. When you actually follow the trajectory, indicators will be displayed on the nav ball to show how the descent profile is configured for your current altitude, and also how you should correct your descent angle to reach your target ground impact.

The mod can also help reaching a precise location on any celestial body. Even when there is no atmosphere, the original game does not display where you'll arrive according to the body rotation. This mod adds a small cross on the body showing you're estimated impact location.

Known bugs
----------

Trajectory mesh is not removed when changing vessel or going back to space center (the bug is apparent only if the map is open at that moment)

The trajectory prediction is incorrect when using time warp

The stock aerodynamic model does not predict lift (prediction will be inaccurate for spacecrafts with wings). The FAR model should be fine, though.

Planned features
----------------

Serialize target point, descent profile, etc, in save file, so that they are correctly restored when loading the game, and also when switching vessels

Use maneuver nodes to predict trajectories of the flight plan, instead of just predicting the current trajectory

Predict trajectory by taking the maximum vessel acceleration into account. That is, if you have a maneuver node where you'll accelerate in a direction, KSP just assumes the velocity change will be instantaneous. This is not true of course, and with low acceleration vessels (electric or nuclear propulsion), it can make a big change

Ray trace to find actual ground intersection (in particular, should detect collision with mountains even if the impact with sea level is way farther)

Refactor GUI so that everything is in a single window. Allow to toggle the window visible/hidden, and to move it on the screen.

Display 3D helpers (rectangles that the craft must go through to follow the planned trajectory)

Display a descent graph, indicating altitude over ground distance, and also ground altitude (would be handy to plan mountain fly-by)

Allow to set an object as target (for example, a flag), and a special option for the Kerbal Space Center.

Add a GUI setting to change the auto-update threshold (some crafts are very sensitive and any change, due to physics simulation, have a huge impact on the resulting aerodynamic model ; but other crafts are very stable)
