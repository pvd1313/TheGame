//Provided by https://gist.github.com/LotteMakesStuff/16b5f2fc108f9a0201950c797d53cfbf

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Networking;

namespace GoogleSheetsToUnity
{
    public class EditorCoroutineRunner
    {
        private static List<EditorCoroutineState> _coroutineStates;
        private static List<EditorCoroutineState> _finishedThisUpdate;
        private static EditorCoroutineState _uiCoroutineState;
        
        public static EditorCoroutine StartCoroutine(IEnumerator coroutine)
        {
            return StoreCoroutine(new EditorCoroutineState(coroutine));
        }

        // Creates objects to manage the coroutines lifecycle and stores them away to be processed
        private static EditorCoroutine StoreCoroutine(EditorCoroutineState state)
        {
            if (_coroutineStates == null)
            {
                _coroutineStates = new List<EditorCoroutineState>();
                _finishedThisUpdate = new List<EditorCoroutineState>();
            }

            if (_coroutineStates.Count == 0)
                EditorApplication.update += Runner;

            _coroutineStates.Add(state);

            return state.editorCoroutineYieldInstruction;
        }

        // Manages running active coroutines!
        private static void Runner()
        {
            // Tick all the coroutines we have stored
            for (var index = 0; index < _coroutineStates.Count; index++)
            {
                var state = _coroutineStates[index];
                
                TickState(state);
            }

            // If a coroutine was finished whilst we were ticking, clear it out now
            foreach (var state in _finishedThisUpdate)
            {
                _coroutineStates.Remove(state);

                if (_uiCoroutineState == state)
                {
                    _uiCoroutineState = null;
                    EditorUtility.ClearProgressBar();
                }
            }
            _finishedThisUpdate.Clear();

            // Stop the runner if were done.
            if (_coroutineStates.Count == 0)
            {
                EditorApplication.update -= Runner;
            }
        }

        private static void TickState(EditorCoroutineState state)
        {
            if (state.IsValid)
            {
                // This coroutine is still valid, give it a chance to tick!
                state.Tick();

                // If this coroutine is the active UI coroutine, give it a chance to update the UI
                if (state.showUI && _uiCoroutineState == state)
                {
                    _uiCoroutineState.UpdateUI();
                }
            }
            else
            {
                // We have finished running the coroutine, lets scrap it
                _finishedThisUpdate.Add(state);
            }
        }
    }

    internal class EditorCoroutineState
    {
        private IEnumerator coroutine;
        public bool IsValid
        {
            get { return coroutine != null; }
        }
        public EditorCoroutine editorCoroutineYieldInstruction;

        // current state
        private object current;
        private Type currentType;
        private float timer; // for WaitForSeconds support    
        private EditorCoroutine nestedCoroutine; // for tracking nested coroutines that are not started with EditorCoroutineRunner.StartCoroutine
        private DateTime lastUpdateTime;

        // UI
        public bool showUI;
        private bool cancelable;
        private bool canceled;
        private string title;
        public string Label;
        public float PercentComplete;

        public EditorCoroutineState(IEnumerator coroutine)
        {
            this.coroutine = coroutine;
            editorCoroutineYieldInstruction = new EditorCoroutine();
            showUI = false;
            lastUpdateTime = DateTime.Now;
        }

