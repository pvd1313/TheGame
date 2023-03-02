/// TaskManager.cs
/// Copyright (c) 2011, Ken Rockot  <k-e-n-@-REMOVE-CAPS-AND-HYPHENS-oz.gs>.  All rights reserved.
/// Added a inline delegate to it. @onevcat
/// Everyone is granted non-exclusive license to do anything at all with this code.

using UnityEngine;
using System.Collections;

namespace GoogleSheetsToUnity
{
    /// A Task object represents a coroutine.  Tasks can be started, paused, and stopped.
    /// It is an error to attempt to start a task that has been stopped or which has
    /// naturally terminated.
    internal class Task
    {
        private readonly TaskManager.TaskState _task;
        
        /// Delegate for termination subscribers.  manual is true if and only if
        /// the coroutine was stopped with an explicit call to Stop().
        public delegate void FinishedHandler(bool manual);

        /// Termination event.  Triggered when the coroutine completes execution.
        public event FinishedHandler Finished;

        /// Creates a new Task object for the given coroutine.
        ///
        /// If autoStart is true (default) the task is automatically started
        /// upon construction.
        public Task(IEnumerator c, bool autoStart = true)
        {
            _task = TaskManager.CreateTask(c);
            _task.Finished += TaskFinished;
            if (autoStart)
                Start();
        }

        public Task(IEnumerator c, FinishedHandler handler, bool autoStart = true) : this(c, autoStart)
        {
            Finished += handler;
        }

        /// Begins execution of the coroutine
        private void Start()
        {
            _task.Start();
        }

        private void TaskFinished(bool manual)
        {
            FinishedHandler handler = Finished;
            if (handler != null)
                handler(manual);
        }
    }

    internal class TaskManager : MonoBehaviour
    {
        public class TaskState
        {
            public delegate void FinishedHandler(bool manual);
            public event FinishedHandler Finished;

            private readonly IEnumerator _coroutine;
            private bool _running;
            private bool _paused;
            private bool _stopped;

            public TaskState(IEnumerator c)
            {
                _coroutine = c;
            }

            public void Start()
            {
                _running = true;
                _singleton.StartCoroutine(CallWrapper());
            }

            private IEnumerator CallWrapper()
            {
                yield return null;
                IEnumerator e = _coroutine;
                while (_running)
                {
                    if (_paused)
                        yield return null;
                    else
                    {
                        if (e != null && e.MoveNext())
                        {
                            yield return e.Current;
                        }
                        else
                        {
                            _running = false;
                        }
                    }
                }

                FinishedHandler handler = Finished;
                if (handler != null)
                    handler(_stopped);
            }
        }
        
        private static TaskManager _singleton;

        public static TaskState CreateTask(IEnumerator coroutine)
        {
            if (_singleton == null)
            {
                GameObject go = new GameObject("TaskManager");
                _singleton = go.AddComponent<TaskManager>();
                go.hideFlags = HideFlags.HideAndDontSave;
            }
            return new TaskState(coroutine);
        }
    }
}