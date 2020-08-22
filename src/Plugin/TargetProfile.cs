/*
  Copyright© (c) 2014-2017 Youen Toupin, (aka neuoy).
  Copyright© (c) 2014-2018 A.Korsunsky, (aka fat-lobyte).
  Copyright© (c) 2017-2020 S.Gray, (aka PiezPiedPy).

  This file is part of Trajectories.
  Trajectories is available under the terms of GPL-3.0-or-later.
  See the LICENSE.md file for more details.

  Trajectories is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  Trajectories is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

  You should have received a copy of the GNU General Public License
  along with Trajectories.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Linq;

namespace Trajectories
{
    internal class TargetProfile
    {
        /// <returns> True if the target is set. </returns>
        internal bool HasTarget() => LocalPosition.HasValue && Body != null;

        /// <summary> The targets reference body </summary>
        internal CelestialBody Body { get; set; }

        /// <summary> The targets position in WorldSpace </summary>
        internal Vector3d? WorldPosition
        {
            // A transform that has double precision would be nice so we can have target precision in meter's
            get => LocalPosition.HasValue ? Body?.transform.TransformDirection(LocalPosition.Value) : null;
            set => LocalPosition = value.HasValue ? Body?.transform.InverseTransformDirection(value.Value) : null;
        }

        /// <summary> The targets position in LocalSpace relative to the target body </summary>
        internal Vector3d? LocalPosition { get; private set; }

        /// <summary> Manual target TextBox string </summary>
        internal string ManualText { get; set; } = "";

        private Trajectory trajectory { get; }

        internal TargetProfile(Trajectory trajectory)
        {
            this.trajectory = trajectory;
        }

        /// <summary> Sets the target to a body and a World position. Saves the target to the active vessel. </summary>
        internal void SetFromWorldPos(CelestialBody body, Vector3d position)
        {
            Body = body;
            WorldPosition = position;

            Save();
        }

        /// <summary> Sets the target to a body and a body-relative position. </summary>
        internal void SetFromLocalPos(CelestialBody body, Vector3d position)
        {
            Body = body;
            LocalPosition = position;
        }

        /// <summary>
        /// Sets the target to a body and a World position. If the altitude is not given, it will be calculated as the surface altitude at that latitude/longitude.
        /// Saves the target to the active vessel.
        /// </summary>
        internal void SetFromLatLonAlt(CelestialBody body, double latitude, double longitude, double? altitude = null)
        {
            Body = body;

            if (!altitude.HasValue)
            {
                Vector3d relPos = body.GetWorldSurfacePosition(latitude, longitude, 2.0) - body.position;
                altitude = Trajectory.GetGroundAltitude(body, relPos);
            }

            LocalPosition = body.GetRelSurfacePosition(latitude, longitude, altitude.Value);

            Save();
        }

        /// <summary>
        /// Returns the trajectories target as latitude, longitude and altitude, returns null if no target.
        /// </summary>
        public Vector3d? GetLatLonAlt()
        {
            if (Body != null && WorldPosition.HasValue)
            {
                Body.GetLatLonAlt(WorldPosition.Value + Body.position, out var latitude, out var longitude, out var altitude);
                return new Vector3d(latitude, longitude, altitude);
            }

            return null;
        }

        /// <summary> Clears the target </summary>
        internal void Clear()
        {
            Util.DebugLog("");
            Body = null;
            LocalPosition = null;
        }

        /// <summary> Saves the profile to the passed vessel module </summary>
        internal void Save(TrajectoriesVesselSettings module)
        {
            module.TargetBody = Body == null ? "" : Body.name;
            module.TargetPosition_x = LocalPosition?.x ?? 0d;
            module.TargetPosition_y = LocalPosition?.y ?? 0d;
            module.TargetPosition_z = LocalPosition?.z ?? 0d;
            module.ManualTargetTxt = ManualText;
        }

        /// <summary> Saves the profile to the active vessel module </summary>
        internal void Save()
        {
            if (!trajectory.IsVesselAttached)
                return;

            //Util.DebugLog("Saving vessels target profile...");
            foreach (TrajectoriesVesselSettings module in trajectory.AttachedVessel.Parts.SelectMany(p => p.Modules.OfType<TrajectoriesVesselSettings>()))
            {
                Save(module);
            }

            //Util.DebugLog("Target profile saved");
        }
    }
}
