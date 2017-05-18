using UnityEngine;
using Vectrosity;

namespace Trajectories
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FlightOverlay : MonoBehaviour
    {
        private const int _defaultVertexCount = 32;
        private const float _lineWidth = 0.1f;
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
            
            var paches = Trajectory.fetch.patches;
            if (paches.Count < 1)
            {
                Line.enabled = false;
                return;
            }

            Trajectory.Patch lastPatch = paches[paches.Count - 1];
            Vector3d bodyPosition = lastPatch.startingState.referenceBody.position;
            if (lastPatch.isAtmospheric)
            {
                Vector3[] vertices = new Vector3[lastPatch.atmosphericTrajectory.Length];

                for (uint i = 0; i < lastPatch.atmosphericTrajectory.Length; ++i)
                {
                    vertices[i] = lastPatch.atmosphericTrajectory[i].pos + bodyPosition;
                }

                Line.SetVertexCount(vertices.Length);
                Line.SetPositions(vertices);
            }
            else
            {
                Vector3[] vertices = new Vector3[_defaultVertexCount];

                double time = lastPatch.endTime;
                double time_increment = (lastPatch.endTime - lastPatch.startingState.time)/ _defaultVertexCount;
                for (uint i = 0; i < _defaultVertexCount; ++i)
                {
                    vertices[i] = lastPatch.spaceOrbit.getPositionAtUT(time);
                    time += time_increment;
                }

                Line.SetVertexCount(_defaultVertexCount);
                Line.SetPositions(vertices);
            }

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
                _crossTransform.gameObject.DestroyGameObject();
        }
    }
}