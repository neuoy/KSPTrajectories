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

using System.Diagnostics;
#if DEBUG_WATCHER
using System.Collections.Generic;
using KSP.Localization;
using UnityEngine;
using UnityEngine.Events;
#endif

namespace Trajectories
{
#if !DEBUG_WATCHER
    /// <summary> Simple gui for watching the state of variables placed in the Watch methods. </summary>
    public sealed class Watcher
    {
#endif
#if DEBUG_WATCHER
    /// <summary> Simple gui for watching the state of variables placed in the Watch methods. </summary>
    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    public sealed class Watcher : MonoBehaviour
    {
        // constants
        private const float WIDTH = 400f;
        private const float HEIGHT = 400f;

        private const float VALUE_WIDTH = 180f;

        // visible flag
        private static bool visible = false;
        private static bool show_zero = false;

        // popup window
        private static MultiOptionDialog multi_dialog;
        private static PopupDialog popup_dialog;
        private static DialogGUIScrollList scroll_list;
        private static DialogGUIVerticalLayout dialog_items;

        // an entry in the watcher
        private enum Type
        {
            DOUBLE = 0,
            VECTOR3D,
            STRING
        }

        private class Entry
        {
            public bool in_gui;             // if true the entry has been added to the Gui
            public Type type;               // used to store last value type
            public double value;            // used to store last value
            public Vector3d vector;         // used to store last value
            public string value_txt = "";   // last value display string
        }

        // store all entries
        private static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        private static readonly List<string> channels = new List<string>();


        // display update timer
        private const double UPDATE_FPS = 5d;      // Frames per second the entry value display will update.
        private static double update_timer = Util.Clocks;
        private static readonly double timeout = Stopwatch.Frequency / UPDATE_FPS;
        private static long tot_frames = 0L;         // total physics frames used for avg calculation
        private static string tot_frames_txt = "";  // total physics frames display string

        //  constructor
        private static bool Ready => (multi_dialog != null && popup_dialog && scroll_list != null);

        static Watcher()
        {
            // create window
            dialog_items = new DialogGUIVerticalLayout();
            scroll_list = new DialogGUIScrollList(new Vector2(), false, true, dialog_items);
            multi_dialog = new MultiOptionDialog(
               "TrajectoriesWatcherWindow",
               "",
               GetTitle(),
               HighLogic.UISkin,
               new Rect(0.5f, 0.5f, WIDTH, HEIGHT),
               new DialogGUIBase[]
               {
                   new DialogGUIVerticalLayout(false, false, 0f, new RectOffset(), TextAnchor.UpperCenter,
                       // create reset button
                       new DialogGUIHorizontalLayout(false, false,
                           new DialogGUIButton(Localizer.Format("#autoLOC_900305"),
                               OnButtonClick_Reset, () => true, 75f, 25f, false),
                           new DialogGUILabel(() => { return tot_frames_txt; }, VALUE_WIDTH + 50f)),
                       // create header line
                       new DialogGUIHorizontalLayout(
                           new DialogGUILabel("<b>   NAME</b>", true),
                           new DialogGUILabel("<b>LAST</b>", VALUE_WIDTH))),
                   // create scrollbox for entry data
                   scroll_list
               });
        }

        // Awake is called only once when the script instance is being loaded. Used in place of the constructor for initialization.
        public void Awake() => SpawnDialog();

#if WATCHER_TELEMETRY
        public void Start() => ConstructTelemetry();
#endif

        private static void SpawnDialog()
        {
            if (multi_dialog != null)
            {
                ClampToScreen();

                // create popup dialog
                popup_dialog = PopupDialog.SpawnPopupDialog(multi_dialog, false, HighLogic.UISkin, false, "");
                popup_dialog.onDestroy.AddListener(new UnityAction(OnPopupDialogDestroy));
                scroll_list.children.Add(new DialogGUIVerticalLayout());
            }
        }

        public void Update()
        {
            if (Util.IsPaused)
                return;

            // skip calculations for a smoother display
#if WATCHER_TELEMETRY
            if ((Util.Clocks - update_timer) > (Stopwatch.Frequency / 25d))   // samples at 25 fps
            {
                update_timer = Util.Clocks;
                Calculate();
            }
#else
            if (((Util.Clocks - update_timer) > timeout) && visible)
            {
                update_timer = Util.Clocks;
                Calculate();
            }
#endif

            // add entries to dialog
            if (dialog_items.children.Count != entries.Count)
                UpdateDialogItems();

            // hide or show the dialog box
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyUp(KeyCode.W))
                visible = !visible;

            if (visible)
            {
                if (popup_dialog == null)
                    SpawnDialog();
                popup_dialog.gameObject.SetActive(true);
            }
            else if (popup_dialog != null)
            {
                popup_dialog.gameObject.SetActive(false);
            }
        }

