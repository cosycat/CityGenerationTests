using System;
using System.Collections.Generic;
using UnityEngine;
using DataStructures;

namespace FreeFormGraph.LineBased
{
    public class BVHLineAdapter: IBVHNodeAdapter<LineEdge>
    {
        private readonly Dictionary<LineEdge, BVHNode<LineEdge>> gameObjectToLeafMap = new();
        public event Action<LineEdge> PositionOrSizeChanged; // TODO is this event needed?

        BVH<LineEdge> IBVHNodeAdapter<LineEdge>.BVH { get; set; }

        //TODO: this is not used?
        public void CheckMap(LineEdge edge)
        {
            if (!gameObjectToLeafMap.ContainsKey(edge))
            {
                throw new Exception("missing map for shuffled child");
            }
        }

        public BVHNode<LineEdge> GetLeaf(LineEdge edge)
        {
            return gameObjectToLeafMap[edge];
        }

        public Vector3 GetObjectPos(LineEdge edge)
        {
            return edge.Position;
        }

        public float GetRadius(LineEdge particle)
        {
           return particle.Radius;
        }


        public void MapObjectToBVHLeaf(LineEdge particle, BVHNode<LineEdge> leaf)
        {       
            gameObjectToLeafMap[particle] = leaf;
        }

        // this allows us to be notified when an object moves, so we can adjust the BVH
        public void OnPositionOrSizeChanged(LineEdge changed)
        {
            // the SSObject has changed, so notify the BVH leaf to refit for the object
            gameObjectToLeafMap[changed].RefitObjectChanged(this, changed);
            PositionOrSizeChanged?.Invoke(changed);
        }

        public void UnmapObject(LineEdge particle)
        {
            gameObjectToLeafMap.Remove(particle);
        }
        
    }
}