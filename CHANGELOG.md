## v2.2.1 for KSP 1.4.3
 - 2018-04-28

### Changes since the last release

 * GUI window would reset to screen center on a game restart if it was hidden in the previous game, now fixed.
 * In-Flight trajectory ground target marker would sometimes appear on screen when behind the camera view, now fixed.
 
### For Developers
 
### Known Issues


## v2.2.0 for KSP 1.4.2
 - 2018-04-02

### Changes since the last release
 * Japanese translation updated. Many thanks to UresiiZo for correcting the mistakes.
 * Italian translation updated. All thanks go to Brusura for the translation fix.
 * German translation updated. Lots of thanks to nistei for updating the mistakes.
 * French translation updated. Huge thanks to d-faure for correcting the syntax.
 * GUI settings are now persistent again.
 * Black toolbar icons when Texture Settings not set to Full-Res bug fixed.
 * In-Flight trajectory ground target marker covering entire screen when camera is close bug fixed.

### For Developers
 * New API functions added `GetTimeTillImpact`, two for the Descent Profile `ProgradeEntry RetrogradeEntry`
   and some for the Trajectories version. `GetVersion GetVersionMajor GetVersionMinor GetVersionPatch`
 

## v2.1.0 for KSP 1.4.1
 - 2018-03-21

### Changes since the last release

 * Trajectories license has been changed from MIT to GPL-3.0-or-later. Please see the LICENSE.md file for more details.
 * Russian translation updated. Many thanks to Fat-Zer for correcting the mistakes.
 * Chinese translation updated. Many thanks to studentmain for rewriting the translation.
 * Added Japanese, German, French, Italian and Portuguese languages. If you know any of these languages,
   please consider helping out with translations.
 * A big cheers and a beer to Jebs_SY for his help in fixing a rendering issue on the MapView and the Target Waypoint bug.
 * ModuleManager updated to v3.0.6
 
### For Developers

### Known Issues

 * Sometimes In-Flight trajectory ground target marker can cover entire screen when camera is close.


## v2.0.0 for KSP 1.3.1
 - 2018-02-15

### Changes since the last release

 * Completely new and overhauled user interface, based on the DialogGUI system.
   Includes (automated) localizations for Russian, Spanish and Chinese (traditional). If you know any of these languages,
   please consider helping out with translations. Send all Thanks and Kudos to PiezPiedPy for this amazing contribution!
 * In-Flight trajectory vector line and ground marker persistence bug fixed.
 * ModuleManager updated to v3.0.3
 * When combined with FAR, aerodynamic forces calculated on a packed vessel would lead to NRE's.
   Vessels are now no longer calculated when a vessel is in a packed state. Thanks go to Alex Wang for this bug fix.
 * Higher precision and better performance through more advanced numeric techniques using an RK4 integration method.
   Try disabling the Cache for more precise predictions, predictions that should not kill your FPS anymore.
   Courtesy of fat-lobyte aka Kobymaru.

### For Developers

 * API changes to help catch NRE's from improper calling of the API methods.
 * New API method `HasTarget()` checks if a target has been set.
 * Flickering entries bug in the Profiler is fixed :)
 * Added Reset averages & Show zero calls buttons, a Framerate limiter, avg calls and frame counter to the Profiler.
 
### Known Issues

 * Sometimes In-Flight trajectory ground target marker can cover entire screen when camera is close.


## v1.7.1 for KSP 1.3.1
 - 2017-10-13

### Changes since the last release

 * In-Flight trajectory overlay now uses GLUtils from MechJeb2 for rendering.
 * Fixed In-Flight trajectory for non-atmospheric bodies

### For Developers

 * Added a Jupyter notebook for descent force analysis.
 * Added the Bug Reporting section to CONTRIBUTING.md file.


## v1.7.0 for KSP 1.3.0
 - 2017-07-08

### Changes since the last release

 * Improved precision by accounting for Reynolds number in drag. Fixes Issue #84.
 * Fix crash when turning on In-Flight trajectory display while in Map view. Fixes Issue #102. Thanks to PiezPiedPy!
 * Prevent click-through in certain situations. Thanks to PiezPiedPy!
 * Numbers in the user interface don't change as quickly anymore, allowing for better readability
 * Deviation readout now switches from E to W and from N to S instead of just showing negative numbers

### For Developers

 * Added a bridge to the newly created [Telemetry](https://github.com/fat-lobyte/KSPTelemetry) module,
   allowing for nice [graphs](https://user-images.githubusercontent.com/173609/27686194-febdbca6-5cd1-11e7-877d-1ab6e5069fb6.png)
   and better debugging of prediction precision.
 * Updated Profiler UI to new Canvas system. Thanks to PiezPiedPy!
 * Added documentation for contributers in CONTRIBUTING.md file with style guidelines,
   help for building, debugging and much more.

### Known Issues

  * In-Flight trajectory for non-atmospheric trajectories is incorrect and jumpy


## v1.6.8 for KSP 1.3.0

- 2017-06-13

### Changes since the last release
  * Fixed in-flight targeting cross remaining in scene even after trajectory display was disabled.
  * Added toggle for in-flight trajectory display. Fixes Issue #96.

### Known Issues
  * Trajectory prediction is still fundamentally incorrect. See Issue #84.
  * In-Flight trajectory for non-atmospheric trajectories is jerky and jumpy.


## v1.6.7 for KSP 1.3.0

- 2017-06-09

### Changes since the last release
  * This release is brought to you by fat-lobyte (aka Kobymaru on the Forums)
  * Update ToolbarWrapper for blizzy78's Toolbar. Fixes Issue #78.
  * Enable Trajectories window in flight scene.
  * Implement in-flight trajectory line display. Please test and give feedback!
  * Fix Parenthesis silliness in .version file to make it valid JSON again. Thanks, ggpeters!
  * Update Module Manager to v2.8.0. Thanks to PiezPiedPy!
  * Disable Click-through. Thanks to PiezPiedPy!

### Known Issues
  * Trajectory prediction is still fundamentally incorrect. See Issue #84.
  * In-Flight trajectory for non-atmospheric trajectories is jerky and jumpy.
  * In-Flight trajectory leaves targeting cross in the scene, even when disabled.

### For Developers
  * KSP directory path is not hardcoded anymore. You can set up an environment variable in your Operating System named KSPDIR that points to the KSP installation directory.
  * Revamped build scripts for Visual Studio. Thanks to PiezPiedPy!
  * Added simple profiler class. Thanks to PiezPiedPy!
