using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static vomark.app.VomarkUtil.VomGraph;

namespace vomark.app
{
    public class VomarkUtil
    {
        // TODO: UNDO ALL XML COMPONENTS, SWITCH TO JSON CONVERSION
        [JsonConverter(typeof(GraphJsonConverter))]
        public class VomGraph
        {
            public VomNode Root { get; set; } = new("__DBG__NULL__", false);
            public VomNode Term {get; set;} = new("__DBG__TERM__", true);
            public string Label { get; set; }
            public List<VomNode> Nodes { get; set; } = [];

            public VomGraph(string graphLabel, bool fromJson = false)
            {
                Label = graphLabel;
                Nodes.Add(Root);
                if(!fromJson)
                {
                    Nodes.Add(Term);
                }
            }

            public VomGraph()
            {
                Label = "NewGraph";
                Nodes.Add(Root);
                Nodes.Add(Term);
            }

            // List should only be used when deserializing JSON
            // Required as a full nodelist cannot be referenced while deserializing.
            public void AddNode(VomNode newNode, VomNode? parent = null, int weight = 1, List<VomNode>? list = null)
            {
                list = list ?? Nodes;
                parent = parent ?? Root;
                VomEdge rel = new(parent, newNode);
                //Debug.WriteLine($"EDGE: {parent.Data} to {newNode.Data}");
                if (list.Contains(newNode))
                {
                    //Debug.WriteLine($"Node {newNode.Data} is in graph list");
                    if (!parent.AdjList.TryAdd(rel, weight))
                    {
                        parent.AdjList[rel] += weight;
                    }
                }
                else
                {
                    //Debug.WriteLine($"Adding node {newNode.Data} to graph.");
                    list.Add(newNode);
                    //foreach(VomEdge edge in parent.AdjList.Keys)
                    //{
                    //    //Debug.WriteLine($"{edge.Start.Data} --- {edge.End.Data}");
                    //}
                   parent.AdjList.TryAdd(rel, weight);
                }
            }

            public void AddJsonNode(Dictionary<string, VomNode> dict, VomNode newNode, VomNode? parent = null, int weight = 1)
            {
                parent = parent ?? Root;
                VomEdge rel = new(parent, newNode);

                if (!dict.TryAdd(newNode.Data, newNode))
                {
                    if (!parent.AdjList.TryAdd(rel, weight))
                    {
                        parent.AdjList[rel] += weight;
                    }
                }
            }

            public VomNode FindNode(string data)
            {
                return Nodes.Find(x => x.Data == data) ?? 
                    throw new ArgumentException($"{data} is not a valid node");
            }

