/// TODO:
///     Write text reader class
///     Write tests for util and reader classes
///     

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace vomark.app
{
    internal class VomarkUtil
    {

        public class VomGraph
        {
            public VomNode root = new();
            public VomNode term = new();
            public List<VomNode> nodes = [];


            public void AddNode(VomNode newNode, VomNode? parent = null, int weight = 1)
            {

                parent = parent ?? root;

                VomEdge rel = new(parent, newNode);

                if (nodes.Contains(newNode))
                {
                    if (parent.GetAdjList().ContainsKey(rel))
                    {
                        parent.GetAdjList()[rel] += weight;
                    }
                }
                else
                {
                    nodes.Add(newNode);
                    parent.GetAdjList().Add(rel, weight);
                }
            }

            public String FormSentence(int maxLen, char punc, VomNode? start)
            {
                StringBuilder sentence = new();
                start = start ?? root;
                VomNode curr = start;
                int wordCount = 0;
                try
                {
                    while (wordCount < maxLen)
                    {
                        curr = GetNextNode(curr);
                        if (curr.Equals(term)) 
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
                sentence.Append(punc);
                return sentence.ToString();
            }

            public VomNode GetNextNode(VomNode curr)
            {
                Dictionary<VomEdge, int> adj = curr.GetAdjList();
                if(adj.Count <= 0)
                {
                    return term;
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

            public class VomNode
            {
                private readonly string data;
                private Dictionary<VomEdge, int> adjList = [];

                public VomNode(string data = "")
                {
                    this.data = data;
                }

                public Dictionary<VomEdge, int> GetAdjList()
                {
                    return adjList;
                }

                public string GetData()
                {
                    return data;
                }

            }

            public class VomEdge
            {
                private readonly VomNode start;
                private readonly VomNode end;

                public VomEdge(VomNode start, VomNode end)
                {
                    this.start = start;
                    this.end = end;
                }

                public VomNode GetStart()
                {
                    return start;
                }

                public VomNode GetEnd()
                {
                    return end;
                }
            }
        }

        public class VomarkReader
        {
            /// TODO:
            /// Process text input into nodes/edges using VomarkGraph
            /// Clean text, removing punctuation and capitalization
            /// Separate process for large text files?
            ///     Assuming PWYK usage:
            ///     Ensure that it can read/build quickly with smaller text chunks
            ///     YoutubeDL C# for subtitle extraction?
        }
    }
}
