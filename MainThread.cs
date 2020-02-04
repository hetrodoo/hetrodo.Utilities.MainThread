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
            syncWaitHandle = new EventWaitHandle(true, EventResetMode.AutoReset);
            mainWaitHandle = new EventWaitHandle(true, EventResetMode.AutoReset);

            HandleRequests();
        }

        public static void Exec(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            if (Instance == null)
                throw new NullReferenceException("MainThread was not initialized.");

            if (Instance.UnityMainThread == Thread.CurrentThread)
                throw new RedundancyException("You are calling the main thread from itself.");

            action += () => { Instance.syncWaitHandle.Set(); };

            Instance.ActionQueue.Add(action);
            WaitHandle.SignalAndWait(Instance.mainWaitHandle, Instance.syncWaitHandle);
        }

        public static void ExecAsync(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            if (Instance == null)
                throw new NullReferenceException("MainThread was not initialized.");

            if (Instance.UnityMainThread == Thread.CurrentThread)
                throw new RedundancyException("You are calling the main thread from itself.");

            Instance.ActionQueue.Add(action);
            Instance.mainWaitHandle.Set();
        }
        #endregion

        #region Private
        private readonly EventWaitHandle syncWaitHandle;
        private readonly EventWaitHandle mainWaitHandle;
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
                    await WaitForHandle(mainWaitHandle, 25);

                    while (ActionQueue.Count != 0)
                    {
                        try { ActionQueue[0]?.Invoke(); } catch (Exception ex) { OnExceptionCaught?.Invoke(ex); }
                        ActionQueue.RemoveAt(0);
                    }
                }
            }
            catch (Exception ex)
            {
                OnExceptionCaught?.Invoke(ex);
                HandleRequests();
            }
        }

        private static Task WaitForHandle(EventWaitHandle waitHandle, int timeout = 25)
        {
            var waitTask = new Task(() => waitHandle.WaitOne(timeout));
            waitTask.Start();

            return waitTask;
        }
        #endregion
    }
}
