/*
  Copyright© (c) 2017-2018 A.Korsunsky, (aka fat-lobyte).

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

namespace Trajectories
{
#if DEBUG
    public class TrajectoriesDebug : PartModule
    {
        [KSPField(isPersistant = false, guiActive = true)]
        public float Drag;

        [KSPField(isPersistant = false, guiActive = true)]
        public float Lift;
    }
#endif
}
