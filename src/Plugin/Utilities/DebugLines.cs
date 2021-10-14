/*
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

using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Trajectories
{
#if !DEBUG
        /// <summary> Handles drawing of lines useful for debugging </summary>
        internal sealed class DebugLines
        {
#else
    /// <summary> Handles drawing of lines useful for debugging </summary>
    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    internal sealed class DebugLines : MonoBehaviour
    {
        private const float MIN_WIDTH = 0.025f;
        private const float MAX_WIDTH = 150f;
        private const float DIST_DIV = 1e3f;
        private const int MAX_LINES = 100;

        internal class DebugLineRenderer
        {
            private Vector3 start;
            private Vector3 end;

            internal GameObject gameObject { get; private set; }
            internal LineRenderer Renderer { get; private set; }
            internal double Clocks { get; private set; }

            // constructor
            internal DebugLineRenderer()
            {
                start = Vector3.zero;
                end = Vector3.zero;
                Clocks = 0d;

                gameObject = new GameObject("Trajectories_DebugLineRenderer");
                if (!gameObject)
                {
                    Util.DebugLogError("game object is null");
                    return;
                }

                gameObject.transform.parent = root_go.transform;

                Renderer = gameObject.AddComponent<LineRenderer>();
                if (!Renderer)
                {
                    Util.DebugLogError("renderer is null");
                    return;
                }

                Renderer.enabled = false;
                Renderer.material = material;
                Renderer.numCapVertices = 5;
                Renderer.numCornerVertices = 7;
                Renderer.positionCount = 2;
                Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Renderer.receiveShadows = false;
                Renderer.startColor = XKCDColors.FireEngineRed;
                Renderer.endColor = XKCDColors.FireEngineRed;
                Renderer.startWidth = MIN_WIDTH;
                Renderer.endWidth = MIN_WIDTH;
                Renderer.SetPosition(0, Vector3.zero);
                Renderer.SetPosition(1, Vector3.zero);
            }

            internal void Reset()
            {
                Renderer.enabled = false;
                start = Vector3.zero;
                end = Vector3.zero;
                Clocks = 0d;
            }

            internal void Set(Vector3 start, Vector3 end, Color color, double seconds = 3d)
            {
                active_count++;
                this.start = start;
                this.end = end;
                Clocks = Util.Clocks + Util.SecondsTo(seconds);
                Renderer.startColor = color != null ? color : XKCDColors.FireEngineRed;
                Renderer.endColor = color != null ? color : XKCDColors.FireEngineRed;
                Renderer.SetPosition(0, root_go.transform.TransformPoint(start));
                Renderer.SetPosition(1, root_go.transform.TransformPoint(end));
                Renderer.enabled = true;
            }

            internal void Update()
            {
                // check for time-outs
                if (current_clock < Clocks)
                    return;

                Renderer.enabled = false;
                Clocks = 0d;
                active_count--;
            }

            internal void PreRender()
            {
                //update position
                if (Transform)
                {
                    Renderer.SetPosition(0, Transform.TransformPoint(start));
                    Renderer.SetPosition(1, Transform.TransformPoint(end));
                }
                else
                {
                    Renderer.SetPosition(0, root_go.transform.TransformPoint(start));
                    Renderer.SetPosition(1, root_go.transform.TransformPoint(end));
                }

                // adjust line width according to its distance from the camera
                Renderer.startWidth = Mathf.Clamp(Vector3.Distance(cam_pos, Renderer.GetPosition(0)) / DIST_DIV, MIN_WIDTH, MAX_WIDTH);
                Renderer.endWidth = Mathf.Clamp(Vector3.Distance(cam_pos, Renderer.GetPosition(1)) / DIST_DIV, MIN_WIDTH, MAX_WIDTH);
            }

            internal void Destroy()
            {
                if (Renderer)
                {
                    Renderer.enabled = false;
                    Object.Destroy(Renderer);
                    Renderer = null;
                }

                if (gameObject)
                {
                    Object.Destroy(gameObject);
                    gameObject = null;
                }
            }
        }

        private static GameObject root_go;
        private static bool locked = true;
        private static List<DebugLineRenderer> line_renderers;
        private static int active_count;
        private static Material material;
        private static Camera ref_camera;
        private static Vector3 cam_pos;
        private static double current_clock;

        private static bool Ready => root_go && line_renderers != null && material;

        /// <summary> The world space origin of a lines zero position and rotation </summary>
        internal static Transform Transform { get; set; }

        //  constructor
        static DebugLines()
        {
            active_count = 0;
            Camera.onPreRender += PreRender;
        }

        internal static void CheckNaterial()
        {
            if (material != null)
                return;

            material = new Material(Shader.Find("KSP/Orbit Line"));
            material ??= new Material(Shader.Find("KSP/Particles/Additive"));    // fallback shader
            if (!material)
            {
                Util.DebugLogError("material is null");
                return;
            }
        }

        internal static void CreateRenderers()
        {
            line_renderers?.Destroy();
            line_renderers = new(MAX_LINES);
            if (line_renderers == null)
            {
                Util.DebugLogError("line renderer list is null");
                return;
            }

            for (int index = 0; index < MAX_LINES; index++)
            {
                line_renderers.Add(new());
            }

            if (line_renderers.Count != MAX_LINES)
            {
                Util.DebugLogError("line renderer list is corrupt");
                line_renderers.Destroy();
                line_renderers = null;
                return;
            }
        }

        // Awake is called only once when the script instance is being loaded. Used in place of the constructor for initialization.
        internal void Awake()
        {
            locked = true;

            root_go = gameObject;

            CheckNaterial();
            CreateRenderers();
            line_renderers?.Reset();
            active_count = 0;
            locked = false;
        }

        internal void Update()
        {
            locked = true;
            if (!Ready)
            {
                Awake();

                if (!Ready)
                    return;
            }

            locked = true;

            ref_camera = CameraManager.GetCurrentCamera();
            root_go.transform.parent = Transform;
            current_clock = Util.Clocks;
            line_renderers.Update();
            locked = false;
        }

        internal static void PreRender(Camera cam)
        {
            if (!Ready || (ref_camera && ref_camera != cam))
                return;

            locked = true;
            cam_pos = ref_camera ? ref_camera.transform.position : Vector3.zero;
            line_renderers.PreRender();
            locked = false;
        }

        internal static void OnEnable()
        {
            if (!Ready)
                return;

            locked = true;
            line_renderers.Enable();
            locked = false;
        }

        internal static void OnDisable()
        {
            if (!Ready)
                return;

            locked = true;
            line_renderers.Disable();
            locked = false;
        }

        internal static void OnDestroy()
        {
            Util.DebugLog("");
            locked = true;
            Camera.onPreRender -= PreRender;
            root_go = null;

            if (line_renderers != null)
            {
                line_renderers.Destroy();
                line_renderers = null;
            }

            if (material != null)
            {
                Destroy(material);
                material = null;
            }
        }

#endif
        ///<summary> Draws a colored debug line that lasts for time in seconds, time defaults to 3 seconds </summary>
        [Conditional("DEBUG")]
        internal static void Add(Vector3 start, Vector3 end, Color color, double seconds = 3d)
        {
#if DEBUG
            if (!Ready || locked || active_count >= MAX_LINES)
                return;

            foreach (DebugLineRenderer line in line_renderers)
            {
                if (line.Renderer.enabled)
                    continue;

                line.Set(start, end, color, seconds);
                break;
            }
#endif
        }

        ///<summary> Draws a red debug line that lasts for time in seconds, time defaults to 3 seconds </summary>
        [Conditional("DEBUG")]
        internal static void Add(Vector3 start, Vector3 end, double seconds = 3d)
        {
#if DEBUG
            if (!Ready || locked || active_count >= MAX_LINES)
                return;

            foreach (DebugLineRenderer line in line_renderers)
            {
                if (line.Renderer.enabled)
                    continue;

                line.Set(start, end, XKCDColors.FireEngineRed, seconds);
                break;
            }
#endif
        }
    }

#if DEBUG
    internal static class DebugLineRendererExtensions
    {
        internal static void Destroy(this ICollection<DebugLines.DebugLineRenderer> collection)
        {
            foreach (DebugLines.DebugLineRenderer line in collection)
            {
                line.Destroy();
            }
        }
        internal static void Reset(this ICollection<DebugLines.DebugLineRenderer> collection)
        {
            foreach (DebugLines.DebugLineRenderer line in collection)
            {
                line.Reset();
            }
        }
        internal static void Update(this ICollection<DebugLines.DebugLineRenderer> collection)
        {
            foreach (DebugLines.DebugLineRenderer line in collection)
            {
                if (line.Renderer.enabled)
                    line.Update();
            }
        }
        internal static void PreRender(this ICollection<DebugLines.DebugLineRenderer> collection)
        {
            foreach (DebugLines.DebugLineRenderer line in collection)
            {
                if (line.Renderer.enabled)
                    line.PreRender();
            }
        }
        internal static void Enable(this ICollection<DebugLines.DebugLineRenderer> collection)
        {
            foreach (DebugLines.DebugLineRenderer line in collection)
            {
                line.gameObject?.SetActive(true);
            }
        }
        internal static void Disable(this ICollection<DebugLines.DebugLineRenderer> collection)
        {
            foreach (DebugLines.DebugLineRenderer line in collection)
            {
                line.gameObject?.SetActive(false);
            }
        }
    }
#endif
}
