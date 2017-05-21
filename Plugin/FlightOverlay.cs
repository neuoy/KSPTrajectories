using UnityEngine;
using Vectrosity;

namespace Trajectories
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FlightOverlay : MonoBehaviour
    {
        private const int _defaultVertexCount = 32;
        private const float _lineWidth = 2.0f;
        private const float _impactRaycastDistance = 300.0f;

        private Texture2D _crossTexture;

        private Transform _crossTransform;
        private Transform CrossTransform
        {
            get
            {
                if (_crossTransform == null)
                {
                    var obj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    var mat = new Material(Shader.Find("Particles/Additive"));
                    mat.SetTexture("_MainTex", _crossTexture);
                    obj.GetComponent<Renderer>().sharedMaterial = mat;
                    obj.GetComponent<Collider>().enabled = false;
                    _crossTransform = obj.transform;
                }
                return _crossTransform;
            }
        }

        private LayerMask _targetLayer;
        private LineRenderer Line { get; set; }

        public void Start()
        {
             _targetLayer = LayerMask.GetMask("TerrainColliders", "PhysicalObjects", "EVA", "Local Scenery");

            _crossTexture = GameDatabase.Instance.GetTexture("Trajectories/Textures/AimCross", false);
			Line = gameObject.AddComponent<LineRenderer>();
            Line.useWorldSpace = false; // true;
			Line.SetVertexCount(_defaultVertexCount);
            Line.SetWidth(_lineWidth, _lineWidth);
			Line.sharedMaterial = Resources.Load("DefaultLine3D") as Material;
			Line.material.SetColor("_TintColor", new Color(0.1f, 1f, 0.1f));
		}

        private void FixedUpdate()
        {
            if (!Settings.fetch.DisplayTrajectories)
            {
                Line.enabled = false;
                return;
            }
            
            if (Trajectory.fetch.patches.Count == 0)
            {
                Line.enabled = false;
                return;
            }

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
                vertices = new Vector3[_defaultVertexCount];

                double time = lastPatch.startingState.time;
                double time_increment = (lastPatch.endTime - lastPatch.startingState.time) / (float) _defaultVertexCount;
                Orbit orbit = lastPatch.spaceOrbit;
                for (uint i = 0; i < _defaultVertexCount; ++i)
                {
                    vertices[i] = orbit.getPositionAtUT(time);
                    if (Settings.fetch.BodyFixedMode)
                        vertices[i] = Trajectory.calculateRotatedPosition(orbit.referenceBody, vertices[i] + bodyPosition, time) - bodyPosition;

                    time += time_increment;
                }
            }

            // add vertices to line
            Line.SetVertexCount(vertices.Length);
            Line.SetPositions(vertices);

            Line.enabled = true;

            if (lastPatch.impactPosition != null)
            {
                Vector3 impactPos = lastPatch.impactPosition.GetValueOrDefault();
                Vector3 up = impactPos - bodyPosition;
                up.Normalize();

                RaycastHit hit;

                // raycast downwards to find terrain and update aiming cross if we have a hit
                if (Physics.Raycast(impactPos + bodyPosition + up * _impactRaycastDistance,
                    -up, out hit, 2.0f * _impactRaycastDistance, _targetLayer))
                {
                    CrossTransform.position = hit.point + hit.normal * 0.16f;
                    CrossTransform.localEulerAngles = Quaternion.FromToRotation(Vector3.up, hit.normal).eulerAngles;
                }
            }
        }

        public void OnDestroy()
        {
            if (_crossTransform != null)
                Destroy(_crossTransform);
            if (Line != null)
                Destroy(Line);
        }
    }
}
