using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace vomark.app
{
    public class VomarkUtil
    {

        public class VomGraph
        {
            private readonly VomNode root = new("__DBG__NULL__", false);
            private readonly VomNode term = new("__DBG__TERM__", true);
            private readonly string graphLabel;
            public List<VomNode> nodes = [];

            public VomGraph(string graphLabel)
            {
                this.graphLabel = graphLabel;
                nodes.Add(GetRoot());
                nodes.Add(GetTerm());
            }

            public void AddNode(VomNode newNode, VomNode? parent = null, int weight = 1)
            {

                parent = parent ?? GetRoot();

                VomEdge rel = new(parent, newNode);

                if (nodes.Contains(newNode))
                {
                    if (parent.GetAdjList().ContainsKey(rel))
                    {
                        parent.GetAdjList()[rel] += weight;
                    }
                    else
                    {
                        parent.GetAdjList().Add(rel, weight);
                    }

                }
                else
                {
                    nodes.Add(newNode);
                    parent.GetAdjList().Add(rel, weight);
                }
            }

            public VomNode FindNode(string data)
            {
                return nodes.Find(x => x.GetData() == data) ?? 
                    throw new ArgumentException($"{data} is not a valid node");
            }

            public string? FormSentence(int maxLen, char punc, VomNode? start)
            {
                if(this == null)
                {
                    return null;
                }
                StringBuilder sentence = new();
                start = start ?? root;
                VomNode curr = start;
                int wordCount = 0;
                try
                {
                    while (wordCount < maxLen)
                    {
                        curr = GetNextNode(curr);
                        if (curr.Equals(GetTerm())) 
                        { 
                            break; 
                        }
                        sentence.Append($"{curr.GetData()} ");
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
                Dictionary<VomEdge, int> adj = curr.GetAdjList();
                if(adj.Count <= 0)
                {
                    return GetTerm();
                }
                int weightSum = adj.Values.Sum();
                return GetNextRand(adj, weightSum).GetEnd();
            }

            private static VomEdge GetNextRand(Dictionary<VomEdge, int> adj, int weightSum)
            {
                if(adj.Count <= 0)
                {
                    throw new NullReferenceException($"Node containing adjList is a terminal node."); ;
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

            public VomNode GetRoot()
            {
                return root;
            }

            public VomNode GetTerm()
            {
                return term;
            }

            public string GetLabel()
            {
                return graphLabel;
            }

            public bool ToXMLFile(string path)
            {
                bool complete = false;
                try
                {
                    using FileStream fs = File.Create(path + GetLabel() + ".xml");
                    System.Xml.Serialization.XmlSerializer x = new(typeof(VomGraph));
                    x.Serialize(fs, this);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
                return complete;
            }



            public class VomNode(string data = "", bool isTerm = false) : IEquatable<VomNode>
            {
                private readonly string data = data;
                private Dictionary<VomEdge, int> adjList = [];
                private bool isTerm = isTerm;

                public Dictionary<VomEdge, int> GetAdjList()
                {
                    return adjList;
                }

                public string GetData()
                {
                    return data;
                }

                public bool GetIsTerm()
                {
                    return isTerm;
                }

                public void MakeTerm(bool state)
                {
                    isTerm = state;
                }
                
                public override string ToString()
                {
                    string res = (
                        $"Data: {data}\n" +
                        $"AdjList: \n"
                        );
                    foreach(VomEdge edge in adjList.Keys)
                    {
                        res += $"\tData: {edge.GetEnd().data}\n\tWeight: {adjList[edge]}\n";
                    }
                    return res;
                }

                public bool Equals(VomNode? obj)
                {
                    obj = obj ?? new();
                    return (data == obj.data);
                }

                public override bool Equals(object? obj)
                {
                    VomNode n = obj as VomNode ?? new VomNode();
                        return (data == n.data);
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(data, adjList, isTerm);
                }

                public static bool operator==(VomNode n1, VomNode n2)
                {
                    return n1.data == n2.data;
                } 

                public static bool operator!=(VomNode n1, VomNode n2)
                {
                    return n1.data != n2.data;
                }

            }

            public class VomEdge(VomNode start, VomNode end) : IEquatable<VomEdge> 
            {
                private readonly VomNode start = start;
                private readonly VomNode end = end;

                public VomNode GetStart()
                {
                    return start;
                }

                public VomNode GetEnd()
                {
                    return end;
                }

                public bool Equals(VomEdge? obj)
                {
                    obj = obj ?? this;
                    return (start == obj.start && end == obj.end);
                }

                public override bool Equals(object? obj)
                {
                    VomEdge e = obj as VomEdge ?? new VomEdge(new(), new());
                    return (start == e.start && end == e.end);
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(start, end);
                }

                public static bool operator ==(VomEdge e1, VomEdge e2)
                {
                    return (e1.start == e2.start && e1.end == e2.end);
                }

                public static bool operator !=(VomEdge e1, VomEdge e2)
                {
                    return !(e1.start == e2.start && e1.end == e2.end);
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
                    VomGraph.VomNode parent = vg.GetRoot();
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
                            VomGraph.VomNode curr = usedWords.Add(inp) ?
                                new VomGraph.VomNode(inp, isTerm) : vg.FindNode(inp);
                            vg.AddNode(curr, parent);
                            if (isTerm)
                            {
                                vg.AddNode(vg.GetTerm(), curr);
                                parent = vg.GetRoot();
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

            public static VomGraph? FromXML(string path)
            {
                try
                {
                System.Xml.Serialization.XmlSerializer x = new(typeof(VomGraph));
                using StringReader xmlReader = new(path);
                VomGraph vg = (VomGraph?)x.Deserialize(xmlReader) ??
                    throw new InvalidOperationException();
                return vg;
                }
                catch(Exception e)
                {
                    Debug.WriteLine(e.ToString());
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
                    VomGraph.VomNode parent = graph.GetRoot();
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
                            VomGraph.VomNode curr = usedWords.Add(inp) ?
                                new VomGraph.VomNode(inp, isTerm) : graph.FindNode(inp);
                            graph.AddNode(curr, parent);
                            if (isTerm)
                            {
                                graph.AddNode(graph.GetTerm(), curr);
                                parent = graph.GetRoot();
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
}
