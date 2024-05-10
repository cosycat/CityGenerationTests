using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FreeFormGraph.World;
using JetBrains.Annotations;
using UnityEngine;
using Random = System.Random;

namespace FreeFormGraph.Agents {
    public class AgentManager : MonoBehaviour {
        
        /// <summary>
        /// The target frames per second the agents should run at.
        ///
        /// If the agent is ever faster than this, it will wait until starting the next frame.
        /// </summary>
        public int TargetFramesPerSecond { get; set; } = 1;
        
        private float TargetFrameTimeSeconds => 1f / TargetFramesPerSecond;
        
        public static AgentManager Instance { get; private set; }

        [ItemNotNull] private readonly List<(IAgent agent, int framesSinceWorked)> agents = new();

        private readonly object stopRequestLock = new();

        private int currCycleCounter = 0;
        private DateTime lastFrameTime = DateTime.Now;
        private int currAgentIndex = -1;
        internal IAgent CurrAgent => agents[currAgentIndex].agent;
        internal int CurrAgentFramesSinceWorked => agents[currAgentIndex].framesSinceWorked;

        internal bool IsAgentRunning => cancellationTokenSource != null;

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
                random = new Random(1337),
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

        private void HandleNextAgent(float timeToWaitSeconds = 0) {
            // check if we are ready to start the next agent
            if (IsAgentRunning) return;
            if (agents.Count == 0) {
                Debug.Log("No agents to run. Stopped AgentManager.");
                return;
            }

            // get next agent and handle frame time
            currAgentIndex = (currAgentIndex + 1) % agents.Count;
            if (currAgentIndex == 0) {
                currCycleCounter++;
                var timeSinceLastFrame = DateTime.Now - lastFrameTime;
                timeToWaitSeconds = (float)(TargetFrameTimeSeconds - timeSinceLastFrame.TotalSeconds); // set wait time, if the previous frame was too fast
                lastFrameTime = DateTime.Now;
                Debug.Log($"Cycle {currCycleCounter} started. Waiting {timeToWaitSeconds} seconds. {agents.Count} agents to run. {TargetFrameTimeSeconds} seconds per frame.");
            } else {
                timeToWaitSeconds = 0; // no need to wait, if we are in the same frame as the last agent
            }
            
            // get the agent and start the work, if the agent is ready
            var agent = CurrAgent;
            var framesSinceWorked = CurrAgentFramesSinceWorked;
            if (framesSinceWorked < agent.WorkFrequency) {
                // the agent is not ready this frame, skip to the next agent.
                agents[currAgentIndex] = (agent, framesSinceWorked + 1);
                HandleNextAgent(timeToWaitSeconds);
                return;
            }
            Debug.Assert(agent != null);
            agents[currAgentIndex] = (agent, 0); // reset the frame counter for the agent
            cancellationTokenSource = new CancellationTokenSource();
            
            // run the task, but let it wait if the previous frame was too fast
            var task = Task.Run(() => {
                if (timeToWaitSeconds > 0) {
                    Debug.Log($"Waiting {timeToWaitSeconds} seconds.");
                    Thread.Sleep((int)(timeToWaitSeconds * 1000));
                }
                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                agent.DoWork(cancellationTokenSource.Token, world, context);
            }, cancellationTokenSource.Token);
            
            // once the agent is done, either start the next agent or stop the manager, if requested
            task.ContinueWith(completedTask => {
                lock (stopRequestLock) {
                    cancellationTokenSource = null;
                    
                    if(completedTask.IsFaulted) {
                        var exceptions = completedTask.Exception?.Flatten().InnerExceptions;
                        Debug.LogError("Aborted AgentManager due to unhandled exception in child task");
                        if (exceptions == null) return;
                        foreach (var exception in exceptions) {
                            Debug.LogError(exception.ToString());
                        }
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
            foreach (var agent in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IAgent>()) {
                agents.Add((agent, agent.WorkFrequency));
            }
        }
        
        public void AddNewAgent(IAgent agent) {
            lock (stopRequestLock) {
                agents.Add((agent, agent.WorkFrequency)); // set to frame rate to make sure it is run in the next frame
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