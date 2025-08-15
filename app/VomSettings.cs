using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vomark.app
{
    public class VomSettings
    {
        public string YtDlPath { get; set; }
        public string YtOutPath {  get; set; }
        public string SubLang { get; set; }
        public int MaxSentenceLen {  get; set; }

        public VomSettings
            (
            string ytDl = "../packages",
            string ytOut = "../ytdir/out",
            string subLang = "en",
            int maxLen = 32
            )
        {
            YtDlPath = ytDl;
            YtOutPath = ytOut;
            SubLang = subLang;
            MaxSentenceLen = maxLen;
        }

    }
}
