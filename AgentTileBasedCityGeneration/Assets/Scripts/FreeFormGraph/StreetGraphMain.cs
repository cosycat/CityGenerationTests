
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FreeFormGraphMain {

    public class BezierStreetGraph : StreetGraphGameObject {
        protected override Dictionary<StreetNode, (StreetEdge, StreetNode)> Connections { get; } = new();
        public override float SnapToExistingNodeThreshold { get; set; }
        public override float SnapToExistingEdgeThreshold { get; set; }
        
        public override bool AddEdge(Vector3 from, Vector3 to, out StreetEdge newEdge, out StreetNode toNode) {
            throw new System.NotImplementedException();
        }

        public override bool IsPointOnStreet(Vector3 point) {
            throw new System.NotImplementedException();
        }

        public override bool IsPointNearEdge(Vector3 point, float withinDistance) {
            throw new System.NotImplementedException();
        }

        public override bool IsPointNearNode(Vector3 point, float withinDistance) {
            throw new System.NotImplementedException();
        }

        public override StreetNode FindClosestNode(Vector3 point) {
            throw new System.NotImplementedException();
        }
    }

    public abstract class StreetGraphGameObject : MonoBehaviour, IStreetGraph {
        
        protected abstract Dictionary<StreetNode, (StreetEdge, StreetNode)> Connections { get; }

        public abstract IEnumerable<StreetNode> Nodes { get; }
        public abstract IEnumerable<StreetEdge> Edges { get; }
        public abstract float SnapToExistingNodeThreshold { get; set; }
        public abstract float SnapToExistingEdgeThreshold { get; set; }
        public abstract bool AddEdge(Vector3 from, Vector3 to, out StreetEdge newEdge, out StreetNode toNode);
        public abstract bool IsPointOnStreet(Vector3 point);
        public abstract bool IsPointNearEdge(Vector3 point, float withinDistance);
        public abstract bool IsPointNearNode(Vector3 point, float withinDistance);
        public abstract StreetNode FindClosestNode(Vector3 point);
    }
    
    public interface IStreetGraph {
        // TODO do we need this really?
        public IEnumerable<StreetNode> Nodes { get; }
        public IEnumerable<StreetEdge> Edges { get; }
        public int NodeCount => Nodes.Count();
        public int EdgeCount => Edges.Count();
        
        /// <summary>
        /// The threshold for snapping the to position to an existing node when adding a new edge.
        ///
        /// When the to position is outside of <see cref="SnapToExistingNodeThreshold"/>, but within <see cref="SnapToExistingEdgeThreshold"/>
        /// of an existing edge, and the position on the edge is within <see cref="SnapToExistingNodeThreshold"/> of an existing node,
        /// the to position will still be snapped to the existing node.
        /// </summary>
        public float SnapToExistingNodeThreshold { get; set; }
        
        /// <summary>
        /// The threshold for snapping the to position to a new Node of an existing edge when adding a new edge.
        /// </summary>
        public float SnapToExistingEdgeThreshold { get; set; }
        
        /// <summary>
        /// Adds a new edge to the street graph.
        /// 
        /// If the to position is within <see cref="SnapToExistingNodeThreshold"/> of an existing node, the edge will be connected to that node.
        /// If the to position is within <see cref="SnapToExistingEdgeThreshold"/> of an existing edge, the edge will be connected to a new node on that edge,
        /// or to an existing node if the new position on the edge is within <see cref="SnapToExistingNodeThreshold"/> of an existing node.
        /// Otherwise, a new node will be created at the to position.
        /// </summary>
        /// <param name="from"> The node to connect the edge from. </param>
        /// <param name="to"> The position to connect the edge to. </param>
        /// <param name="newEdge"> The new edge that was created. </param>
        /// <param name="toNode"> The node that the edge was connected to. </param>
        /// <returns> True if the edge was successfully added, false otherwise. </returns>
        public bool AddEdge(Vector3 from, Vector3 to, out StreetEdge newEdge, out StreetNode toNode);

        /// <summary>
        /// Returns whether the given point is on a street.
        /// </summary>
        /// <param name="point"> The point to check. </param>
        /// <returns> True if the point is on the street, false otherwise. </returns>
        public bool IsPointOnStreet(Vector3 point);
        
        /// <summary>
        /// Returns whether the given point is within the given threshold of the street.
        /// </summary>
        /// <param name="point"> The point to check. </param>
        /// <param name="withinDistance"> The threshold to check, measured from the center of the street. </param>
        /// <returns> True if the point is within the threshold of the center of the street, false otherwise. </returns>
        public bool IsPointNearEdge(Vector3 point, float withinDistance);
        
        /// <summary>
        /// Returns whether the given point is within the given threshold of a node.
        /// </summary>
        /// <param name="point"> The point to check. </param>
        /// <param name="withinDistance"> The threshold to check, measured from the center of the node. </param>
        /// <returns> True if the point is within the threshold of the center of the node, false otherwise. </returns>
        public bool IsPointNearNode(Vector3 point, float withinDistance);
        
        /// <summary>
        /// Returns the closest node to the given position.
        ///
        /// Undefined behavior if there are no nodes in the graph.
        /// </summary>
        /// <param name="point"> The position to find the closest node to. </param>
        /// <returns> The closest node to the position. </returns>
        public StreetNode FindClosestNode(Vector3 point);
    }
    
    public struct StreetNode {
        public Vector3 Position { get; }
    }
    
    public struct StreetEdge {
        /// <summary>
        /// The position of the first node of the street.
        /// </summary>
        public Vector3 PosA { get; }
        /// <summary>
        /// The position of the second node of the street.
        /// </summary>
        public Vector3 PosB { get; }

        /// <summary>
        /// The street width measured from the center of the street to the edge of the street.
        /// </summary>
        public float StreetWidth { get; }
        
    }
    
}