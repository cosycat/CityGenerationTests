using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FreeFormGraph.World;
using JetBrains.Annotations;
using UnityEngine;

namespace FreeFormGraph.Agents {
    public class AgentManager : MonoBehaviour {
        
        public static AgentManager Instance { get; private set; }

        [ItemNotNull] private readonly List<IAgent> agents = new();

        private readonly object stopRequestLock = new();

        private int currAgentIndex = -1;
        private IAgent CurrAgent => agents[currAgentIndex];

        private bool IsAgentRunning => cancellationTokenSource != null;

        [CanBeNull] private CancellationTokenSource cancellationTokenSource = null;

        private bool stopRequested = false;
        [CanBeNull] private Action onStoppedMethod;
        
        private IWorld world;

        private Context context;

        private void Awake() {
            if (Instance != null) {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            context = new Context() {
                random = new(1337),
                manager = this
            };
        }

        private void Start() {
            world = FindObjectOfType<WorldGameObject>();
            GenerateAgents();
            HandleNextAgent();
        }

        private void OnDestroy() {
            RequestStopAgents(() => { Debug.Log("OnDestroy stopped");});
        }

        private void OnDisable() {
            RequestStopAgents(() => { Debug.Log("OnDisable stopped");});
        }
        
        private void OnApplicationQuit() {
            RequestStopAgents(() => { Debug.Log("OnApplicationQuit stopped");});
        }

        private void HandleNextAgent() {
            if (IsAgentRunning) return;
            currAgentIndex = (currAgentIndex + 1) % agents.Count;
            var agent = CurrAgent;
            Debug.Assert(agent != null);
            cancellationTokenSource = new CancellationTokenSource();
            var task = Task.Run(() => agent.DoWork(cancellationTokenSource.Token, world, context), cancellationTokenSource.Token);
            task.ContinueWith(completedTask => {

                lock (stopRequestLock) {
                    cancellationTokenSource = null;
                    if(completedTask.IsFaulted) {
                        foreach (var exception in completedTask.Exception.Flatten().InnerExceptions) {
                            Debug.LogError(exception.ToString());
                        }
                        Debug.LogError("Aborted AgentManager due to unhandled exception in child task");
                        return;
                    }
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
            agents.AddRange(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IAgent>());
        }
        
        public void AddNewAgent(IAgent agent) {
            lock (stopRequestLock) {
                agents.Add(agent);
                if (agents.Count == 1) HandleNextAgent(); // If there was no agent before, start now.
            }
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

        public class Context {
            public System.Random random;
            public AgentManager manager;
        }
    }
}