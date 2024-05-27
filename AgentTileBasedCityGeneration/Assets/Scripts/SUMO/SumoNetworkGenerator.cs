using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using FreeFormGraph;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SUMO {
    
    /// <summary>
    /// Generates a SUMO network from a given graph.
    /// </summary>
    public class SumoNetworkGenerator : MonoBehaviour {
        private const string NETCONVERT_PATH_HOMEBREW = "/opt/homebrew/bin/netconvert"; // TODO Add more systems and installations. This only works on macOS with Homebrew installation of SUMO. alternatives: "/usr/local/bin/netconvert"
        
        [SerializeField] private bool openFolderAfterGeneration = true;
        
        private string SumoPath { get; set; }

        private void Awake() {
            SumoPath = $"{Application.persistentDataPath}/SUMO";
        }

        public void GenerateNetwork(IStreetGraph graph) {
            Debug.Log($"Generating SUMO network from graph with {graph.NodeCount} nodes and {graph.EdgeCount} edges in {SumoPath}..");
            var sumoFileGenerator = SumoFileGenerator.Create(SumoPath, graph);
            StartCoroutine(ConvertToSumoNetwork(sumoFileGenerator));
        }

        private IEnumerator ConvertToSumoNetwork(SumoFileGenerator sumoFileGenerator) {
            var nodesFilePath = sumoFileGenerator.NodesFilePath;
            var edgesFilePath = sumoFileGenerator.EdgesFilePath;
            var connectionsFilePath = sumoFileGenerator.ConnectionsFilePath;
            var routesFilePath = sumoFileGenerator.RoutesFilePath;
            var outputNetFilePath = sumoFileGenerator.OutputNetFilePath;
            var configurationFilePath = sumoFileGenerator.ConfigurationFilePath;
            
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
                process.OutputDataReceived += (_, e) => Debug.Log(e.Data);
                process.ErrorDataReceived += (_, e) => Debug.LogError(e.Data);

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
            
            if (openFolderAfterGeneration) {
                using var process = new Process();
                process.StartInfo.FileName = "open";
                process.StartInfo.Arguments = $"\"{SumoPath}\"";

                process.Start();

                while (!process.HasExited) {
                    yield return null;
                }
                    
                if (process.ExitCode != 0) {
                    Debug.LogError($"Failed to open folder with exit code {process.ExitCode}");
                }
                else {
                    Debug.Log($"Opened folder {SumoPath}.");
                }
            }

            Debug.Log("Conversion completed.");
        }
        
    }

    internal class SumoFileGenerator {
        private const string EDGES_FILE_NAME = "edges.edg.xml";
        private const string NODES_FILE_NAME = "nodes.nod.xml";
        private const string CONNECTIONS_FILE_NAME = "connections.con.xml";
        private const string ROUTES_FILE_NAME = "routes.rou.xml";
        private const string OUTPUT_NET_FILE_NAME = "output.net.xml";
        private const string CONFIGURATION_FILE_NAME = "configuration.sumocfg";

        private IStreetGraph Graph { get; }
        private string SumoPath { get; }

        public string NodesFilePath => $"{SumoPath}/{NODES_FILE_NAME}";
        public string EdgesFilePath => $"{SumoPath}/{EDGES_FILE_NAME}";
        public string ConnectionsFilePath => $"{SumoPath}/{CONNECTIONS_FILE_NAME}";
        public string RoutesFilePath => $"{SumoPath}/{ROUTES_FILE_NAME}";
        public string OutputNetFilePath => $"{SumoPath}/{OUTPUT_NET_FILE_NAME}";
        public string ConfigurationFilePath => $"{SumoPath}/{CONFIGURATION_FILE_NAME}";

        private SumoFileGenerator(string sumoPath, IStreetGraph graph) {
            Graph = graph;
            SumoPath = sumoPath;
            InitializeDirectory(sumoPath);
            CreateNetworkFiles();
        }
        
        internal static SumoFileGenerator Create(string sumoPath, IStreetGraph graph) {
            return new SumoFileGenerator(sumoPath, graph);
        }

        private static void InitializeDirectory(string sumoPath) {
            if (!System.IO.Directory.Exists(sumoPath)) {
                System.IO.Directory.CreateDirectory(sumoPath);
                return;
            }

            var directoryInfo = new System.IO.DirectoryInfo(sumoPath);
            foreach (var file in directoryInfo.GetFiles()) {
                file.Delete();
            }
                
            foreach (var dir in directoryInfo.GetDirectories()) {
                dir.Delete(true);
            }
        }
        
        private void CreateNetworkFiles() {
            GenerateNodes();
            GenerateEdges();
            GenerateConnections();
            GenerateRoutes();
            GenerateConfiguration();
        }
        
        
        private void GenerateNodes() {
            var nodesDoc = new XDocument(new XElement("nodes"));
            
            var nodes = Graph.Nodes.ToArray();
            for (var i = 0; i < nodes.Length; i++) {
                var node = nodes[i];
                AddNode(nodesDoc, $"n{i}", node.Position.x, node.Position.y, "priority");
            }
            
            nodesDoc.Save(NodesFilePath);
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

        private void GenerateEdges() {
            var edgesDoc = new XDocument(new XElement("edges"));
            
            var edges = Graph.Edges.ToArray();
            var nodes = Graph.Nodes.Select((node, i) => (node, i)).ToDictionary(t => t.node, t => t.i);
            for (var i = 0; i < edges.Length; i++) {
                var edge = edges[i];
                var nodeA = edge.NodeA;
                var nodeB = edge.NodeB;
                var nodeAIndex = nodes[nodeA];
                var nodeBIndex = nodes[nodeB];
                AddEdge(edgesDoc, $"e{i}", $"n{nodeAIndex}", $"n{nodeBIndex}", 1, 1, 10);
                AddEdge(edgesDoc, $"e{i}_reverse", $"n{nodeBIndex}", $"n{nodeAIndex}", 1, 1, 10);
            }
            
            edgesDoc.Save(EdgesFilePath);
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

        private void GenerateConnections()
        {
            var connectionsDoc = new XDocument(new XElement("connections"));
            
            var edges = Graph.Edges.Select((edge, i) => (edge, i)).ToDictionary(t => t.edge, t => t.i);
            var nodes = Graph.Nodes.ToArray();
            
            foreach (var node in nodes) {
                var connectedEdges = node.Edges.ToArray();
                foreach (var edgeJ in connectedEdges) {
                    var edgeJIndex = edges[edgeJ];
                    foreach (var edgeK in connectedEdges) {
                        var edgeKIndex = edges[edgeK];
                        if (edgeJ == edgeK) continue;
                        
                        var fromEdge = $"e{edgeJIndex}" + (edgeJ.NodeB == node ? "" : "_reverse"); // the fromEdge needs to be the edge going into the node, while the toEdge needs to be the edge going out of the node
                        var toEdge = $"e{edgeKIndex}" + (edgeK.NodeA == node ? "" : "_reverse");
                        
                        AddConnection(connectionsDoc, fromEdge, toEdge, "0", "0");
                    }
                }
            }

            connectionsDoc.Save(ConnectionsFilePath);
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

        private void GenerateRoutes() {
            var routesDoc = new XDocument(new XElement("routes"));

            AddCarType(routesDoc, "Car", 15.0f, 2.0f, 1.0f, 5.0f, 0.0f);
            
            AddRoute(routesDoc, "route0", "e0 e1 e2 e3");
            AddVehicle(routesDoc, "veh0", "route0", "Car");
            
            AddRoute(routesDoc, "route1", "e3_reverse e2_reverse e1_reverse e0_reverse");
            AddVehicle(routesDoc, "veh1", "route1", "Car");
            
            AddRoute(routesDoc, "route2", "e1 e3");
            AddVehicle(routesDoc, "veh2", "route2", "Car");
            
            routesDoc.Save(RoutesFilePath);
        }
        
        private static void AddCarType(XDocument routesDoc, string id, float maxSpeed, float length, float accel, float decel, float sigma) {
            var vType = new XElement("vType",
                new XAttribute("id", id),
                new XAttribute("maxSpeed", maxSpeed),
                new XAttribute("length", length),
                new XAttribute("accel", accel),
                new XAttribute("decel", decel),
                new XAttribute("sigma", sigma)
            );

            Debug.Assert(routesDoc.Root != null, "routesDoc.Root != null");
            routesDoc.Root.Add(vType);
        }
        
        private static void AddRoute(XDocument routesDoc, string id, string edges) {
            var route = new XElement("route",
                new XAttribute("id", id),
                new XAttribute("edges", edges)
            );

            Debug.Assert(routesDoc.Root != null, "routesDoc.Root != null");
            routesDoc.Root.Add(route);
        }
        
        private static void AddVehicle(XDocument routesDoc, string id, string route, string type) {
            var vehicle = new XElement("vehicle",
                new XAttribute("depart", 1),
                new XAttribute("id", id),
                new XAttribute("route", route),
                new XAttribute("type", type)
            );

            Debug.Assert(routesDoc.Root != null, "routesDoc.Root != null");
            routesDoc.Root.Add(vehicle);
        }

        private void GenerateConfiguration() {
            var configurationDoc = new XDocument(new XElement("configuration"));
            var input = new XElement("input",
                new XElement("net-file", new XAttribute("value", OUTPUT_NET_FILE_NAME)),
                new XElement("route-files", new XAttribute("value", ROUTES_FILE_NAME))
            );
            configurationDoc.Root?.Add(input);
            
            var time = new XElement("time",
                new XElement("begin", new XAttribute("value", "0")),
                new XElement("end", new XAttribute("value", "10000"))
            );
            configurationDoc.Root?.Add(time);
            
            configurationDoc.Save(ConfigurationFilePath);
        }
    }
}