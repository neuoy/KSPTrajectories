## v1.6.8 for KSP 1.3.0

- 2017-06-13

### Changes since the last release
  * Fixed in-flight targeting cross remaining in scence even after trajectory display was disabled.
  * Added toggle for in-flight trajectory display. Fixes Issue #96.

### Known Issues
  * Trajectory prediction is still fundamentally incorrect. See Issue #84.
  * In-Flight trajectory for non-atmospheric trajectories is jerky and jumpy.


## v1.6.7 for KSP 1.3.0

- 2017-06-09

### Changes since the last release
  * This release is brought to you by fat-lobyte (aka Kobymaru on the Forums)
  * Update ToolbarWrapper for blizzy78's Toolbar. Fixes Issue #78.
  * Enable Trajectories window in flight scence.
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
