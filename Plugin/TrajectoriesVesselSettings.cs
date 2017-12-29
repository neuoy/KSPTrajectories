using UnityEngine;

namespace Trajectories
{
    class TrajectoriesVesselSettings: PartModule
    {
        [KSPField(isPersistant = true, guiActive = false)]
        public double EntryAngle;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool EntryHorizon;

        [KSPField(isPersistant = true, guiActive = false)]
        public double HighAngle;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool HighHorizon;

        [KSPField(isPersistant = true, guiActive = false)]
        public double LowAngle;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool LowHorizon;

        [KSPField(isPersistant = true, guiActive = false)]
        public double GroundAngle;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool GroundHorizon;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool ProgradeEntry = true;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool RetrogradeEntry;

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
