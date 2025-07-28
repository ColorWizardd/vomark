using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;
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
            public List<VomNode> nodes = [];

            public VomGraph(string graphLabel)
            {
                Label = graphLabel;
                nodes.Add(Root);
                nodes.Add(Term);
            }

            public VomGraph()
            {
                Label = "NewGraph";
                nodes.Add(Root);
                nodes.Add(Term);
            }

            public void AddNode(VomNode newNode, VomNode? parent = null, int weight = 1)
            {

                parent = parent ?? Root;

                VomEdge rel = new(parent, newNode);

                if (nodes.Contains(newNode))
                {
                    if (parent.AdjList.ContainsKey(rel))
                    {
                        parent.AdjList[rel] += weight;
                    }
                    else
                    {
                        parent.AdjList.Add(rel, weight);
                    }

                }
                else
                {
                    nodes.Add(newNode);
                    parent.AdjList.Add(rel, weight);
                }
            }

            public VomNode FindNode(string data)
            {
                return nodes.Find(x => x.Data == data) ?? 
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
                            break; 
                        }
                        sentence.Append($"{curr.Data} ");
                        ++wordCount;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                string res = sentence.ToString().Trim();
                //DELETE
                //Debug.WriteLine($"FULL SENTENCE: {res}");
                return res + punc;
            }

            public VomNode GetNextNode(VomNode curr)
            {
                Dictionary<VomEdge, int> adj = curr.AdjList;
                if(adj.Count <= 0)
                {
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

                public VomNode(string data = "NewNode", bool isTerm=false)
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
                    return HashCode.Combine(Data, AdjList, IsTerm);
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
        }
        
    }

    public class NodeJsonConverter : JsonConverter<VomNode>
    {
        public override VomNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if(reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            using JsonDocument doc = JsonDocument.ParseValue(ref reader); {
                JsonElement root = doc.RootElement;
                string data = root.GetProperty("data").GetString() ?? "ERR_EMPTY";
                bool isTerm = root.GetProperty("isterm").GetBoolean();
                VomNode vm = new(data, isTerm);
                foreach(JsonElement js in root.GetProperty("adjlist").EnumerateArray())
                {
                    string next = js.GetProperty("next").GetString()
                        ?? throw new FormatException("No next node found");
                    int weight = js.GetProperty("weight").GetInt32();
                    vm.AppendAdj(next, weight);
                }
                return vm;
            }
        }


        public override void Write(Utf8JsonWriter writer, VomNode node, JsonSerializerOptions options)
        {
            writer.WriteStartObject("node");
            writer.WriteString("data", node.Data);
            writer.WriteBoolean("isterm", node.IsTerm);
            writer.WriteStartArray("adjlist");
            foreach(VomEdge edge in node.AdjList.Keys)
            {
                writer.WriteStartObject("edge");
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
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, VomarkUtil.VomGraph graph, JsonSerializerOptions options)
        {
            writer.WriteStartObject("graph");
            writer.WriteString("label", graph.Label);
            writer.WriteStartObject("node_root");
            writer.WriteString("data", graph.Root.Data);
            writer.WriteBoolean("isterm", false);
            writer.WriteEndObject();
            writer.WriteStartObject("node_term");
            writer.WriteString("data", graph.Term.Data);
            writer.WriteBoolean("isterm", true);
            writer.WriteEndObject();
            writer.WriteStartArray("nodes");
            foreach (VomNode node in graph.nodes)
            {
                JsonSerializer.Serialize(writer, node);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
