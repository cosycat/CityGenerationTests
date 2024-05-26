using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using FreeFormGraph;
using FreeFormGraph.LineBased;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SUMO {
    
    /// <summary>
    /// Generates a SUMO network from a given graph.
    /// </summary>
    public class SumoNetworkGenerator : MonoBehaviour {
        private const string NETCONVERT_PATH_HOMEBREW = "/opt/homebrew/bin/netconvert"; // TODO Add more systems and installations. This only works on macOS with Homebrew installation of SUMO.
        private static string SumoPath => $"{Application.persistentDataPath}/SUMO";
        private string NodesFilePath => $"{SumoPath}/nodes.nod.xml";
        private string EdgesFilePath => $"{SumoPath}/edges.edg.xml";
        private string ConnectionsFilePath => $"{SumoPath}/connections.con.xml";
        private string OutputNetFilePath => $"{SumoPath}/output.net.xml";

        private void Start() {
            GenerateTestGraph();
        }

        private void GenerateTestGraph() {
            var graph = new GameObject("TestGraph").AddComponent<LineGraph>();
            graph.CreateUnconnectedNode(new Vector3(0, 0, 0), out var n0);
            
            graph.CreateEdge(n0, new Vector3(100, 0, 0), out var e0, out var n1, out var isToNodeNew, out var isEdgeNew);
            graph.CreateEdge(n1, new Vector3(100, 100, 0), out var e1, out var n2, out var isToNodeNew1, out var isEdgeNew1);
            graph.CreateEdge(n2, new Vector3(0, 100, 0), out var e2, out var n3, out var isToNodeNew2, out var isEdgeNew2);
            graph.CreateEdge(n3, new Vector3(0, 0, 0), out var e3, out var n02, out var isToNodeNew3, out var isEdgeNew3);
            Debug.Assert(n0 == n02, "n0 == n02");
            Debug.Assert(!isToNodeNew3);
            Debug.Assert(isEdgeNew3); 

            GenerateNetwork(graph);
            StartCoroutine(ConvertToSumoNetwork(NodesFilePath, EdgesFilePath, ConnectionsFilePath, OutputNetFilePath));
        }

        public void GenerateNetwork(IStreetGraph graph) {
            Debug.Log($"Generating SUMO network from graph with {graph.NodeCount} nodes and {graph.EdgeCount} edges in {SumoPath}..");
            InitializeFiles();
            GenerateNodes(NodesFilePath, graph);
            GenerateEdges(EdgesFilePath, graph);
            GenerateConnections(ConnectionsFilePath, graph);
        }

        private void InitializeFiles() {
            if (!System.IO.Directory.Exists(SumoPath)) {
                System.IO.Directory.CreateDirectory(SumoPath);
            }
            if (System.IO.File.Exists(NodesFilePath)) {
                System.IO.File.Delete(NodesFilePath);
            }
            if (System.IO.File.Exists(EdgesFilePath)) {
                System.IO.File.Delete(EdgesFilePath);
            }
            if (System.IO.File.Exists(ConnectionsFilePath)) {
                System.IO.File.Delete(ConnectionsFilePath);
            }
            if (System.IO.File.Exists(OutputNetFilePath)) {
                System.IO.File.Delete(OutputNetFilePath);
            }
            // create output file
            // System.IO.File.Create(OutputNetFilePath);
        }

        private static void GenerateNodes(string nodesFilePath, IStreetGraph graph) {
            var nodesDoc = new XDocument(new XElement("nodes"));
            
            var nodes = graph.Nodes.ToArray();
            for (var i = 0; i < nodes.Length; i++) {
                var node = nodes[i];
                AddNode(nodesDoc, $"n{i}", node.Position.x, node.Position.y, "priority");
            }
            
            nodesDoc.Save(nodesFilePath);
        }

        private static void AddNode(XDocument nodesDoc, string nodeId, double x, double y, string type) {
            var newNode = new XElement("node",
                new XAttribute("id", nodeId),
                new XAttribute("x", x),
                new XAttribute("y", y),
                new XAttribute("type", type)
            );

            Debug.Assert(nodesDoc.Root != null, "nodesDoc.Root != null");
            nodesDoc.Root.Add(newNode);
        }

        private static void GenerateEdges(string edgesFilePath, IStreetGraph graph) {
            var edgesDoc = new XDocument(new XElement("edges"));
            
            var edges = graph.Edges.ToArray();
            for (var i = 0; i < edges.Length; i++) {
                var edge = edges[i];
                var nodeA = edge.NodeA;
                var nodeB = edge.NodeB;
                var nodeAIndex = Array.IndexOf(graph.Nodes.ToArray(), nodeA);
                var nodeBIndex = Array.IndexOf(graph.Nodes.ToArray(), nodeB);
                AddEdge(edgesDoc, $"e{i}", $"n{nodeAIndex}", $"n{nodeBIndex}", 1, 1, 10);
                AddEdge(edgesDoc, $"e{i}_reverse", $"n{nodeBIndex}", $"n{nodeAIndex}", 1, 1, 10);
            }
            
            edgesDoc.Save(edgesFilePath);
        }

        private static void AddEdge(XDocument edgesDoc, string edgeId, string fromNode, string toNode, int priority, int numLanes, double speed) {
            var newEdge = new XElement("edge",
                new XAttribute("id", edgeId),
                new XAttribute("from", fromNode),
                new XAttribute("to", toNode),
                new XAttribute("priority", priority),
                new XAttribute("numLanes", numLanes),
                new XAttribute("speed", speed)
            );

            Debug.Assert(edgesDoc.Root != null, "edgesDoc.Root != null");
            edgesDoc.Root.Add(newEdge);
        }

        private static void GenerateConnections(string connectionsFilePath, IStreetGraph graph)
        {
            var connectionsDoc = new XDocument(new XElement("connections"));

            // // Define connections (optional)
            // AddConnection(connectionsDoc, "e1", "e2", "1", "1");
            // AddConnection(connectionsDoc, "e2", "e3", "1", "1");
            // AddConnection(connectionsDoc, "e3", "e4", "1", "1");
            // AddConnection(connectionsDoc, "e4", "e1", "1", "1");

            connectionsDoc.Save(connectionsFilePath);
        }

        private static void AddConnection(XDocument connectionsDoc, string fromEdge, string toEdge, string fromLane, string toLane)
        {
            var newConnection = new XElement("connection",
                new XAttribute("from", fromEdge),
                new XAttribute("to", toEdge),
                new XAttribute("fromLane", fromLane),
                new XAttribute("toLane", toLane)
            );

            Debug.Assert(connectionsDoc.Root != null, "connectionsDoc.Root != null");
            connectionsDoc.Root.Add(newConnection);
        }

        private IEnumerator ConvertToSumoNetwork(string nodesFilePath, string edgesFilePath, string connectionsFilePath,
            string outputNetFilePath) {
            // const string netconvertPath = "/usr/local/bin/netconvert"; // Ensure netconvert is installed and path is correct
            var arguments = $"--node-files=\"{nodesFilePath}\" --edge-files=\"{edgesFilePath}\" --connection-files=\"{connectionsFilePath}\" --output-file=\"{outputNetFilePath}\"";

            var startInfo = new ProcessStartInfo {
                FileName = NETCONVERT_PATH_HOMEBREW,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process()) {
                process.StartInfo = startInfo;
                process.OutputDataReceived += (sender, e) => Debug.Log(e.Data);
                process.ErrorDataReceived += (sender, e) => Debug.LogError(e.Data);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                while (!process.HasExited) {
                    yield return null;
                }

                if (process.ExitCode != 0) {
                    Debug.LogError($"netconvert failed with exit code {process.ExitCode}");
                }
                else {
                    Debug.Log("netconvert completed successfully.");
                }
            }

            Debug.Log("Conversion completed.");
        }

        // private static void ConvertToSumoNetwork(string nodesFilePath, string edgesFilePath, string connectionsFilePath, string outputNetFilePath)
        // {
        //     var startInfo = new ProcessStartInfo
        //     {
        //         FileName = "netconvert",
        //         Arguments = $"--node-files={nodesFilePath} --edge-files={edgesFilePath} --output-file={outputNetFilePath}",
        //         RedirectStandardOutput = true,
        //         RedirectStandardError = true,
        //         UseShellExecute = false,
        //         CreateNoWindow = true
        //     };
        //
        //     using (var process = Process.Start(startInfo))
        //     {
        //         if (process == null) {
        //             Debug.Log("Failed to start conversion process");
        //             return;
        //         }
        //
        //         process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
        //         process.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);
        //
        //         process.BeginOutputReadLine();
        //         process.BeginErrorReadLine();
        //
        //         process.WaitForExit();
        //     }
        // }
    }
}