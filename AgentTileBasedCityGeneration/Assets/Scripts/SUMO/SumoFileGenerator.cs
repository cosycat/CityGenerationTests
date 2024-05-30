using System.Linq;
using System.Xml.Linq;
using FreeFormGraph;
using UnityEngine;

namespace SUMO {

    public class SumoSimulationOptions {
        public float SimulationStepLengthSeconds { get; set; } = 0.03f;
        public int RandomTripCount { get; set; } = 10;
        public int RandomFlowCount { get; set; } = 100;
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
        public SumoSimulationOptions SimulationOptions { get; }

        public string NodesFilePath => $"{SumoPath}/{NODES_FILE_NAME}";
        public string EdgesFilePath => $"{SumoPath}/{EDGES_FILE_NAME}";
        public string ConnectionsFilePath => $"{SumoPath}/{CONNECTIONS_FILE_NAME}";
        public string RoutesFilePath => $"{SumoPath}/{ROUTES_FILE_NAME}";
        public string OutputNetFilePath => $"{SumoPath}/{OUTPUT_NET_FILE_NAME}";
        public string ConfigurationFilePath => $"{SumoPath}/{CONFIGURATION_FILE_NAME}";

        private SumoFileGenerator(string sumoPath, IStreetGraph graph, SumoSimulationOptions simulationOptions) {
            Graph = graph;
            SimulationOptions = simulationOptions;
            SumoPath = sumoPath;
            InitializeDirectory(sumoPath);
            CreateNetworkFiles();
        }
        
        internal static SumoFileGenerator Create(string sumoPath, IStreetGraph graph, SumoSimulationOptions simulationOptions) {
            return new SumoFileGenerator(sumoPath, graph, simulationOptions);
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
                AddNode(nodesDoc, $"n{i}", node.PositionMeters.x, node.PositionMeters.y, "priority");
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

            edgesDoc.Root!.Add(newEdge);
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

            connectionsDoc.Root!.Add(newConnection);
        }

        private void GenerateRoutes() {
            var routesDoc = new XDocument(new XElement("routes"));
            
            AddCarType(routesDoc, "Car", 15.0f, 2.0f, 1.0f, 5.0f, 0.0f);

            for (int i = 0; i < SimulationOptions.RandomTripCount; i++) {
                var fromEdge = GetRandomEdge();
                var toEdge = GetRandomEdge();
                AddTrip(routesDoc, $"trip{i}", fromEdge, toEdge, "Car");
            }
            
            for (int i = 0; i < SimulationOptions.RandomFlowCount; i++) {
                var fromEdge = GetRandomEdge();
                var toEdge = GetRandomEdge();
                AddFlow(routesDoc, $"flow{i}", fromEdge, toEdge, 0, 10000, 20, "Car");
            }

            
            // AddTrip(routesDoc, "trip0", "e0", "e3", "Car");
            //
            // AddTrip(routesDoc, "trip1", "e0", "e4", "Car", 10);
            
            routesDoc.Save(RoutesFilePath);
        }
        
        private string GetRandomEdge(bool withReverse = false) {
            var edgeIndex = Random.Range(0, Graph.Edges.Count());
            return $"e{edgeIndex}{(withReverse && Random.value < 0.5 ? "_reverse" : "")}";
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

            routesDoc.Root!.Add(vType);
        }
        
        private static void AddRoute(XDocument routesDoc, string id, string edges) {
            var route = new XElement("route",
                new XAttribute("id", id),
                new XAttribute("edges", edges)
            );

            routesDoc.Root!.Add(route);
        }
        
        private static void AddTrip(XDocument routesDoc, string id, string fromEdge, string toEdge, string vehicleType, int depart = 0) {
            var trip = new XElement("trip",
                new XAttribute("id", id),
                new XAttribute("from", fromEdge),
                new XAttribute("to", toEdge),
                new XAttribute("depart", depart),
                new XAttribute("type", vehicleType)
            );

            routesDoc.Root!.Add(trip);
        }
        
        private static void AddFlow(XDocument routesDoc, string id, string fromEdge, string toEdge, int beginTime, int endTime, int period, string vehicleType) {
            var flow = new XElement("flow",
                new XAttribute("id", id),
                new XAttribute("from", fromEdge),
                new XAttribute("to", toEdge),
                new XAttribute("begin", beginTime),
                new XAttribute("end", endTime),
                new XAttribute("period", period),
                new XAttribute("type", vehicleType)
            );

            routesDoc.Root!.Add(flow);
        }
        
        private static void AddVehicle(XDocument routesDoc, string id, string route, string type) {
            var vehicle = new XElement("vehicle",
                new XAttribute("depart", 1),
                new XAttribute("id", id),
                new XAttribute("route", route),
                new XAttribute("type", type)
            );

            routesDoc.Root!.Add(vehicle);
        }

        private void GenerateConfiguration() {
            var configurationDoc = new XDocument(new XElement("configuration"));
            var input = new XElement("input",
                new XElement("net-file", new XAttribute("value", OUTPUT_NET_FILE_NAME)),
                new XElement("route-files", new XAttribute("value", ROUTES_FILE_NAME))
            );
            configurationDoc.Root!.Add(input);
            
            var time = new XElement("time",
                new XElement("begin", new XAttribute("value", "0")),
                new XElement("end", new XAttribute("value", "10000")),
                new XElement("step-length", new XAttribute("value", SimulationOptions.SimulationStepLengthSeconds))
            );
            configurationDoc.Root!.Add(time);
            
            configurationDoc.Save(ConfigurationFilePath);
        }
    }
}