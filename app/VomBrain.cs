using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static vomark.app.VomarkUtil;

namespace vomark.app
{
    public class VomBrain
    {
        public List<VomGraph> GraphList { get; set; } = [];
        // Include a "context" class where:
            // Contains a label - same as the respective graph
            // Contains an "emotion" - dictates normal/all-caps and punctuation
        public VomGraph? FindGraph(string label)
        {
            return GraphList.First(x => label == x.Label) ?? null;
        }
    }
}
