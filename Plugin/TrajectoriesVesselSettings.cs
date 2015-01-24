using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Trajectories
{
    class TrajectoriesVesselSettings : PartModule
    {
        [KSPField(isPersistant = true, guiActive = false)]
        public float EntryAngle;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool EntryHorizon;

        [KSPField(isPersistant = true, guiActive = false)]
        public float HighAngle;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool HighHorizon;

        [KSPField(isPersistant = true, guiActive = false)]
        public float LowAngle;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool LowHorizon;

        [KSPField(isPersistant = true, guiActive = false)]
        public float GroundAngle;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool GroundHorizon;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool ProgradeEntry;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool RetrogradeEntry;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool hasTarget;

        [KSPField(isPersistant = true, guiActive = false)]
        public Vector3 targetLocation;

        [KSPField(isPersistant = true, guiActive = false)]
        public string targetReferenceBody;
    }
}
