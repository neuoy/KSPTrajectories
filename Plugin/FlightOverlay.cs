using System.Collections.Generic;
using UnityEngine;

namespace Trajectories
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FlightOverlay: MonoBehaviour
    {
        private const int defaultVertexCount = 32;
        private const float lineWidth = 2.0f;

        private LineRenderer line { get; set; }

        private TrajectoryLine trajectoryLine;

        private TargetingCross targetingCross;

        public void Awake()
        {
            trajectoryLine = FlightCamera.fetch.mainCamera.gameObject.AddComponent<TrajectoryLine>();
            targetingCross = FlightCamera.fetch.mainCamera.gameObject.AddComponent<TargetingCross>();
        }

        public void Start()
        {
            line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false; // true;
            line.SetVertexCount(defaultVertexCount);
            line.SetWidth(lineWidth, lineWidth);
            line.sharedMaterial = Resources.Load("DefaultLine3D") as Material;
            line.material.SetColor("_TintColor", new Color(0.1f, 1f, 0.1f));
        }

        private void FixedUpdate()
        {
            line.enabled = false;
            trajectoryLine.enabled = false;
            targetingCross.enabled = false;

            if (!Settings.fetch.DisplayTrajectories
                || Util.IsMap
                || !Settings.fetch.DisplayTrajectoriesInFlight
                || Trajectory.fetch.patches.Count == 0)
                return;

            Vector3[] vertices;
            trajectoryLine.Vertices.Clear();

            Trajectory.Patch lastPatch = Trajectory.fetch.patches[Trajectory.fetch.patches.Count - 1];
            Vector3d bodyPosition = lastPatch.startingState.referenceBody.position;
            if (lastPatch.isAtmospheric)
            {
                vertices = new Vector3[lastPatch.atmosphericTrajectory.Length];

                for (uint i = 0; i < lastPatch.atmosphericTrajectory.Length; ++i)
                {
                    Vector3 vertex = lastPatch.atmosphericTrajectory[i].pos + bodyPosition;
                    vertices[i] = vertex;
                    trajectoryLine.Vertices.Add(vertices[i]);
                }
            }
            else
            {
                vertices = new Vector3[defaultVertexCount];

                double time = lastPatch.startingState.time;
                double time_increment = (lastPatch.endTime - lastPatch.startingState.time) / defaultVertexCount;
                Orbit orbit = lastPatch.spaceOrbit;
                for (uint i = 0; i < defaultVertexCount; ++i)
                {
                    Vector3 vertex = Util.SwapYZ(orbit.getRelativePositionAtUT(time));
                    if (Settings.fetch.BodyFixedMode)
                        vertex = Trajectory.calculateRotatedPosition(orbit.referenceBody, vertex, time);

                    vertex += bodyPosition;

                    vertices[i] = vertex;
                    trajectoryLine.Vertices.Add(vertex);

                    time += time_increment;
                }
            }

            // add vertices to line
            line.SetVertexCount(vertices.Length);
            line.SetPositions(vertices);

            line.enabled = true;

            trajectoryLine.Body = lastPatch.startingState.referenceBody;
            trajectoryLine.enabled = true;

            if (lastPatch.impactPosition != null)
            {
                targetingCross.ImpactPosition = lastPatch.impactPosition.Value;
                targetingCross.ImpactBody = lastPatch.startingState.referenceBody;
                targetingCross.enabled = true;
            }
            else
            {
                targetingCross.ImpactPosition = null;
                targetingCross.ImpactBody = null;
            }
        }

        public void OnDestroy()
        {
            if (line != null)
                Destroy(line);
        }
    }

    public class TrajectoryLine: MonoBehaviour
    {
        public List<Vector3d> Vertices = new List<Vector3d>();
        public CelestialBody Body = null;

        public void OnPostRender()
        {
            if (Body == null || Vertices == null || Vertices.Count == 0)
                return;

            GLUtils.DrawPath(Body, Vertices, Color.blue, false, true);
        }
    }

    public class TargetingCross: MonoBehaviour
    {
        public const double markerSize = 50.0f; // in meters

        public Vector3? ImpactPosition { get; internal set; }
        public CelestialBody ImpactBody { get; internal set; }


        public void OnPostRender()
        {
            if (ImpactPosition == null || ImpactBody == null)
                return;

            double impactLat, impactLon, impactAlt;

            // get impact position, translate to latitude and longitude
            ImpactBody.GetLatLonAlt(ImpactPosition.Value + ImpactBody.position, out impactLat, out impactLon, out impactAlt);

            // draw ground marker at this position
            GLUtils.DrawGroundMarker(ImpactBody, impactLat, impactLon, Color.red, false, 0, markerSize);
        }

    }
}