        private static void UpdateDialogItems()
        {
            foreach (KeyValuePair<string, Entry> p in entries)
            {
                if (!p.Value.in_gui)
                    AddDialogItem(p);
            }
        }

        private static void Calculate()
        {
            if (entries == null)
                return;

            foreach (KeyValuePair<string, Entry> p in entries)
            {
                Entry e = p.Value;

#if WATCHER_TELEMETRY
                Telemetry.Send(p.Key, e.value);
#endif
                switch (e.type)
                {
                    case Type.DOUBLE:
                        e.value_txt = e.value.ToString("F8");
                        break;
                    case Type.VECTOR3D:
                        e.value_txt = Util.ToString(e.vector, "0.00000");
                        break;
                    case Type.STRING:
                        break;
                    default:
                        e.value_txt = "";
                        break;
                }
            }

            tot_frames_txt = tot_frames.ToString() + " Frames";
        }

        public void FixedUpdate() => ++tot_frames;

        public static void OnDestroy()
        {
            if (popup_dialog != null)
            {
                popup_dialog.Dismiss();
                popup_dialog = null;
            }
        }

#if WATCHER_TELEMETRY
        private static void ConstructTelemetry()
        {
            foreach (string name in channels)
            {
                Telemetry.AddChannel<double>(name);
            }
        }
#endif

        /// <summary>
        /// Defaults window to center of screen and also ensures it remains within the screen bounds.
        /// </summary>
        private static void ClampToScreen()
        {
            float border = 50f;
            bool adjusted = false;

            if (multi_dialog.dialogRect.position.x <= 0f || multi_dialog.dialogRect.position.y <= 0f)
            {
                // default window to center of screen
                multi_dialog.dialogRect.Set(0.5f, 0.5f, WIDTH, HEIGHT);
                adjusted = true;
            }
            else
            {
                // ensure window remains within the screen bounds
                Vector2 pos = new Vector2(((multi_dialog.dialogRect.position.x * Screen.width) - (Screen.width / 2)) * GameSettings.UI_SCALE,
                                          ((multi_dialog.dialogRect.position.y * Screen.height) - (Screen.height / 2)) * GameSettings.UI_SCALE);

                if (pos.x > (Screen.width / 2) - border)
                {
                    pos.x = (Screen.width / 2) - (border + (WIDTH / 2));
                    adjusted = true;
                }
                else if (pos.x < ((Screen.width / 2) - border) * -1f)
                {
                    pos.x = ((Screen.width / 2) - (border + (WIDTH / 2))) * -1f;
                    adjusted = true;
                }

                if (pos.y > (Screen.height / 2) - border)
                {
                    pos.y = (Screen.height / 2) - (border + (HEIGHT / 2));
                    adjusted = true;
                }
                else if (pos.y < ((Screen.height / 2) - border) * -1f)
                {
                    pos.y = ((Screen.height / 2) - (border + (HEIGHT / 2))) * -1f;
                    adjusted = true;
                }

                if (adjusted)
                {
                    multi_dialog.dialogRect.Set(
                        ((Screen.width / 2) + (pos.x / GameSettings.UI_SCALE)) / Screen.width,
                        ((Screen.height / 2) + (pos.y / GameSettings.UI_SCALE)) / Screen.height,
                        WIDTH, HEIGHT);
                }
            }
        }