            public string? FormSentence(int maxLen, char punc, VomNode? start)
            {
                if(this == null)
                {
                    return null;
                }
                StringBuilder sentence = new();
                start = start ?? Root;
                VomNode curr = start;
                int wordCount = 0;
                try
                {
                    while (wordCount < maxLen)
                    {
                        curr = GetNextNode(curr);
                        if (curr.Equals(Term)) 
                        {
                            //Debug.WriteLine($"Node {curr.Data} is equal to {Term.Data} -- BREAKING");
                            break; 
                        }
                        sentence.Append($"{curr.Data} ");
                        ++wordCount;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                string res = sentence.ToString().Trim();
                //DELETE
                //Debug.WriteLine($"FULL SENTENCE: {res}");
                return res + punc;
            }

            public VomNode GetNextNode(VomNode curr)
            {
                Dictionary<VomEdge, int> adj = curr.AdjList;
                //Debug.WriteLine("CURR NODE: \n" + curr.ToString());
                if(adj.Count <= 0)
                {
                    //Debug.WriteLine("NEXT NODE - TERMINAL NODE REACHED");
                    return Term;
                }
                int weightSum = adj.Values.Sum();
                return GetNextRand(adj, weightSum).End;
            }

            private static VomEdge GetNextRand(Dictionary<VomEdge, int> adj, int weightSum)
            {
                if(adj.Count <= 0)
                {
                    throw new NullReferenceException($"Node containing AdjList is a terminal node."); ;
                }
                int randWeight = new Random().Next(weightSum);
                foreach(VomEdge edge in adj.Keys)
                {
                    if(adj[edge] >= randWeight)
                    {
                        return edge;
                    }
                    randWeight -= adj[edge];
                }
                throw new ArgumentOutOfRangeException($"AdjList {adj} could not produce a result.");
            }

            public bool ToJson(string folder, JsonSerializerOptions options)
            {
                bool complete = false;
                try
                {
                    string data = JsonSerializer.Serialize(this, options);
                    string dest = $"{folder}/{Label}.json";
                    File.WriteAllText(dest, data);
                    complete = true;
                }
                catch(Exception e)
                {
                    Debug.WriteLine(e);
                }

                return complete;
            }

            [JsonConverter(typeof(NodeJsonConverter))]
            public class VomNode : IEquatable<VomNode>
            {
                public string Data { get; set; }
                public bool IsTerm { get; set; }
                public Dictionary<VomEdge, int> AdjList { get; set; } = [];

                public VomNode(string data = "NewNode", bool isTerm = false)
                {
                    Data = data;
                    IsTerm = isTerm;
                }
                public VomNode()
                {
                    Data = "NewNode";
                    IsTerm = false;
                }

                /// <summary>
                /// Only used as JSON deserialization utility.
                /// Will act weird around collisions in existing graph
                /// </summary>
                public bool AppendAdj(string data, int weight = 1)
                {
                    if(string.IsNullOrWhiteSpace(data))
                    {
                        return false;
                    }
                    VomNode nv = new(data);
                    return AdjList.TryAdd(new(this, nv), weight);
                }

                public bool AppendAdj(VomNode vm, int weight = 1)
                {
                    return AdjList.TryAdd(new(this, vm), weight);
                }
                
                public override string ToString()
                {
                    string res = (
                        $"Data: {Data}\n" +
                        $"AdjList: \n"
                        );
                    foreach(VomEdge edge in AdjList.Keys)
                    {
                        res += $"\tData: {edge.End.Data}\n\tWeight: {AdjList[edge]}\n";
                    }
                    return res;
                }

                //// Formatting idea:
                //// Start with data, create arr of each "next" word with edge followed by a weight int
                //// The adjlist can be easily split by a pipe "|" into subarrays
                //// Last char (which needs to be popped) marks the parent node as term
                //public string JsonFormat()
                //{
                //    StringBuilder sb = new(Data);
                //    foreach (VomEdge edge in AdjList.Keys)
                //    {
                //        sb.Append($"|{edge.End.Data}{AdjList[edge]}");
                //    }
                //    sb.Append(IsTerm ? "T" : "F");
                //    return sb.ToString();
                //}

                public bool Equals(VomNode? obj)
                {
                    obj = obj ?? new();
                    return (Data == obj.Data);
                }

                public override bool Equals(object? obj)
                {
                    VomNode n = obj as VomNode ?? new VomNode();
                        return (Data == n.Data);
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(Data);
                }

                public static bool operator==(VomNode n1, VomNode n2)
                {
                    return n1.Data == n2.Data;
                } 

                public static bool operator!=(VomNode n1, VomNode n2)
                {
                    return n1.Data != n2.Data;
                }

            }

            public class VomEdge : IEquatable<VomEdge> 
            {
                public VomNode Start { get; set; }
                public VomNode End { get; set; }

                public VomEdge(VomNode start, VomNode end)
                {
                    Start = start;
                    End = end;
                }

                public VomEdge()
                {
                    Start = new("DBG_EMPTY");
                    End = new("DBG_EMPTY");
                }

                public bool Equals(VomEdge? obj)
                {
                    obj = obj ?? this;
                    return (Start == obj.Start && End == obj.End);
                }

                public override bool Equals(object? obj)
                {
                    VomEdge e = obj as VomEdge ?? new VomEdge(new(), new());
                    return (Start == e.Start && End == e.End);
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(Start, End);
                }

                public static bool operator ==(VomEdge e1, VomEdge e2)
                {
                    return (e1.Start == e2.Start && e1.End == e2.End);
                }

                public static bool operator !=(VomEdge e1, VomEdge e2)
                {
                    return !(e1.Start == e2.Start && e1.End == e2.End);
                }
            }
        }

        public class VomarkReader
        {
            private enum STRING_TERMINAL { 
                TERM_PD = '.',
                TERM_EX = '!',
                TERM_QU = '?'
            }

            private static string ReadTxt(string path)
            {
                string txt = "";
                try
                {
                    if (Path.GetExtension(path) != ".txt")
                    {
                        throw new ArgumentException("Input file must be a txt file.");
                    }

                    using (StreamReader sr = new(path))
                    {
                        txt = sr.ReadToEnd();
                    }
                    txt = SanitizeText(txt);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.ToString());
                }
                return txt;
            }

            public static VomGraph? GraphFromString(string data, string graphName = "NewGraph", bool sanitized = false)
            {
                if(!sanitized)
                {
                    data = SanitizeText(data);
                }
                try
                {
                    string[] words = data.Split(' ');
                    HashSet<string> usedWords = [];
                    VomGraph vg = new(graphName);
                    VomNode parent = vg.Root;
                    bool isTerm = false;
                    foreach (string word in words)
                    {
                        string inp = word;
                        if(!string.IsNullOrWhiteSpace(word))
                        {
                            isTerm = Enum.IsDefined(typeof(STRING_TERMINAL), (STRING_TERMINAL)word.Last<char>());
                            if (isTerm)
                            {
                                inp = word.TrimEnd(word[word.Length - 1]);
                            }
                            VomNode curr = usedWords.Add(inp) ?
                                new VomNode(inp, isTerm) : vg.FindNode(inp);
                            vg.AddNode(curr, parent);
                            if (isTerm)
                            {
                                vg.AddNode(vg.Term, curr);
                                parent = vg.Root;
                            }
                            else
                            {
                                parent = curr;
                            }
                        }
                    }
                    return vg;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.ToString());
                }
                return null;
            }

