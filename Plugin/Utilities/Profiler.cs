/*
  Copyright© (c) 2017-2018 S.Gray, (aka PiezPiedPy).
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

#if DEBUG_PROFILER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using KSP.Localization;
using UnityEngine;
#endif

namespace Trajectories
{
#if !DEBUG_PROFILER
    /// <summary> Simple profiler for measuring the execution time of code placed between the Start and Stop methods. </summary>
    public sealed class Profiler
    {
#endif
#if DEBUG_PROFILER
    /// <summary> Simple profiler for measuring the execution time of code placed between the Start and Stop methods. </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public sealed class Profiler: MonoBehaviour
    {
        // constants
        private const float width = 500.0f;
        private const float height = 500.0f;

        private const float value_width = 65.0f;

        // visible flag
        private static bool visible = false;
        private static bool show_zero = true;

        // popup window
        private static MultiOptionDialog multi_dialog;
        private static PopupDialog popup_dialog;
        private static DialogGUIVerticalLayout dialog_items;

        // an entry in the profiler
        private class Entry
        {
            public double start;        // used to measure call time
            public long calls;          // number of calls in current simulation step
            public double time;         // time in current simulation step
            public long prev_calls;     // number of calls in previous simulation step
            public double prev_time;    // time in previous simulation step
            public long tot_calls;      // number of calls in total used for avg calculation
            public double tot_time;     // total time used for avg calculation

            public string last_txt = "";        // last call time display string
            public string avg_txt = "";         // average call time display string
            public string calls_txt = "";       // number of calls display string
            public string avg_calls_txt = "";   // number of average calls display string
        }

        // store all entries
        private Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

        // display update timer
        private static double update_timer = Util.Clocks;
        private static double timeout = Stopwatch.Frequency / update_fps;
        private const double update_fps = 5.0;      // Frames per second the entry value display will update.
        private static long tot_frames = 0;         // total physics frames used for avg calculation
        private static string tot_frames_txt = "";  // total physics frames display string


        // permit global access
        public static Profiler Fetch { get; private set; } = null;

        //  constructor
        public Profiler()
        {
            // enable global access
            Fetch = this;

            // create window
            dialog_items = new DialogGUIVerticalLayout();
            multi_dialog = new MultiOptionDialog(
               "TrajectoriesProfilerWindow",
               "",
               GetTitle(),
               HighLogic.UISkin,
               new Rect(0.5f, 0.5f, width, height),
               new DialogGUIBase[]
               {
                   new DialogGUIVerticalLayout(false, false, 0, new RectOffset(), TextAnchor.UpperCenter,
                       // create average reset and show zero calls buttons
                       new DialogGUIHorizontalLayout(false, false,
                           new DialogGUIButton(Localizer.Format("#autoLOC_900305"),
                               OnButtonClick_Reset, () => true, 75, 25, false),
                           new DialogGUIToggle(() => { return show_zero; },"Show zero calls", OnButtonClick_ShowZero),
                           new DialogGUILabel(() => { return tot_frames_txt; }, value_width + 50f)),
                       // create header line
                       new DialogGUIHorizontalLayout(
                           new DialogGUILabel("<b>   NAME</b>", true),
                           new DialogGUILabel("<b>LAST</b>", value_width),
                           new DialogGUILabel("<b>AVG</b>", value_width),
                           new DialogGUILabel("<b>CALLS</b>", value_width - 15f),
                           new DialogGUILabel("<b>AVG</b>", value_width - 10f))),
                   // create scrollbox for entry data
                   new DialogGUIScrollList(new Vector2(), false, true, dialog_items)
               });
        }

        // Awake is called only once when the script instance is being loaded. Used in place of the constructor for initialization.
        private void Awake()
        {
            // create popup dialog
            popup_dialog = PopupDialog.SpawnPopupDialog(multi_dialog, false, HighLogic.UISkin, false, "");
            if (popup_dialog != null)
                popup_dialog.gameObject.SetActive(false);
        }

        private void Update()
        {
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
                     Input.GetKeyUp(KeyCode.P) && popup_dialog != null)
            {
                visible = !visible;
                popup_dialog.gameObject.SetActive(visible);
            }

            // skip updates for a smoother display
            if (((Util.Clocks - update_timer) > timeout) && visible)
            {
                update_timer = Util.Clocks;
                Calculate();
            }
        }

        private static void Calculate()
        {
            foreach (KeyValuePair<string, Entry> p in Fetch.entries)
            {
                Entry e = p.Value;

                if (e.prev_calls > 0L)
                {
                    e.last_txt = Util.Microseconds(e.prev_time / e.prev_calls).ToString("F2") + "ms";
                    e.calls_txt = e.prev_calls.ToString();
                }
                else if (show_zero)
                {
                    e.last_txt = "ms";
                    e.calls_txt = "0";
                }

                e.avg_txt = (e.tot_calls > 0L ? Util.Microseconds(e.tot_time / e.tot_calls).ToString("F2") : "") + "ms";
                e.avg_calls_txt = tot_frames > 0L ? ((float)e.tot_calls / (float)tot_frames).ToString("F3") : "0";
            }

            tot_frames_txt = tot_frames.ToString() + " Frames";
        }

        private void FixedUpdate()
        {
            foreach (KeyValuePair<string, Entry> p in Fetch.entries)
            {
                Entry e = p.Value;

                e.prev_calls = e.calls;
                e.prev_time = e.time;
                e.tot_calls += e.calls;
                e.tot_time += e.time;
                e.calls = 0L;
                e.time = 0.0;
            }

            ++tot_frames;
        }

        private void OnDestroy()
        {
            Fetch = null;
            if (popup_dialog != null)
            {
                popup_dialog.Dismiss();
                popup_dialog = null;
            }
        }

        private static string GetTitle()
        {
            switch (Localizer.CurrentLanguage)
            {
                case "es-es":
                    return "Trayectorias Profiler";
                case "ru":
                    return "Провайдер Траектория";
                case "zh-cn":
                    return "軌跡分析儀";
                case "ja":
                    return "軌道プロファイラ";
                case "de-de":
                    return "Trajektorien Profiler";
                case "fr-fr":
                    return "Trajectoires Profiler";
                case "it-it":
                    return "Trailerories Profiler";
                case "pt-br":
                    return "Trajectórias perfil";
                default:
                    return "Trajectories Profiler";
            }
        }

        private static void OnButtonClick_Reset()
        {
            foreach (KeyValuePair<string, Entry> e in Fetch.entries)
            {
                e.Value.tot_calls = 0L;
                e.Value.tot_time = 0.0;
            }

            tot_frames = 0L;
        }

        private static void OnButtonClick_ShowZero(bool inState)
        {
            show_zero = inState;
        }

        private void AddDialogItem(string e_name)
        {
            // add item
            dialog_items.AddChild(
                new DialogGUIHorizontalLayout(
                    new DialogGUILabel("  " + e_name, true),
                    new DialogGUILabel(() => { return entries[e_name].last_txt; }, value_width),
                    new DialogGUILabel(() => { return entries[e_name].avg_txt; }, value_width),
                    new DialogGUILabel(() => { return entries[e_name].calls_txt; }, value_width - 15f),
                    new DialogGUILabel(() => { return entries[e_name].avg_calls_txt; }, value_width - 10f)));

            // required to force the Gui creation
            Stack<Transform> stack = new Stack<Transform>();
            stack.Push(dialog_items.uiItem.gameObject.transform);
            dialog_items.children[dialog_items.children.Count - 1].Create(ref stack, HighLogic.UISkin);
        }
#endif

        [System.Diagnostics.Conditional("DEBUG_PROFILER")]
        /// <summary> Start a profiler entry. </summary>
        public static void Start(string e_name)
        {
#if DEBUG_PROFILER
            if (Fetch == null)
                return;

            if (!Fetch.entries.ContainsKey(e_name))
            {
                Fetch.entries.Add(e_name, new Entry());
                Fetch.AddDialogItem(e_name);
            }

            Fetch.entries[e_name].start = Util.Clocks;
#endif
        }

        [System.Diagnostics.Conditional("DEBUG_PROFILER")]
        /// <summary> Stop a profiler entry. </summary>
        public static void Stop(string e_name)
        {
#if DEBUG_PROFILER
            if (Fetch == null)
                return;

            Entry e = Fetch.entries[e_name];

            ++e.calls;
            e.time += Util.Clocks - e.start;
#endif
        }

#if DEBUG_PROFILER

        /// <summary> Profile a function scope. </summary>
        public class ProfileScope : IDisposable
        {
            public ProfileScope(string name)
            {
                this.name = name;
                Profiler.Start(name);
            }

            public void Dispose()
            {
                Profiler.Stop(name);
            }

            private string name;
        }

#endif
    }

} // Trajectories
