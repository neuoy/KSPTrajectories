using UnityEngine;

namespace Trajectories
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FlightOverlay: MonoBehaviour
    {
        private const int defaultVertexCount = 32;
        private const float lineWidth = 2.0f;

        private LineRenderer line { get; set; }

        private TargetingCross targetingCross;

        public void Awake()
        {
            targetingCross = gameObject.AddComponent<TargetingCross>();
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
            targetingCross.enabled = false;

            if (!Settings.fetch.DisplayTrajectories
                || Util.IsMap
                || !Settings.fetch.DisplayTrajectoriesInFlight
                || Trajectory.fetch.patches.Count == 0)
                return;

            Vector3[] vertices;

            Trajectory.Patch lastPatch = Trajectory.fetch.patches[Trajectory.fetch.patches.Count - 1];
            Vector3d bodyPosition = lastPatch.startingState.referenceBody.position;
            if (lastPatch.isAtmospheric)
            {
                vertices = new Vector3[lastPatch.atmosphericTrajectory.Length];

                for (uint i = 0; i < lastPatch.atmosphericTrajectory.Length; ++i)
                {
                    vertices[i] = lastPatch.atmosphericTrajectory[i].pos + bodyPosition;
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
                    vertices[i] = Util.SwapYZ(orbit.getRelativePositionAtUT(time));
                    if (Settings.fetch.BodyFixedMode)
                        vertices[i] = Trajectory.calculateRotatedPosition(orbit.referenceBody, vertices[i], time);

                    vertices[i] += bodyPosition;

                    time += time_increment;
                }
            }

            // add vertices to line
            line.SetVertexCount(vertices.Length);
            line.SetPositions(vertices);

            line.enabled = true;

            if (lastPatch.impactPosition != null)
            {
                Vector3 impactPos = lastPatch.impactPosition.GetValueOrDefault();
                Vector3 up = impactPos - bodyPosition;
                up.Normalize();

                RaycastHit hit;
                if (Physics.Raycast(impactPos + bodyPosition + up * TargetingCross.impactRaycastDistance,
                    -up, out hit))
                {
                    targetingCross.CrossTransform.position = hit.point + hit.normal * 0.16f;
                    targetingCross.CrossTransform.localEulerAngles = Quaternion.FromToRotation(Vector3.up, hit.normal).eulerAngles;

                    targetingCross.enabled = true;
                }
            }
        }

        public void OnDestroy()
        {
            if (line != null)
                Destroy(line);
        }
    }

    public class TargetingCross: MonoBehaviour
    {
        public const float impactRaycastDistance = 300.0f;

        private GameObject planeObject;
        private Renderer renderer;

        public Transform CrossTransform { get {
                return planeObject?.transform;
            } }

        public void Start()
        {
            var crossTexture = GameDatabase.Instance.GetTexture("Trajectories/Textures/AimCross", false);

            planeObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var mat = new Material(Shader.Find("Particles/Additive"));
            mat.SetTexture("_MainTex", crossTexture);

            renderer = planeObject.GetComponent<Renderer>();
            renderer.sharedMaterial = mat;
            renderer.enabled = false;
            planeObject.GetComponent<Collider>().enabled = false;
        }

        public void OnEnable()
        {
            if (renderer != null)
                renderer.enabled = true;
        }

        public void OnDisable()
        {
            if (renderer != null)
                renderer.enabled = false;
        }


        public void OnDestroy()
        {
            if (planeObject != null)
                Destroy(planeObject);
        }
    }
}
