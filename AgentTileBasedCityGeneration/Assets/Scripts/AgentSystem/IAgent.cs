using System.Threading;
using Graph.World;
using UnityEngine;

namespace AgentSystem {
    public interface IAgent {
        /// <summary>
        ///     The agent will be called every <see cref="WorkFrequency" /> frame by the <see cref="AgentManager" />.
        ///     Values smaller 1 are considered 1.
        /// </summary>
        AgentVariableInt WorkFrequency => Parameters.WorkFrequency;
        
        AgentParameters Parameters { get; }
        
        /// <summary>
        ///     A work package for an agent, that is run on a thread in the background.
        ///     Check periodically if the agent should cancel with the <see cref="CancellationToken" />.
        ///     (https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-cancellation)
        /// </summary>
        /// <param name="cancellationToken"> The cancellation token to check if the agent should cancel. </param>
        /// <param name="world"> The world the agent is working on. </param>
        /// <param name="context"> The context of the agent. </param>
        void DoWork(CancellationToken cancellationToken, IWorld world, AgentManager.Context context);

        string GetName() => GetType().Name;
    }
}