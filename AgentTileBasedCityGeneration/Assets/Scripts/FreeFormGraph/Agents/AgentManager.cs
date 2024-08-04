#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FreeFormGraph.World;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

namespace FreeFormGraph.Agents {
    public class AgentManager : MonoBehaviour {

        [SerializeField] private bool useRandomSeed = true;
        [SerializeField] private int seed = 1337;

        [SerializeField] private bool workOnMainThread = false;
        
        /// <summary>
        /// The target frames per second the agents should run at.
        ///
        /// If the agent is ever faster than this, it will wait until starting the next frame.
        /// </summary>
        [field: SerializeField] public int TargetFramesPerSecond { get; set; } = 20;
        
        private float TargetFrameTimeSeconds => 1f / TargetFramesPerSecond;

        public static AgentManager Instance { get; private set; } = null!;

        private readonly List<(IAgent agent, int framesSinceWorked)> agents = new();

        private readonly object stopRequestLock = new();

        private int currCycleCounter = 0;
        private DateTime lastFrameTime = DateTime.Now;
        private int currAgentIndex = -1;
        internal IAgent CurrAgent => agents[currAgentIndex].agent;
        internal int CurrAgentFramesSinceWorked => agents[currAgentIndex].framesSinceWorked;

        internal bool IsAgentRunning => cancellationTokenSource != null;

        private CancellationTokenSource? cancellationTokenSource = null;
        
        private Task? completedTask = null;
        private readonly object completedTaskLock = new();

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
            seed = useRandomSeed ? new Random().Next() : seed;
            context = new Context(new Random(seed), this);
            Debug.Log($"AgentManager initialized with seed {seed}");
        }

        private void Start() {
            world = FindObjectOfType<WorldGameObject>();
            GenerateAgents();
            HandleNextAgent();
            
// #if UNITY_EDITOR
// // not needed anymore, since if the editor is paused, the task that ended just does not get handled until the next time it is run again.
//             EditorApplication.pauseStateChanged += (state) => {
//                 if (state == PauseState.Paused) {
//                     RequestStopAgents(() => { Debug.Log("Editor paused stopped");});
//                 } else {
//                     RestartAgents();
//                 }
//             };
// #endif
        }

        private void Update() {
            HandleCompletedTask();
            return;

            void HandleCompletedTask() {
                if (!Monitor.TryEnter(completedTaskLock)) return;
                
                try {
                    if (completedTask == null) return;
                    
                    var currCompletedTask = completedTask!;
                    completedTask = null;
                    
                    // Debug.Log($"Completed task, Before Lock - Application.IsPlaying(Instance): {Application.IsPlaying(Instance)}, Application.isPlaying: {Application.isPlaying}");
                    lock (stopRequestLock) {
                        cancellationTokenSource = null;
                        if (!Application.IsPlaying(Instance) || !Application.isPlaying) {
                            Debug.Log($"Application.IsPlaying(Instance): {Application.IsPlaying(Instance)} - If this is false only when a thread continues to run after play stopped, then this could be used here to stop a thread."); // TODO does this help in stopping tasks?
                            Debug.Log($"Application.isPlaying: {Application.isPlaying} - If this is false only when a thread continues to run after play stopped, then this could be used here to stop a thread.");
                        }

                        if(currCompletedTask.IsFaulted) {
                            var exceptions = currCompletedTask.Exception?.Flatten().InnerExceptions;
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
                    
                } finally {
                    Monitor.Exit(completedTaskLock);
                }
            }
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
            
            // reset the frame counter for the agent
            agents[currAgentIndex] = (agent, 0); 
            cancellationTokenSource = new CancellationTokenSource();
            
            
            // initialize the task, but let it wait if the previous frame was too fast
            var task = new Task(() => {
                if (timeToWaitSeconds > 0) {
                    Thread.Sleep((int)(timeToWaitSeconds * 1000));
                }
                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                
                agent.DoWork(cancellationTokenSource.Token, world, context);
                
                Thread.Sleep(1); // make sure the task is not too fast, not sure if needed.
            }, cancellationTokenSource.Token);
            
            // once the agent is done, either start the next agent or stop the manager, if requested
            task.ContinueWith(currCompletedTask => {
                Monitor.Enter(completedTaskLock);
                try {
                    if (completedTask != null) throw new Exception($"Somehow the next task was started before the previous one was handled. Tasks should always run in sequence. current: {completedTask}, new: {currCompletedTask}, status: {currCompletedTask.Status}");
                    completedTask = currCompletedTask;
                } finally {
                    Monitor.Exit(completedTaskLock);
                }
            });
            
            if(workOnMainThread) task.Start(TaskScheduler.FromCurrentSynchronizationContext());
            else task.Start();
        }

        private void GenerateAgents() {
            foreach (var agent in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IAgent>()) {
                agents.Add((agent, agent.WorkFrequency));
                Debug.Log($"Added agent {agent.GetType().Name} with frequency {agent.WorkFrequency}");
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