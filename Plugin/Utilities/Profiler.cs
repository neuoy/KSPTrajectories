using System;
using System.Collections.Generic;
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
        private static Profiler instance = null;

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
            public double tot_calls;    // number of calls in total
            public double tot_time;     // total time
        }

        // store all entries
        private Dictionary<string, entry> entries = new Dictionary<string, entry>();


        public static Profiler Instance
        {
            get
            {
                return instance;
            }
        }

        //  constructor
        public Profiler()
        {
            // enable global access
            instance = this;

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
            foreach (var p in entries)
            {
                entry e = p.Value;
                e.prev_calls = e.calls;
                e.prev_time = e.time;
                e.tot_calls += e.calls;
                e.tot_time += e.time;
                e.calls = 0;
                e.time = 0;
            }
        }

        private void OnDestroy()
        {
            instance = null;
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

        private void AddDialogItem(string e_name)
        {
            // add item
            dialog_items.AddChild(
                new DialogGUIHorizontalLayout(
                    new DialogGUILabel("  " + e_name, true),
                    new DialogGUILabel(() =>
                    {
                        return (entries[e_name].prev_calls > 0 ? Util.Microseconds(entries[e_name].prev_time /
                                    entries[e_name].prev_calls).ToString("F2") : "") + "ms";
                    }, value_width),
                    new DialogGUILabel(() =>
                    {
                        return (entries[e_name].tot_calls > 0 ? Util.Microseconds(entries[e_name].tot_time /
                                    entries[e_name].tot_calls).ToString("F2") : "") + "ms";
                    }, value_width),
                    new DialogGUILabel(() =>
                    {
                        return entries[e_name].prev_calls.ToString();
                    }, value_width - 15)));

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
            if (instance == null)
                return;

            if (!instance.entries.ContainsKey(e_name))
            {
                instance.entries.Add(e_name, new entry());
                instance.AddDialogItem(e_name);
            }

            instance.entries[e_name].start = Util.Clocks;
#endif
        }

        [System.Diagnostics.Conditional("DEBUG_PROFILER")]
        /// <summary> Stop a profiler entry. </summary>
        public static void Stop(string e_name)
        {
#if DEBUG_PROFILER
            if (instance == null)
                return;

            entry e = instance.entries[e_name];

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
