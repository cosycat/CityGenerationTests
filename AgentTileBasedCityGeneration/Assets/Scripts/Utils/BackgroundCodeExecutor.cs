#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Utils {
    public class BackgroundCodeExecutor : MonoBehaviour {
        private readonly
            List<(Task task, Action? onComplete, Action<Exception>? onError, Action? onCancel, CancellationTokenSource
                cancelToken)> runningTasks = new();

        private readonly
            List<(Task task, Action? onComplete, Action<Exception>? onError, Action? onCancel, CancellationTokenSource
                cancelToken)> finishedTasks = new();

        private readonly object runningTasksLock = new();
        private readonly object finishedTasksLock = new();

        private static BackgroundCodeExecutor? instance;

        private static BackgroundCodeExecutor Instance {
            get {
                if (instance != null) return instance;
                instance = new GameObject("BackgroundCodeExecutor").AddComponent<BackgroundCodeExecutor>();
                DontDestroyOnLoad(instance.gameObject);
                return instance;
            }
        }

        private async void OnDestroy() {
            while (runningTasks.Count > 0) {
                var (task, _, _, _, cancelToken) = runningTasks[0];
                try {
                    cancelToken.Cancel();
                    await task;
                }
                catch (OperationCanceledException e) {
                    Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e.Message}");
                }
                finally {
                    task.Dispose();
                    Debug.Log("Task stopped and disposed");
                }
            }

            lock (finishedTasksLock) {
                foreach (var (task, _, _, _, _) in finishedTasks) {
                    task.Dispose();
                    Debug.Log("Task disposed");
                }
            }
        }

        /// <summary>
        /// Executes the given action in the background, on a different thread.
        ///
        /// Make sure that the actionToExecute is thread-safe, as it will be executed on a different thread.
        /// Make sure that actionToExecute calls cancelToken.ThrowIfCancellationRequested() from time to time (cancelling the execution).
        /// When you want to manually cancel the execution (for example, when the world has changed and the execution needs to start over), call Cancel() on the returned <see cref="BackgroundTask"/>.
        /// </summary>
        /// <param name="actionToExecute"> The action to execute in the background. </param>
        /// <param name="onComplete"> The action to execute when the background action is completed. Will be executed on the main thread (in an Update function) </param>
        /// <param name="onError"> The optional action to execute when an error occurs during the background action. Will be executed on the main thread (in an Update function) </param>
        /// <param name="onCancel"> The optional action to execute when the background action is canceled via the
        /// <see cref="CancellationTokenSource"/> and the actionToExecute has thrown via ThrowIfCancellationRequested(). Will be executed on the main thread (in an Update function) </param>
        /// <returns> The <see cref="BackgroundTask"/> that can be used to cancel the execution. </returns>
        public static BackgroundTask ExecuteInBackground(Action<CancellationToken> actionToExecute,
            Action? onComplete = null, Action<Exception>? onError = null, Action? onCancel = null) {
            // TODO pass in an object that can be used as an identifier, and all tasks with the same object will be run sequentially
            var executor = Instance;
            var cancelTokenSource = new CancellationTokenSource();
            var cancelToken = cancelTokenSource.Token;
            var task = Task.Run(() => actionToExecute(cancelToken), cancelToken);
            lock (executor.runningTasksLock) {
                executor.runningTasks.Add((task, onComplete, onError, onCancel, cancelTokenSource));
            }

            task.ContinueWith(_ => {
                lock (executor.runningTasksLock) {
                    executor.runningTasks.Remove((task, onComplete, onError, onCancel, cancelTokenSource));
                }

                lock (executor.finishedTasksLock) {
                    executor.finishedTasks.Add((task, onComplete, onError, onCancel, cancelTokenSource));
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
            return new BackgroundTask(cancelTokenSource, task);
        }

        // public static BackgroundTask ExecuteInBackgroundOrdered(List<Action<CancellationToken>> actionsToExecute, Action onComplete, Action<Exception>? onError = null, Action? onCancel = null) {
        //     return ExecuteInBackground((token => {
        //         foreach (var action in actionsToExecute) {
        //             action(token);
        //             token.ThrowIfCancellationRequested();
        //         }
        //     }), onComplete, onError, onCancel);
        // }


        private void Update() {
            lock (finishedTasksLock) {
                foreach (var (task, onComplete, onError, onCancel, cancelToken) in finishedTasks) {
                    Debug.Assert(
                        cancelToken.IsCancellationRequested == task.IsCanceled && cancelToken.IsCancellationRequested ==
                        (task.Status == TaskStatus.Canceled),
                        $"Cancel token is {cancelToken.IsCancellationRequested}, task is canceled {task.IsCanceled}, task status is {task.Status}");
                    switch (task.Status) {
                        case TaskStatus.Canceled:
                            onCancel?.Invoke();
                            break;
                        case TaskStatus.Created:
                            Debug.LogError("Task was created, but should have been started and completed");
                            break;
                        case TaskStatus.Faulted:
                            onError?.Invoke(task.Exception);
                            break;
                        case TaskStatus.RanToCompletion:
                            onComplete?.Invoke();
                            break;
                        case TaskStatus.Running:
                            Debug.LogError("Task is still running, but should have been completed");
                            break;
                        case TaskStatus.WaitingForActivation:
                            Debug.LogError("Task is waiting for activation, but should have been completed");
                            break;
                        case TaskStatus.WaitingForChildrenToComplete:
                            Debug.LogError("Task is waiting for children to complete, but should have been completed");
                            break;
                        case TaskStatus.WaitingToRun:
                            Debug.LogError("Task is waiting to run, but should have been completed");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                finishedTasks.Clear();
            }
        }
    }

    /// <summary>
    /// Represents a background task that can be canceled.
    /// </summary>
    public class BackgroundTask {
        private readonly CancellationTokenSource cancelTokenSource;
        private Task Task { get; }
        public TaskStatus Status => Task.Status;

        public BackgroundTask(CancellationTokenSource cancelTokenSource, Task task) {
            this.cancelTokenSource = cancelTokenSource;
            Task = task;
        }

        public void Cancel() {
            cancelTokenSource.Cancel();
        }

        public void CancelAfter(TimeSpan timeSpan) {
            cancelTokenSource.CancelAfter(timeSpan);
        }
    }
}