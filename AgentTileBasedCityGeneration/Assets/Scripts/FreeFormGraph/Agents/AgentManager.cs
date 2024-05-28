#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FreeFormGraph.World;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace FreeFormGraph.Agents {
    public class AgentManager : MonoBehaviour {

        [SerializeField] private bool useSeed = true;
        [SerializeField] private int seed = 1337;
        
        /// <summary>
        /// The target frames per second the agents should run at.
        ///
        /// If the agent is ever faster than this, it will wait until starting the next frame.
        /// </summary>
        [field: SerializeField] public int TargetFramesPerSecond { get; set; } = 20;
        
        private float TargetFrameTimeSeconds => 1f / TargetFramesPerSecond;

        private static AgentManager Instance { get; set; } = null!;

        private readonly List<(IAgent agent, int framesSinceWorked)> agents = new();

        private readonly object stopRequestLock = new();

        private int currCycleCounter = 0;
        private DateTime lastFrameTime = DateTime.Now;
        private int currAgentIndex = -1;
        internal IAgent CurrAgent => agents[currAgentIndex].agent;
        internal int CurrAgentFramesSinceWorked => agents[currAgentIndex].framesSinceWorked;

        internal bool IsAgentRunning => cancellationTokenSource != null;

        private CancellationTokenSource? cancellationTokenSource = null;

        private bool stopRequested = false;
        private Action? onStoppedMethod;
        
        private IWorld world = null!;

        private Context context = null!;

        private void Awake() {
            if (Instance != null) {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            context = new Context(useSeed ? new Random(seed) : new Random(), this);
        }

        private void Start() {
            world = FindObjectOfType<WorldGameObject>();
            GenerateAgents();
            HandleNextAgent();
#if UNITY_EDITOR
            EditorApplication.pauseStateChanged += (state) => {
                if (state == PauseState.Paused) {
                    RequestStopAgents(() => { Debug.Log("Editor paused stopped");});
                } else {
                    RestartAgents();
                }
            };
#endif
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
                timeToWaitSeconds += (float)(TargetFrameTimeSeconds - timeSinceLastFrame.TotalSeconds); // set wait time, if the previous frame was too fast
                lastFrameTime = DateTime.Now;
                // Debug.Log($"Cycle {currCycleCounter} started. Waiting {timeToWaitSeconds} seconds. {agents.Count} agents to run. {TargetFrameTimeSeconds} seconds per frame.");
            } else {
                // no need to wait, if we are in the same frame as the last agent
                // timeToWaitSeconds = 0; // but take the time from the last agent into account
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
            agents[currAgentIndex] = (agent, 0); // reset the frame counter for the agent
            cancellationTokenSource = new CancellationTokenSource();
            
            // run the task, but let it wait if the previous frame was too fast
            var task = Task.Run(() => {
                if (timeToWaitSeconds > 0) {
                    // Debug.Log($"Waiting {timeToWaitSeconds} seconds.");
                    Thread.Sleep((int)(timeToWaitSeconds * 1000));
                }
                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                agent.DoWork(cancellationTokenSource.Token, world, context);
            }, cancellationTokenSource.Token);
            
            // once the agent is done, either start the next agent or stop the manager, if requested
            task.ContinueWith(completedTask => {
                lock (stopRequestLock) {
                    cancellationTokenSource = null;
                    Debug.Log($"Application.IsPlaying(Instance): {Application.IsPlaying(Instance)} - If this is false only when a thread continues to run after play stopped, then this could be used here to stop a thread."); // TODO does this help in stopping tasks?
                    Debug.Log($"Application.isPlaying: {Application.isPlaying} - If this is false only when a thread continues to run after play stopped, then this could be used here to stop a thread.");
                    
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
                onStoppedMethod = onStopped;
            }
        }

        public void RestartAgents() {
            lock (stopRequestLock) {
                if (IsAgentRunning) return;
                Debug.Assert(cancellationTokenSource == null, $"cancellationTokenSource was not null");
                stopRequested = false;
                onStoppedMethod = null;
                HandleNextAgent();
            }
        }

        public class Context {
            public Random Random { get; }
            public AgentManager Manager { get; }

            public Context(Random random, AgentManager manager) {
                Random = random;
                Manager = manager;
            }
        }
    }
}