            private static string SanitizeText(string data)
            {
                return Regex.Replace(data, "[//\\\\<>%$#@&*()]", " ").ToLower().Trim();
            }

            //TODO: Test method and minimize repeated code where possible
            public static bool AppendGraph(string data, VomGraph graph)
            {
                if(graph == null || data == null)
                {
                    return false;
                }

                data = SanitizeText(data);

                try
                {
                    string[] words = data.Split(' ');
                    HashSet<string> usedWords = new();
                    VomNode parent = graph.Root;
                    bool isTerm = false;
                    foreach (string word in words)
                    {
                        string inp = word;
                        if (!string.IsNullOrWhiteSpace(word))
                        {
                            isTerm = Enum.IsDefined(typeof(STRING_TERMINAL), (STRING_TERMINAL)word.Last<char>());
                            if (isTerm)
                            {
                                inp = word.TrimEnd(word[word.Length - 1]);
                            }
                            VomNode curr = usedWords.Add(inp) ?
                                new VomNode(inp, isTerm) : graph.FindNode(inp);
                            graph.AddNode(curr, parent);
                            if (isTerm)
                            {
                                graph.AddNode(graph.Term, curr);
                                parent = graph.Root;
                            }
                            else
                            {
                                parent = curr;
                            }
                        }
                    }
                    return true;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.ToString());
                    return false;
                }

            }

            public static VomGraph? GraphFromTxt(string path, string graphName = "NewGraph")
            {
                string data = ReadTxt(path);
                return GraphFromString(data, graphName, true);
            }

            public static VomGraph? GraphFromJson(string path, string graphName)
            {
                string data = File.ReadAllText($"{path}/{graphName}.json");
                return JsonSerializer.Deserialize<VomGraph>(data);
            }
        }
        
    }

    public class NodeJsonConverter : JsonConverter<VomNode>
    {
        // For consistency in object references, node adjlist should be handled in graph deserialization
        public override VomNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if(reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            using JsonDocument doc = JsonDocument.ParseValue(ref reader); 
            {
                JsonElement root = doc.RootElement;
                string data = root.GetProperty("data").GetString() ?? "ERR_EMPTY";
                bool isTerm = root.GetProperty("isterm").GetBoolean();
                VomNode vm = new(data, isTerm);
                return vm;
            }
        }


        public override void Write(Utf8JsonWriter writer, VomNode node, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("data", node.Data);
            writer.WriteBoolean("isterm", node.IsTerm);
            writer.WriteStartArray("adjlist");
            foreach(VomEdge edge in node.AdjList.Keys)
            {
                writer.WriteStartObject();
                writer.WriteString("next", edge.End.Data);
                writer.WriteNumber("weight", node.AdjList[edge]);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }

    public class GraphJsonConverter : JsonConverter<VomarkUtil.VomGraph>
    {
        public override VomarkUtil.VomGraph? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            {
                JsonElement root = doc.RootElement;
                string label = root.GetProperty("label").GetString() ?? "ERR_NULL";
                VomarkUtil.VomGraph vg = new(label, true);
                //List<VomNode> nodes = [];
                Dictionary<string, VomNode> nodes = [];
                foreach(JsonElement js in root.GetProperty("nodes").EnumerateArray())
                {
                    VomNode next = JsonSerializer.Deserialize<VomNode>(js)
                        ?? throw new FormatException("Cannot create node from data");
                    nodes.TryAdd(next.Data, next);
                }

                foreach(JsonElement js in root.GetProperty("nodes").EnumerateArray())
                {
                    //VomNode? curr = nodes.Find(js.GetProperty("data").GetString());
                    string currData = js.GetProperty("data").GetString()
                        ?? throw new FormatException("Could not extract node data from json");
                    VomNode curr = nodes[currData];
                    foreach (JsonElement el in js.GetProperty("adjlist").EnumerateArray())
                    {
                        string childData = el.GetProperty("next").GetString()
                            ?? throw new FormatException("Could not extract node data from json (adjlist)"); ;
                        VomNode child = nodes[childData]
                            ?? throw new FormatException("Cannot retrieve child node");
                        int weight = el.GetProperty("weight").GetInt32();
                        //vg.AddNode(child, curr, weight, nodes.Values.ToList());
                        vg.AddJsonNode(nodes, child, curr, weight);
                    }
                }

                vg.Nodes = nodes.Values.ToList();
                vg.Root = nodes["__DBG__NULL__"]
                    ?? throw new FormatException("No root node present");
                vg.Term = nodes["__DBG__TERM__"]
                    ?? throw new FormatException("No terminal node present");
                //Debug.WriteLine("FINAL NODES");
                //foreach(VomNode node in vg.Nodes)
                //{
                //    Debug.WriteLine(node);
                //}
                return vg;
            }
        }

        public override void Write(Utf8JsonWriter writer, VomarkUtil.VomGraph graph, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("label", graph.Label);
            writer.WriteStartArray("nodes");
            foreach (VomNode node in graph.Nodes)
            {
                JsonSerializer.Serialize(writer, node);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
