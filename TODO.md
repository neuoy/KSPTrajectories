# TODO

## Documentation


### GitHub
 - Finish CONTRIBUTING.md
   - Bug reporting section
   - Git/Newline section
 - Rewrite README.md

### Forum Thread

 - Redo Imgur Album
 - Simplify and update "manual"

## Numerical

 - Improve cache
   -  find better ordinate distribution?
 - Extract implicit verlet integrator from the code, make it explicit
 - Add Runge-Kutta 4 integrator
 - Play around with time-delta. Make configurable?
 - Implement binary search for impact position?

## Debugging

### Performance

 - Find worst offenders for cache-less computation. Maybe we can improve that.

### Telemetry

 - Implement telemetry for single simulation instances

# Old todo

Youen's old TODO list points

## Auto-pilot

 - ./ auto-pilot UI next to the navball (both in flight view and map view)
 - ./ disengage auto-pilot when touching an input, or enabling SAS
 - smooth avoidance of mountains
 - add guidance and auto-pilot for aerobraking (specify desired Pe either from current trajectory or in a text box)
 - detect auto-pilot failures (can't steer as needed, or can't correct the trajectory to reach the target)
 - display auto-pilot status (On track, Correcting, Avoiding mountain, Can't steer, Can't correct trajectory)
 - ./ test auto-pilot on capsules (prograde and retrograde)
 - move auto-pilot feature in a module, as a separate part, add in research tree for career mode (or embed in probe cores of high-enough tech-level)
 - make auto-pilot align with the runway (for spaceplane landing)

## Staging

 - check if it's possible to use FAR to predict aerodynamic forces for a future stage of the rocket

## Refactoring

 - VesselAerodynamicModel: each model (stock and FAR) in a different class that derives from VesselAerodynamicModel, and add a static function that instantiates the correct model for the installed mods
 - Implement the cache system separately from the VesselAerodynamicModel, and allow storing multiple caches for different bodies
