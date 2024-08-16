#nullable enable
using System.Collections.Generic;
using System.Linq;
using Graph;
using UnityEngine;

namespace Utils.DataStructures {
    public interface ISpatialPointDatastructure<T> {
        public T? FindNearest(float x, float y);
        public List<T> FindRegion(Vector2 bottomLeft, Vector2 topRight);

        public void Insert(T obj);
        public void Remove(T obj);
    }

    public class QuadTreePointAdapter : ISpatialPointDatastructure<IStreetNode> {
        private const int BUCKET_SIZE = 50;
        private readonly float centerX;
        private readonly float centerY;
        private readonly float halfSize;
        private QuadTreeFloatPoint<NodeDataAdapter> quadTree;

        public QuadTreePointAdapter(Vector2 bottomLeft, Vector2 topRight) {
            Debug.Assert(bottomLeft.x < topRight.x);
            Debug.Assert(bottomLeft.y < topRight.y);
            Debug.Assert(Mathf.Approximately((topRight.x - bottomLeft.x) / 2.0f, (topRight.y - bottomLeft.y) / 2.0f),
                "width and height of rectangle has to be the same!");
            centerX = bottomLeft.x + (topRight.x - bottomLeft.x) / 2.0f;
            centerY = bottomLeft.y + (topRight.y - bottomLeft.y) / 2.0f;
            halfSize = (topRight.x - bottomLeft.x) / 2.0f;
            var region = new QuadTreeFloatPointRegion(centerX, centerY, halfSize);
            quadTree = new QuadTreeFloatPoint<NodeDataAdapter>(region, BUCKET_SIZE);
        }

        public IStreetNode? FindNearest(float x, float y) {
            var result = quadTree.QueryNeighbours(x, y, 1);
            if (result.Count == 0) return null;
            return result[0].n;
        }

        public List<IStreetNode> FindRegion(Vector2 bottomLeft, Vector2 topRight) {
            Debug.Assert(bottomLeft.x < topRight.x);
            Debug.Assert(bottomLeft.y < topRight.y, $"{bottomLeft} {topRight}");
            var result = quadTree.Query(bottomLeft.x, topRight.y, topRight.x - bottomLeft.x, topRight.y - bottomLeft.y);
            return result.Select(o => o.n).ToList();
        }

        public void Insert(IStreetNode obj) {
            Debug.Assert(obj != null);
            var quadTreeNode = new NodeDataAdapter(obj!);
            quadTree.Insert(quadTreeNode);
        }

        public void Remove(IStreetNode objToRemove) {
            Debug.Assert(objToRemove != null);
            //ugly but the quadtree can't remove elements...
            var region = new QuadTreeFloatPointRegion(centerX, centerY, halfSize);
            var newQuadTree = new QuadTreeFloatPoint<NodeDataAdapter>(region, BUCKET_SIZE);
            foreach (var o in quadTree) {
                if (o != objToRemove)
                    newQuadTree.Insert(new NodeDataAdapter(o.n));
            }

            quadTree = newQuadTree;
        }

        private class NodeDataAdapter : IQuadTreeData {
            public readonly IStreetNode n;

            public NodeDataAdapter(IStreetNode n) {
                this.n = n;
            }

            public float X => n.Position.x;
            public float Y => n.Position.y;
        }
    }
}