/*
  Copyright© (c) 2017-2018 A.Korsunsky, (aka fat-lobyte).
  Copyright© (c) 2017-2021 S.Gray, (aka PiezPiedPy).

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

using UnityEngine;


namespace Trajectories
{
    internal static class GfxUtil
    {
        internal sealed class TrajectoryLine : MonoBehaviour
        {
            private const float MIN_WIDTH = 0.025f;
            private const float MAX_WIDTH = 250f;
            private const float DIST_DIV = 1e3f;

            private GameObject game_object;
            private LineRenderer line_renderer;
            private Material material;
            private Camera ref_camera;
            private Vector3 cam_pos;

            internal GameScenes Scene { get; set; }
            private bool Ready => (gameObject && line_renderer && material);

            internal void Awake()
            {
                if (!gameObject)
                    return;

                // Unity only allows one LineRenderer per object so we create a new one rather than attaching it to the camera
                game_object ??= new GameObject("Trajectories_LineRenderer");
                if (!game_object)
                {
                    Util.LogError("TrajectoryLine game object is null");
                    return;
                }

                game_object.transform.parent = gameObject.transform;

                line_renderer = game_object.AddComponent<LineRenderer>();
                if (!line_renderer)
                {
                    Util.LogError("TrajectoryLine line renderer is null");
                    return;
                }

                material ??= new Material(Shader.Find("KSP/Orbit Line"));
                material ??= new Material(Shader.Find("KSP/Particles/Additive"));    // fallback shader
                if (!material)
                {
                    Util.LogError("TrajectoryLine material is null");
                    return;
                }

                line_renderer.enabled = false;
                line_renderer.material = material;
                line_renderer.positionCount = 0;
                line_renderer.startColor = XKCDColors.BlueBlue;
                line_renderer.endColor = XKCDColors.BlueBlue;
                line_renderer.numCapVertices = 5;
                line_renderer.numCornerVertices = 7;
                line_renderer.startWidth = MIN_WIDTH;
                line_renderer.endWidth = MIN_WIDTH;
                line_renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line_renderer.receiveShadows = true;
            }

            private bool InScene()
            {
                switch (Scene)
                {
                    case GameScenes.FLIGHT:
                        return Util.IsFlight;
                }
                return false;
            }

            internal void Update()
            {
                if (!Ready)
                    Awake();
            }

            internal void SetStart(Vector3 start)
            {
                if (Ready && line_renderer.positionCount > 0 && line_renderer.enabled)
                    line_renderer.SetPosition(0, start);
            }

            internal void OnPreRender()
            {
                if (!Ready || !InScene())
                    return;

                // adjust line width according to its distance from the camera
                if (line_renderer.positionCount > 0 && line_renderer.enabled)
                {
                    ref_camera = CameraManager.GetCurrentCamera();
                    cam_pos = ref_camera ? ref_camera.transform.position : Vector3.zero;
                    line_renderer.startWidth = Mathf.Clamp(Vector3.Distance(cam_pos, line_renderer.GetPosition(0)) / DIST_DIV, MIN_WIDTH, MAX_WIDTH);
                    line_renderer.endWidth = Mathf.Clamp(Vector3.Distance(cam_pos, line_renderer.GetPosition(line_renderer.positionCount - 1)) / DIST_DIV, MIN_WIDTH, MAX_WIDTH);
                }
            }

            internal void OnEnable()
            {
                if (!Ready)
                    return;
                line_renderer.enabled = true;
            }

            internal void OnDisable()
            {
                if (!Ready)
                    return;
                line_renderer.enabled = false;
            }

            internal void OnDestroy()
            {
                if (line_renderer != null)
                    Destroy(line_renderer);
                if (material != null)
                    Destroy(material);
                if (game_object != null)
                    Destroy(game_object);

                line_renderer = null;
                material = null;
                game_object = null;
            }

            internal void Clear()
            {
                if (!Ready)
                    return;

                line_renderer.positionCount = 0;
            }

            internal void Add(Vector3 point)
            {
                if (!Ready)
                    return;

                line_renderer.positionCount++;
                line_renderer.SetPosition(line_renderer.positionCount - 1, point);
            }
        }

        internal sealed class TargetingCross : MonoBehaviour
        {
            internal const float MIN_SIZE = 2f;
            internal const float MAX_SIZE = 2e3f;
            internal const float DIST_DIV = 50f;

            private double latitude = 0d;
            private double longitude = 0d;
            private double altitude = 0d;
            private Vector3 screen_point;
            private float size = 0f;

            internal Vector3? Position { get; set; }
            internal CelestialBody Body { get; set; }
            internal Color Color { get; set; } = XKCDColors.FireEngineRed;

            internal void OnPostRender()
            {
                if (Position == null || Body == null)
                    return;

                // get impact position, translate to latitude and longitude
                Body.GetLatLonAlt(Position.Value, out latitude, out longitude, out altitude);
                // only draw if visible on the camera
                screen_point = FlightCamera.fetch.mainCamera.WorldToViewportPoint(Position.Value);
                if (!(screen_point.z >= 0f && screen_point.x >= 0f && screen_point.x <= 1f && screen_point.y >= 0f && screen_point.y <= 1f))
                    return;
                // resize marker in respect to distance from camera.
                size = Mathf.Clamp(Vector3.Distance(FlightCamera.fetch.mainCamera.transform.position, Position.Value) / DIST_DIV, MIN_SIZE, MAX_SIZE);
                // draw ground marker at this position
                GLUtils.DrawGroundMarker(Body, latitude, longitude, Color, false, 0d, size);
            }
        }

#if false
        // Class to dump loaded shader names to a text file in KSP's root, file is named shaders.txt
        internal sealed class DumpShaders : MonoBehaviour
        {
            internal void Awake()
            {
                HashSet<string> shaders = new HashSet<string>();

                FindObjectsOfType<Shader>().ToList().ForEach(sh => shaders.Add(sh.name));
                Resources.FindObjectsOfTypeAll<Shader>().ToList().ForEach(sh => shaders.Add(sh.name));

                Util.DebugLog("{0} loaded shaders", shaders.Count);
                List<string> sorted = new List<string>(shaders);
                sorted.Sort();

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(KSPUtil.ApplicationRootPath + "/shaders.txt"))
                    foreach (var sh in sorted)
                        file.WriteLine(sh);
            }
        }
#endif
    }
}
