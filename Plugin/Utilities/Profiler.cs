using System;
using System.Collections.Generic;
using System.Diagnostics;
using KSP.Localization;
using UnityEngine;


namespace Trajectories
{
    /// <summary> Simple profiler for measuring the execution time of code placed between the Start and Stop methods. </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public sealed class Profiler : MonoBehaviour
    {
#if DEBUG_PROFILER
        // constants
        private const float width = 400.0f;
        private const float height = 500.0f;

        private const float value_width = 65.0f;

        // permit global access
        private static Profiler fetch_ = null;

        // visible flag
        private static bool visible;

        // popup window
        private static MultiOptionDialog multi_dialog;
        private static PopupDialog popup_dialog;
        private static DialogGUIVerticalLayout dialog_items;

        // an entry in the profiler
        private class entry
        {
            public double start;        // used to measure call time
            public double calls;        // number of calls in current simulation step
            public double time;         // time in current simulation step
            public double prev_calls;   // number of calls in previous simulation step
            public double prev_time;    // time in previous simulation step
            public double tot_calls;    // number of calls in total used for avg calculation
            public double tot_time;     // total time used for avg calculation

            public string last_txt = "";    // last call time display string
            public string avg_txt = "";     // average call time display string
            public string calls_txt = "";   // number of calls display string
        }

        // store all entries
        private Dictionary<string, entry> entries = new Dictionary<string, entry>();

        // display update timer
        private static double update_timer = Util.Clocks;
        private static double update_fps = 10;  // Frames per second the entry values displayed will update.
        private static bool calculate = true;


        public static Profiler fetch
        {
            get
            {
                return fetch_;
            }
        }

        //  constructor
        public Profiler()
        {
            // enable global access
            fetch_ = this;

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
                   // create average reset button
                   new DialogGUIButton(Localizer.Format("#autoLOC_900305"),
                           OnButtonClick_Reset, () => true, 75, 25, false),
                   // create header line
                   new DialogGUIHorizontalLayout(
                       new DialogGUILabel("<b>   NAME</b>", true),
                       new DialogGUILabel("<b>LAST</b>", value_width),
                       new DialogGUILabel("<b>AVG</b>", value_width),
                       new DialogGUILabel("<b>CALLS</b>", value_width - 15)),
                   // create scrollbox for entry data
                   new DialogGUIScrollList(new Vector2(), false, true, dialog_items)
               });
        }

        // Awake is called only once when the script instance is being loaded. Used in place of the constructor for initialization.
        private void Awake()
        {
            // keep it alive
            DontDestroyOnLoad(this);

            // create popup dialog
            popup_dialog = PopupDialog.SpawnPopupDialog(multi_dialog, true, HighLogic.UISkin, false, "");
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
        }

        private void FixedUpdate()
        {
            // skip updates for a smoother display
            if (Util.Clocks - update_timer <= Stopwatch.Frequency / update_fps)
                calculate = false;
            else
            {
                update_timer = Util.Clocks;
                calculate = true;
            }

            foreach (KeyValuePair<string, entry> p in fetch_.entries)
            {
                entry e = p.Value;
                e.prev_calls = e.calls;
                e.prev_time = e.time;
                e.tot_calls += e.calls;
                e.tot_time += e.time;
                e.calls = 0;
                e.time = 0;

                if (calculate)
                {
                    e.last_txt = (e.prev_calls > 0 ? Util.Microseconds(e.prev_time /
                                    e.prev_calls).ToString("F2") : "") + "ms";
                    e.avg_txt = (e.tot_calls > 0 ? Util.Microseconds(e.tot_time /
                                    e.tot_calls).ToString("F2") : "") + "ms";
                    e.calls_txt = e.prev_calls.ToString();
                }
            }
        }

        private void OnDestroy()
        {
            fetch_ = null;
            popup_dialog.Dismiss();
            popup_dialog = null;
        }

        private static string GetTitle()
        {
            switch (Localizer.CurrentLanguage)
            {
                case "es-es":
                    return "Trayectorias Profiler";
                case "ru":
                    return "Провайдер траектории";
                case "zh-cn":
                    return "軌跡分析儀";
                default:
                    return "Trajectories Profiler";
            }
        }

        private static void OnButtonClick_Reset()
        {
            foreach (KeyValuePair<string, entry> e in fetch_.entries)
            {
                e.Value.tot_calls = 0;
                e.Value.tot_time = 0;
            }
        }

        private void AddDialogItem(string e_name)
        {
            // add item
            dialog_items.AddChild(
                new DialogGUIHorizontalLayout(
                    new DialogGUILabel("  " + e_name, true),
                    new DialogGUILabel(() => { return entries[e_name].last_txt; }, value_width),
                    new DialogGUILabel(() => { return entries[e_name].avg_txt; }, value_width),
                    new DialogGUILabel(() => { return entries[e_name].calls_txt; }, value_width - 15)));

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
            if (fetch_ == null)
                return;

            if (!fetch_.entries.ContainsKey(e_name))
            {
                fetch_.entries.Add(e_name, new entry());
                fetch_.AddDialogItem(e_name);
            }

            fetch_.entries[e_name].start = Util.Clocks;
#endif
        }

        [System.Diagnostics.Conditional("DEBUG_PROFILER")]
        /// <summary> Stop a profiler entry. </summary>
        public static void Stop(string e_name)
        {
#if DEBUG_PROFILER
            if (fetch_ == null)
                return;

            entry e = fetch_.entries[e_name];

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
