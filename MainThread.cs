using System;
using UnityEngine;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace hetrodo.Utilities
{
    public class MainThread
    {
        #region Public
        public static bool IsRunning { get { return Instance != null; } }

        public MainThread()
        {
            if (IsRunning)
                return;

            Instance = this;
            ActionQueue = new List<Action>();
            waitHandle = new EventWaitHandle(true, EventResetMode.AutoReset);

            HandleRequests();
        }

        public static void Exec(Action a)
        {
            if (Instance == null)
            {
                Debug.LogError("Initialization required.");
                return;
            }

            Instance.ActionQueue.Add(a);
            Instance.waitHandle.WaitOne(1000);
        }
        #endregion

        #region Private
        private readonly EventWaitHandle waitHandle;
        private readonly List<Action> ActionQueue;
        private static MainThread Instance;

        private async void HandleRequests()
        {
            //Just in case, you know.
            try
            {
                while (true)
                {
                    if (ActionQueue.Count > 0)
                    {
                        for (int i = 0; i < ActionQueue.Count; i++)
                        {
                            //Avoid outside error messing up with the MainThread handler.
                            try { ActionQueue[i].Invoke(); } catch (Exception ex) { Debug.LogError(ex); }
                            waitHandle.Set();
                        }

                        ActionQueue.Clear();
                    }

                    await Task.Delay(Mathf.Clamp(25, 1, 1000));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);

                Debug.Log("[MainThread Fail Safe]: Restarting in 1 second.");
                await Task.Delay(1000);
                HandleRequests();
            }
        }
        #endregion

        #region Timing
        public static class Timing
        {
            private static readonly Dictionary<Thread, DateTime> threads = new Dictionary<Thread, DateTime>();

            private static readonly EventWaitHandle analyzerWaitHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
            private static readonly Thread analyzer = new Thread(() =>
            {
                while (true)
                {
                    threads.Keys.ToList().ForEach(thread =>
                    {
                        if (!thread.IsAlive)
                            threads.Remove(thread);
                    });

                    analyzerWaitHandle.WaitOne(1000);
                    analyzerWaitHandle.Reset();
                }
            });

            public static float DeltaTime
            {
                get
                {
                    var thread = Thread.CurrentThread;
                    analyzerWaitHandle.Set();

                    if (threads.ContainsKey(thread))
                    {
                        var t = (float)(DateTime.Now - threads[thread]).TotalSeconds;
                        threads[thread] = DateTime.Now;

                        return t;
                    }
                    else
                    {
                        threads.Add(thread, DateTime.Now);

                        if (!analyzer.IsAlive)
                            analyzer.Start();

                    }

                    return 0;
                }
            }
        }
        #endregion
    }
}
