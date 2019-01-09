/*
  Copyright© (c) 2014-2017 Youen Toupin, (aka neuoy).
  Copyright© (c) 2017-2018 A.Korsunsky, (aka fat-lobyte).
  Copyright© (c) 2017-2018 S.Gray, (aka PiezPiedPy).

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

using System;

namespace Trajectories
{
    class TrajectoriesVesselSettings: PartModule
    {
        [KSPField(isPersistant = true, guiActive = false)]
        public bool Initialized = false;

        [KSPField(isPersistant = true, guiActive = false)]
        public double EntryAngle = Math.PI;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool EntryHorizon;

        [KSPField(isPersistant = true, guiActive = false)]
        public double HighAngle = Math.PI;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool HighHorizon;

        [KSPField(isPersistant = true, guiActive = false)]
        public double LowAngle = Math.PI;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool LowHorizon;

        [KSPField(isPersistant = true, guiActive = false)]
        public double GroundAngle = Math.PI;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool GroundHorizon;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool ProgradeEntry;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool RetrogradeEntry = true;

        [KSPField(isPersistant = true, guiActive = false)]
        public string TargetBody = "";

        [KSPField(isPersistant = true, guiActive = false)]
        public double TargetPosition_x = 0;

        [KSPField(isPersistant = true, guiActive = false)]
        public double TargetPosition_y = 0;

        [KSPField(isPersistant = true, guiActive = false)]
        public double TargetPosition_z = 0;

        [KSPField(isPersistant = true, guiActive = false)]
        public string ManualTargetTxt = "";
    }
}