        public void Tick()
        {
            if (coroutine != null)
            {
                // First check if we have been canceled by the UI. If so, we need to stop before doing any wait processing
                if (canceled)
                {
                    Stop();
                    return;
                }

                // Did the last Yield want us to wait?
                bool isWaiting = false;
                var now = DateTime.Now;
                if (current != null)
                {
                    if (currentType == typeof(WaitForSeconds))
                    {
                        // last yield was a WaitForSeconds. Lets update the timer.
                        var delta = now - lastUpdateTime;
                        timer -= (float)delta.TotalSeconds;

                        if (timer > 0.0f)
                        {
                            isWaiting = true;
                        }
                    }
                    else if (currentType == typeof(WaitForEndOfFrame) || currentType == typeof(WaitForFixedUpdate))
                    {
                        // These dont make sense in editor, so we will treat them the same as a null return...
                        isWaiting = false;
                    }
                    else if (currentType == typeof(UnityWebRequestAsyncOperation))
                    {
                        var webRequest = current as UnityWebRequestAsyncOperation;
                        if (!webRequest.isDone)
                        {
                            isWaiting = true;
                        }
                    }
                    else if (currentType.IsSubclassOf(typeof(CustomYieldInstruction)))
                    {
                        // last yield was a custom yield type, lets check its keepWaiting property and react to that
                        var yieldInstruction = current as CustomYieldInstruction;
                        if (yieldInstruction.keepWaiting)
                        {
                            isWaiting = true;
                        }
                    }
                    else if (currentType == typeof(EditorCoroutine))
                    {
                        // Were waiting on another coroutine to finish
                        var editorCoroutine = current as EditorCoroutine;
                        if (!editorCoroutine.HasFinished)
                        {
                            isWaiting = true;
                        }
                    }
                    else if (typeof(IEnumerator).IsAssignableFrom(currentType))
                    {
                        // if were just seeing an enumerator lets assume that were seeing a nested coroutine that has been passed in without calling start.. were start it properly here if we need to
                        if (nestedCoroutine == null)
                        {
                            nestedCoroutine = EditorCoroutineRunner.StartCoroutine(current as IEnumerator);
                            isWaiting = true;
                        }
                        else
                        {
                            isWaiting = !nestedCoroutine.HasFinished;
                        }

                    }
                    else if (currentType == typeof(Coroutine))
                    {
                        // UNSUPPORTED
                        Debug.LogError("Nested Coroutines started by Unity's defaut StartCoroutine method are not supported in editor! please use EditorCoroutineRunner.Start instead. Canceling.");
                        canceled = true;
                    }
                    else
                    {
                        // UNSUPPORTED
                        Debug.LogError("Unsupported yield (" + currentType + ") in editor coroutine!! Canceling.");
                        canceled = true;
                    }
                }
                lastUpdateTime = now;

                // have we been canceled?
                if (canceled)
                {
                    Stop();
                    return;
                }

                if (!isWaiting)
                {
                    // nope were good! tick the coroutine!
                    bool update = coroutine.MoveNext();

                    if (update)
                    {
                        // yup the coroutine returned true so its been ticked...

                        // lets see what it actually yielded
                        current = coroutine.Current;
                        if (current != null)
                        {
                            // is it a type we have to do extra processing on?
                            currentType = current.GetType();

                            if (currentType == typeof(WaitForSeconds))
                            {
                                // its a WaitForSeconds... lets use reflection to pull out how long the actual wait is for so we can process the wait
                                var wait = current as WaitForSeconds;
                                FieldInfo m_Seconds = typeof(WaitForSeconds).GetField("m_Seconds", BindingFlags.NonPublic | BindingFlags.Instance);
                                if (m_Seconds != null)
                                {
                                    timer = (float)m_Seconds.GetValue(wait);
                                }
                            }
                            else if (currentType == typeof(EditorStatusUpdate))
                            {
                                // Special case yield that wants to update the UI!
                                var updateInfo = current as EditorStatusUpdate;
                                if (updateInfo.HasLabelUpdate)
                                {
                                    Label = updateInfo.Label;
                                }
                                if (updateInfo.HasPercentUpdate)
                                {
                                    PercentComplete = updateInfo.PercentComplete;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Coroutine returned false so its finally finished!!
                        Stop();
                    }
                }
            }
        }

        private void Stop()
        {
            // Coroutine has finished! do some cleanup...
            coroutine = null;
            editorCoroutineYieldInstruction.HasFinished = true;
        }

        public void UpdateUI()
        {
            if (cancelable)
            {
                canceled = EditorUtility.DisplayCancelableProgressBar(title, Label, PercentComplete);
                if (canceled)
                    Debug.Log("CANCLED");
            }
            else
            {
                EditorUtility.DisplayProgressBar(title, Label, PercentComplete);
            }
        }
    }

    /// <summary>
    /// Coroutine Yield instruction that allows an Editor Coroutine to update the Coroutine runner UI
    /// </summary>
    public class EditorStatusUpdate : CustomYieldInstruction
    {
        public string Label;
        public float PercentComplete;

        public bool HasLabelUpdate;
        public bool HasPercentUpdate;

        // Always go to the next update
        public override bool keepWaiting => false;
    }

    /// <summary>
    /// Created when an Editor Coroutine is started, can be yielded to to allow another coroutine to finish first.
    /// </summary>
    public class EditorCoroutine : YieldInstruction
    {
        public bool HasFinished;
    }
}
#endif