#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FreeFormGraph;
using FreeFormGraph.World;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SUMO {
    internal class SumoFileGenerator {
        private const string EDGES_FILE_NAME = "edges.edg.xml";
        private const string NODES_FILE_NAME = "nodes.nod.xml";
        private const string CONNECTIONS_FILE_NAME = "connections.con.xml";
        private const string ROUTES_FILE_NAME = "routes.rou.xml";
        private const string OUTPUT_NET_FILE_NAME = "output.net.xml";
        private const string CONFIGURATION_FILE_NAME = "configuration.sumocfg";
        private const string EDGES_TYPES_FILE_NAME = "edges.typ.xml";

        private IStreetGraph Graph { get; }
        private IWorld World { get; }
        private string SumoFilesPath { get; }
        public SumoSimulationOptions SimulationOptions { get; }

        public string NodesFilePath => $"{SumoFilesPath}/{NODES_FILE_NAME}";
        public string EdgesFilePath => $"{SumoFilesPath}/{EDGES_FILE_NAME}";
        public string ConnectionsFilePath => $"{SumoFilesPath}/{CONNECTIONS_FILE_NAME}";
        public string RoutesFilePath => $"{SumoFilesPath}/{ROUTES_FILE_NAME}";
        public string OutputNetFilePath => $"{SumoFilesPath}/{OUTPUT_NET_FILE_NAME}";
        public string ConfigurationFilePath => $"{SumoFilesPath}/{CONFIGURATION_FILE_NAME}";
        public string EdgesTypesFilePath => $"{SumoFilesPath}/{EDGES_TYPES_FILE_NAME}";

        private SumoFileGenerator(string sumoFilesPath, IStreetGraph graph, SumoSimulationOptions simulationOptions, IWorld world) {
            Graph = graph;
            World = world;
            SimulationOptions = simulationOptions;
            SumoFilesPath = sumoFilesPath;
            InitializeDirectory(sumoFilesPath);
            CreateNetworkFiles();
        }
        
        internal static SumoFileGenerator Create(string sumoFilesPath, IStreetGraph graph, SumoSimulationOptions simulationOptions, IWorld world) {
            return new SumoFileGenerator(sumoFilesPath, graph, simulationOptions, world);
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
            GenerateEdgesTypes();
            GenerateConnections();
            GenerateRoutes();
            GenerateConfiguration();
        }
        
        
        private void GenerateNodes() {
            var nodesDoc = new XDocument(new XElement("nodes"));
            
            var nodes = Graph.Nodes.ToArray();
            for (var i = 0; i < nodes.Length; i++) {
                var node = nodes[i];
                AddNode(nodesDoc, $"n{i}", node.PositionMeters.x, node.PositionMeters.y, World.GetHeightAt(node.Position.x, node.Position.y), "priority");
            }
            
            nodesDoc.Save(NodesFilePath);
        }

        private static void AddNode(XDocument nodesDoc, string nodeId, double x, double y, double height, string type) {
            var newNode = new XElement("node",
                new XAttribute("id", nodeId),
                new XAttribute("x", x),
                new XAttribute("y", y),
                new XAttribute("z", height),
                new XAttribute("type", type)
            );

            Debug.Assert(nodesDoc.Root != null, "nodesDoc.Root != null");
            nodesDoc.Root?.Add(newNode);
        }

        private void GenerateEdges() {
            var edgesDoc = new XDocument(new XElement("edges"));
            
            var edges = Graph.Edges.ToArray();
            var nodesToIndex = Graph.Nodes.Select((node, i) => (node, i)).ToDictionary(t => t.node, t => t.i);
            for (var i = 0; i < edges.Length; i++) {
                var edge = edges[i];
                var nodeA = edge.NodeA;
                var nodeB = edge.NodeB;
                var nodeAIndex = nodesToIndex[nodeA];
                var nodeBIndex = nodesToIndex[nodeB];
                var roadType = edge.Type;
                var edgeType = SimulationOptions.RoadTypeToEdgeType[roadType].Id;
                AddEdge(edgesDoc, $"e{i}", $"n{nodeAIndex}", $"n{nodeBIndex}", edgeType);
                AddEdge(edgesDoc, $"e{i}_reverse", $"n{nodeBIndex}", $"n{nodeAIndex}", edgeType);
            }
            
            edgesDoc.Save(EdgesFilePath);
        }

        private static void AddEdge(XDocument edgesDoc, string edgeId, string fromNode, string toNode, string typeId) {
            var newEdge = new XElement("edge",
                new XAttribute("id", edgeId),
                new XAttribute("from", fromNode),
                new XAttribute("to", toNode),
                new XAttribute("type", typeId)
            );

            Debug.Assert(edgesDoc.Root != null, "edgesDoc.Root != null");
            edgesDoc.Root?.Add(newEdge);
        }

        private void GenerateEdgesTypes() {
            var edgesTypesDoc = new XDocument(new XElement("types"));
            
            foreach (var roadType in Enum.GetValues(typeof(RoadType)).Cast<RoadType>()) {
                var edgeType = SimulationOptions.RoadTypeToEdgeType.TryGetValue(roadType, out var e) ? e : SumoEdgeTypes.DefaultSumoEdgeType;
                var type = new XElement("type",
                    new XAttribute("id", edgeType.Id),
                    new XAttribute("speed", edgeType.Speed),
                    new XAttribute("numLanes", edgeType.NumLanes),
                    new XAttribute("priority", edgeType.Priority)
                );
                edgesTypesDoc.Root!.Add(type);
            }

            edgesTypesDoc.Save(EdgesTypesFilePath);
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

            foreach (var vehicleType in SimulationOptions.VehicleTypes) {
                AddCarType(routesDoc, vehicleType);
            }

            for (int i = 0; i < SimulationOptions.RandomTripCount; i++) {
                var fromEdge = GetRandomEdge();
                var toEdge = GetRandomEdge();
                if (fromEdge == toEdge) continue;
                AddTrip(routesDoc, $"trip{i}", fromEdge, toEdge, GetRandomVehicleType());
            }
            
            for (int i = 0; i < SimulationOptions.RandomFlowCount; i++) {
                var fromEdge = GetRandomEdge();
                var toEdge = GetRandomEdge();
                if (fromEdge == toEdge) continue;
                AddFlow(routesDoc, $"flow{i}", fromEdge, toEdge, 0, 10000, SimulationOptions.FlowPeriod, GetRandomVehicleType());
            }
            
            routesDoc.Save(RoutesFilePath);
        }
        
        private string GetRandomVehicleType() {
            var vehicleTypeIndex = Random.Range(0, SimulationOptions.VehicleTypes.Length);
            return SimulationOptions.VehicleTypes[vehicleTypeIndex].Id;
        }

        private string GetRandomEdge(bool withReverse = false) {
            var edgeIndex = Random.Range(0, Graph.Edges.Count());
            return $"e{edgeIndex}{(withReverse && Random.value < 0.5 ? "_reverse" : "")}";
        }

        private static void AddCarType(XDocument routesDoc, SumoVehicleType vehicleType) {
            var vType = new XElement("vType",
                new XAttribute("id", vehicleType.Id),
                new XAttribute("length", vehicleType.Length),
                new XAttribute("maxSpeed", vehicleType.MaxSpeed),
                new XAttribute("accel", vehicleType.Accel),
                new XAttribute("decel", vehicleType.Decel),
                new XAttribute("vClass", vehicleType.VehicleClass.ToString().ToLower())
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

        private static void AddFlow(XDocument routesDoc, string id, string fromEdge, string toEdge, int beginTime, int endTime, float period, string vehicleType) {
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