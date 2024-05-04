using System;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace Utils {
    public class BackgroundCodeExecutorTester : MonoBehaviour {
        
        // private void Update() {
        //     Debug.Log("Update");
        // }

        private void Start() {
            // Testing the BackgroundCodeExecutor
            Debug.Log("Start");
            ScheduleLongMethod(false);
            ScheduleLongMethod(true);
            var cancellationTokenSource = ScheduleLongMethod(false);
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(0.5f));
        }

        private static CancellationTokenSource ScheduleLongMethod(bool throwException = false) {
            var sum = 0L;
            return BackgroundCodeExecutor.ExecuteInBackground((cancelToken) => {
                Debug.Log("Task started");
                sum = 0L;
                for (long i = 0; i < 10_000_000_000; i++) {
                    // Debug.Log($"Task iteration {i}");
                    sum += i;
                    if (cancelToken.IsCancellationRequested) {
                        Debug.Log("Task is being canceled");
                        cancelToken.ThrowIfCancellationRequested();
                    }
                    if (throwException && i == 5_000_000_000) {
                        throw new System.Exception("Exception in task");
                    }
                }
                Debug.Log("Task done: " + sum);
            }, () => {
                var go = new GameObject("Finished BackgroundCodeExecutor Task");
                Debug.Log("OnComplete: " + sum);
            }, exception => {
                Debug.Log("OnError: " + exception);
            }, () => {
                Debug.Log("OnCancel: " + sum);
            });
        }
    }
}