        /// <summary>
        /// Called when the PopupDialog OnDestroy method is called. Used for saving the window position.
        /// </summary>
        private static void OnPopupDialogDestroy()
        {
            // save popup position. Note. PopupDialog.RTrf is an offset from the center of the screen.
            if (popup_dialog != null)
            {
                Vector2 window_pos = new Vector2(
                    ((Screen.width / 2) + (popup_dialog.RTrf.position.x / GameSettings.UI_SCALE)) / Screen.width,
                    ((Screen.height / 2) + (popup_dialog.RTrf.position.y / GameSettings.UI_SCALE)) / Screen.height);
                //Util.DebugLog("Saving profiler window position as {0}", window_pos.ToString());
                multi_dialog.dialogRect.Set(window_pos.x, window_pos.y, WIDTH, HEIGHT);
                dialog_items.children.Clear();
                scroll_list.children.Clear();
                entries.Clear();
            }
        }

        private static string GetTitle()
        {
            switch (Localizer.CurrentLanguage)
            {
                case "es-es":
                    return "Trayectorias Observatorio";
                case "ru":
                    return "траекториями Наблюдатель";
                case "zh-cn":
                    return "轨迹观察者";
                case "ja":
                    return "軌跡ウォッチャー";
                case "de-de":
                    return "Trajectories Beobachter";
                case "fr-fr":
                    return "Trajectoires Observateur";
                case "it-it":
                    return "Traiettorie Osservatore";
                case "pt-br":
                    return "Trajetórias Observador";
                default:
                    return "Trajectories Watcher";
            }
        }

        private static void OnButtonClick_Reset()
        {
            foreach (KeyValuePair<string, Entry> e in entries)
            {
                e.Value.value = 0d;
                e.Value.vector = Vector3d.zero;
                e.Value.value_txt = "";
            }

            tot_frames = 0L;
        }

        private static void OnButtonClick_ShowZero(bool inState) => show_zero = inState;

        private static void AddDialogItem(KeyValuePair<string, Entry> entry)
        {
            if (!Ready)
                return;

            string e_name = entry.Key;
            //Util.DebugLog("{0}: {1}", e_name, dialog_items.children.Count.ToString());

            // add item
            dialog_items.AddChild(
                new DialogGUIHorizontalLayout(
                    new DialogGUILabel("  " + e_name, true),
                    new DialogGUILabel(() => { return entries[e_name].value_txt; }, VALUE_WIDTH)));

            // required to force the Gui creation
            Stack<Transform> stack = new Stack<Transform>();
            stack.Push(dialog_items.uiItem.gameObject.transform);
            dialog_items.children[dialog_items.children.Count - 1].Create(ref stack, HighLogic.UISkin);
            entry.Value.in_gui = true;
        }
#endif

        /// <summary> Add a watcher entry. </summary>
        [Conditional("DEBUG_WATCHER")]
        public static void Watch(string e_name, double value)
        {
#if DEBUG_WATCHER
            if (entries == null)
                return;

            if (!entries.ContainsKey(e_name))
            {
                entries.Add(e_name, new Entry());
#if WATCHER_TELEMETRY
                if (!channels.Contains(e_name))
                    channels.Add(e_name);
#endif
            }

            entries[e_name].type = Type.DOUBLE;
            entries[e_name].value = value;
#endif
        }

        /// <summary> Add a watcher entry. </summary>
        [Conditional("DEBUG_WATCHER")]
        public static void Watch(string e_name, Vector3d vector)
        {
#if DEBUG_WATCHER
            if (entries == null)
                return;

            if (!entries.ContainsKey(e_name))
            {
                entries.Add(e_name, new Entry());
#if WATCHER_TELEMETRY
                if (!channels.Contains(e_name))
                    channels.Add(e_name);
#endif
            }

            entries[e_name].type = Type.VECTOR3D;
            entries[e_name].vector = vector;
#endif
        }

        /// <summary> Add a watcher entry. </summary>
        [Conditional("DEBUG_WATCHER")]
        public static void Watch(string e_name, string text)
        {
#if DEBUG_WATCHER
            if (entries == null)
                return;

            if (!entries.ContainsKey(e_name))
            {
                entries.Add(e_name, new Entry());
#if WATCHER_TELEMETRY
                if (!channels.Contains(e_name))
                    channels.Add(e_name);
#endif
            }

            entries[e_name].type = Type.STRING;
            entries[e_name].value_txt = text;
#endif
        }
    }

} // Trajectories
