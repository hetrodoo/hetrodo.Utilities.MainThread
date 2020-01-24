using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace hetrodo.Utilities
{
    public class MainThread
    {
        #region Other
        public delegate void OnExceptionCaughtEvent(Exception ex);

        public class RedundancyException : Exception
        {
            public RedundancyException()
            {
            }
            public RedundancyException(string message) : base(message)
            {
            }
        }
        #endregion

        #region Public
        public static bool IsRunning { get { return Instance != null; } }
        public static OnExceptionCaughtEvent OnExceptionCaught;

        public MainThread()
        {
            if (IsRunning)
                return;

            Instance = this;
            ActionQueue = new List<Action>();
            UnityMainThread = Thread.CurrentThread;
            waitHandle = new EventWaitHandle(true, EventResetMode.AutoReset);

            HandleRequests();
        }

        public static void Exec(Action a)
        {
            if (Instance == null)
                throw new NullReferenceException("MainThread was not initialized.");

            if (Instance.UnityMainThread == Thread.CurrentThread)
                throw new RedundancyException("You are calling the main thread from itself.");

            Instance.ActionQueue.Add(a);
            Instance.waitHandle.WaitOne();
        }
        #endregion

        #region Private
        private readonly EventWaitHandle waitHandle;
        private readonly List<Action> ActionQueue;
        private readonly Thread UnityMainThread;
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
                        var count = ActionQueue.Count;

                        //Execute calls
                        for (int i = 0; i < count; i++)
                            try { ActionQueue[i].Invoke(); } catch (Exception ex) { OnExceptionCaught?.Invoke(ex); }

                        //Clear pool before releasing threads
                        ActionQueue.Clear();

                        //Release threads
                        for (int i = 0; i < count; i++)
                            waitHandle.Set();
                    }

                    await Task.Delay(25);
                }
            }
            catch (Exception ex)
            {
                OnExceptionCaught?.Invoke(ex);

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
