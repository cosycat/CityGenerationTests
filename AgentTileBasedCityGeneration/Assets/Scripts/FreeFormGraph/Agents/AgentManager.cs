using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FreeFormGraph.World;
using JetBrains.Annotations;
using UnityEngine;

namespace FreeFormGraph.Agents {
    public class AgentManager : MonoBehaviour {

        private readonly List<IAgent> agents = new();

        private readonly object stopRequestLock = new();

        private int currAgentIndex = -1;
        private IAgent CurrAgent => agents[currAgentIndex];

        private bool IsAgentRunning => cancellationTokenSource != null;

        [CanBeNull] private CancellationTokenSource cancellationTokenSource = null;

        private bool stopRequested = false;
        [CanBeNull] private Action onStoppedMethod;
        
        private IWorld world;

        private void Start() {
            GenerateAgents();
            HandleNextAgent();
        }

        private void OnDestroy() {
            RequestStopAgents(() => { Debug.Log("OnDestroy stopped");});
        }

        private void HandleNextAgent() {
            Debug.Log("HandleNextAgent");
            if (IsAgentRunning) return;
            Debug.Log("HandleNextAgent - no agent running");
            currAgentIndex = (currAgentIndex + 1) % agents.Count;
            var agent = CurrAgent;
            cancellationTokenSource = new CancellationTokenSource();
            var task = Task.Run(() => agent.DoWork(cancellationTokenSource.Token, world), cancellationTokenSource.Token);
            task.ContinueWith(_ => {
                Debug.Log($"Continue With (stopRequested: {stopRequested})");
                lock (stopRequestLock) {
                    cancellationTokenSource = null;
                    if (stopRequested) {
                        onStoppedMethod?.Invoke();
                        onStoppedMethod = null; // to make sure it is not called again
                    }
                    else {
                        HandleNextAgent();
                    }
                }
            });
        }

        private void GenerateAgents() {
            agents.Add(new TestAgentPathFinding());
        }

        

        public void RequestStopAgents(Action onStopped) {
            lock (stopRequestLock) {
                if (!IsAgentRunning) return;
                stopRequested = true;
                cancellationTokenSource!.Cancel();
                this.onStoppedMethod = onStopped;
            }
        }

        public void RestartAgents() {
            lock (stopRequestLock) {
                if (IsAgentRunning) return;
                Debug.Assert(cancellationTokenSource == null, $"cancellationTokenSource was not null");
                stopRequested = false;
                this.onStoppedMethod = null;
                HandleNextAgent();
            }
        }

        
    }
}