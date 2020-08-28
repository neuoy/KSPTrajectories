/*
  Worker.cs : Worker thread manager.
  Copyright© (c) 2012-2020 Piez Software.
  All rights reserved.

  Redistribution and use in source and binary forms, with or without modification,
  are permitted provided that the following conditions are met:

  1. This is free software, and you are welcome to redistribute it under certain conditions,
     as outlined in the full content of the GNU General Public License (GNU GPL), version 3

  2. Redistributions of source code must retain the above copyright notice, this
     list of conditions and the following disclaimer.

  3. Redistributions in binary form must reproduce the above copyright notice,
     this list of conditions and the following disclaimer in the documentation
     and/or other materials provided with the distribution.

  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
  ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
  WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
  DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
  FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
  DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
  SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
  CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
  OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
  OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

  Supplied and modified for Trajectories by PiezPiedPy.
*/

using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Trajectories
{
    #region EVENT_DELEGATES
    internal delegate void WorkerReportEventHandler(Worker.EVENT_TYPE type, int progress_percentage = 0);
    internal delegate void WorkerUpdateEventHandler(Worker.JOB job, bool result);
    internal delegate void WorkerErrorEventHandler(Worker.JOB job, Exception error);
    #endregion

    ///<summary> Background worker classes and methods </summary>
    internal static class Worker
    {
        #region CONSTANT_VALUES

        #region ENUMS
        ///<summary> Worker job types </summary>
        internal enum JOB
        {
            NO_JOB = 0,
            COMPUTE_PATCHES,
        }

        ///<summary> Worker event types </summary>
        internal enum EVENT_TYPE
        {
            ///<summary> Progress percentage report </summary>
            PERCENTAGE = 0,
            ///<summary> Update StatusBar </summary>
            STATUSBAR,
            ///<summary> Update percentage, form buttons and StatusBar </summary>
            ALL,
            ///<summary> App is closing </summary>
            APPCLOSE
        }
        #endregion

        #endregion

        #region INTERNAL_FIELDS
        ///<summary> The main threads Scheduler </summary>
        private static TaskScheduler main_scheduler;

        ///<summary> Workers main thread </summary>
        internal static BackgroundWorker Thread { get; private set; } = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
        ///<summary> Current job being processed </summary>
        internal static JOB CurrentJob { get; set; } = JOB.NO_JOB;
        ///<summary> If true then the worker thread is busy </summary>
        internal static bool Busy => Thread != null ? Thread.IsBusy && (CurrentJob != JOB.NO_JOB) : true;
        ///<summary> If set to true then application is closing and waiting for the worker to finish, the worker should call MainForm.Close() when it has finished </summary>
        internal static bool Closing { get; set; } = false;
        ///<summary> True if devices should connect to their vehicle network on the next worker job </summary>
        internal static bool ConnectNetworks { get; set; } = true;
        ///<summary> If set to true then the devices form is waiting for the worker to finish, the worker should call DevicesForm.WorkerFinished() when it has finished </summary>
        internal static bool DevicesFormWaiting { get; set; } = false;
        #endregion

        #region EVENTS
        ///<summary> Event raised when the current Worker job has a Progress report </summary>
        internal static event WorkerReportEventHandler OnReport;
        ///<summary> Event raised when the current Worker job has an update to data </summary>
        internal static event WorkerUpdateEventHandler OnUpdate;
        ///<summary> Event raised when the current Worker job has encountered an error </summary>
        internal static event WorkerErrorEventHandler OnError;
        #endregion

        #region CONSTRUCTOR_DESTRUCTOR
        ///<summary> Initializes the Worker background thread </summary>
        internal static void Initialize()
        {
            Util.DebugLog("");
            main_scheduler = TaskScheduler.FromCurrentSynchronizationContext();

            Thread.DoWork += new DoWorkEventHandler(DoWork);
            Thread.ProgressChanged += new ProgressChangedEventHandler(ProgressChanged);
            Thread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Completed);
        }

        ///<summary> Clean up any resources being used </summary>
        internal static void Dispose()
        {
            Util.DebugLog("");

            if (Thread != null)
            {
                Thread.CancelAsync();
                Thread.Dispose();
            }

            Thread = null;
        }
        #endregion

        #region METHODS
        ///<summary> Cancels the currently running worker thread </summary>
        internal static void Cancel()
        {
            if (Thread != null)
                Thread.CancelAsync();
        }

        /// <summary> Code that is executed inside the worker thread </summary>
        private static void DoWork(object sender, DoWorkEventArgs e)
        {
            Util.DebugLog("{0}", ((JOB)e.Argument).ToString());
            CurrentJob = (JOB)e.Argument;
            //DeviceManager.CONNECT_MODE connect_mode = DeviceManager.CONNECT_MODE.NORMAL;

            //ECU_RipperPC.Update_AllButtons(false);      // change to an event maybe ??

            if (Thread.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            //Thread.ReportProgress(10);

            switch (CurrentJob)
            {
                case JOB.COMPUTE_PATCHES:
                    Trajectory.ComputeTrajectoryPatches();
                    break;
                //case JOB.RESET_DEVICES:
                    //DeviceManager.ResetDevices();
                    //return;
                //case JOB.RESET_NET:
                    //connect_mode = DeviceManager.CONNECT_MODE.RESET;
                    //DeviceManager.DetectDevices();
                    //break;
                //case JOB.CHANGE_PROTOCOL:
                    //connect_mode = DeviceManager.CONNECT_MODE.CHANGE;
                    //DeviceManager.DetectDevices();
                    //break;
            }
            if (Thread.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

            // Vehicle Network Connection and AutoDetect Management
            /*if (DeviceManager.ConnectedDevices.Count > 0)
            {
                if (ConnectNetworks)
                {
                    Thread.ReportProgress(20, true);

                    switch (connect_mode)
                    {
                        case DeviceManager.CONNECT_MODE.RESET:
                            DeviceManager.CurrentDevice.ResetNetwork();
                            break;
                        case DeviceManager.CONNECT_MODE.CHANGE:
                            DeviceManager.CurrentDevice.ChangeProtocol();
                            break;
                        default:
                            DeviceManager.ConnectNetworks();
                            break;
                    }

                    //DeviceManager.CurrentDevice.Update();      // change to an event maybe ??
                    ConnectNetworks = false;
                    if (DeviceManager.AutoDetecting)
                    {
                        Thread.ReportProgress(30, true);
                        DateTime auto_detect_time = DateTime.Now;
                        while (DeviceManager.AutoDetecting && (DateTime.Now < (auto_detect_time + DeviceManager.AutoDetectTime)))
                        {
                            if (Thread.CancellationPending)
                            {
                                e.Cancel = true;
                                return;
                            }
                            DeviceManager.ConnectNetworks();
                            //DeviceManager.CurrentDevice.Update();      // change to an event maybe ??
                            Thread.ReportProgress((int)(30 + ((DateTime.Now - auto_detect_time).TotalSeconds * 2)));
                        }
                        double detect_time = (DateTime.Now - auto_detect_time).TotalMilliseconds;
                        if (detect_time != 0)
                            Util.DebugLog("Auto detect time {0}", ((detect_time / 1000) > 1 ?
                                string.Format("{0:F1} seconds", detect_time / 1000) : string.Format("{0:F1} ms", detect_time)));
                    }
                }
                //if (DeviceManager.DoDevicesPassBack)
                // DeviceManager.CurrentDevice.Update();      // change to an event maybe ??
            }
            else
            {
            }  */
            Thread.ReportProgress(90);
            if (Thread.CancellationPending)
                e.Cancel = true;
        }

        /// <summary> Event method that is invoked when the worker thread has completed been cancelled or on error </summary>
        private static void Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((e.Error != null))
            {
                Util.DebugLog("Error detected");
                OnError(CurrentJob, e.Error);
            }
            else if (e.Cancelled)
            {
                Util.DebugLog("Job cancelled");
            }
            else
            {
                // Operation succeeded.
                OnReport(EVENT_TYPE.PERCENTAGE, 100);
                Util.DebugLog("ProgressChanged: 100%");

                OnUpdate(CurrentJob, (bool)(e.Result ?? false));
            }

            // is application closing
            if (Closing)
            {
                OnReport(EVENT_TYPE.APPCLOSE);
                return;
            }

            OnReport(EVENT_TYPE.ALL);
            CurrentJob = JOB.NO_JOB;

            Util.DebugLog("Done.");

            //if (DevicesFormWaiting)
            //DevicesForm.Instance.WorkerFinished();
        }

        /// <summary> Event method that is invoked when the worker thread needs to report any progress data </summary>
        private static void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Util.DebugLog("{0}%", e.ProgressPercentage);
            OnReport(EVENT_TYPE.PERCENTAGE, e.ProgressPercentage);
            if (e.UserState != null)
            {
                Util.DebugLog("Update Data");
                OnReport(EVENT_TYPE.STATUSBAR);
                //if (DevicesForm.Exists)
                //DevicesForm.Instance.UpdateData();
            }
        }
        #endregion
    }
}
