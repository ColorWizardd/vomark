using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static vomark.app.VomarkUtil;
using static vomark.app.VomContext;

namespace vomark.app
{
    /// <summary>
    /// Holds information about the punctuation/capitalization state of a given "context".
    /// Can be associated with one or many graphs.
    /// </summary>
    public class VomContext
    {
        public string Label { get; set; }
        public int PunctuationFlag { get; set; }
        public int CapitalizationFlag { get; set; }

        public VomContext(string label,  int puncFlag = (int)Punctuation.PERIOD, int capFlag = (int)Capitalization.SMALLCAPS) 
        { 
            Label = label;
            PunctuationFlag = puncFlag;
            CapitalizationFlag = capFlag;
        }

        [Flags]
        public enum Punctuation
        {
            PERIOD = 1,
            QUEST = 2,
            EXC = 4,
            TRIPEXC = 8
        }
        [Flags]
        public enum Capitalization
        {
            SMALLCAPS = 1,
            FIRSTCAP = 2,
            ALLCAPS = 4,
        }

        // Probably the "easiest" way to stack punctuation

        public string GetPunctuation()
        {
            StringBuilder sb = new();
            sb.Append(((int)Punctuation.PERIOD & PunctuationFlag) == (int)Punctuation.PERIOD ? '.' : "");
            sb.Append(((int)Punctuation.QUEST & PunctuationFlag) == (int)Punctuation.QUEST ? '?' : "");
            sb.Append(((int)Punctuation.EXC & PunctuationFlag) == (int)Punctuation.EXC ? '!' : "");
            sb.Append(((int)Punctuation.TRIPEXC & PunctuationFlag) == (int)Punctuation.TRIPEXC ? "!!!" : "");
            return sb.ToString();
        }

        public string ApplyCapitalization(string inp)
        {
            inp = ((int)Capitalization.SMALLCAPS & CapitalizationFlag) == (int)Capitalization.SMALLCAPS ? inp.ToLowerInvariant() : inp;
            inp = ((int)Capitalization.FIRSTCAP & CapitalizationFlag) == (int)Capitalization.FIRSTCAP ? 
                (char.ToUpperInvariant(inp[0]) + inp.Substring(1)) : inp;
            inp = ((int)Capitalization.ALLCAPS & CapitalizationFlag) == (int)Capitalization.ALLCAPS ? inp.ToUpperInvariant() : inp;
            return inp;
        }

        public void AddPuncFlag(int newFlag)
        {
            PunctuationFlag |= newFlag;
        }

        public void RemovePuncFlag(int flag)
        {
            // Checking if flag is valid for removal
            if((PunctuationFlag & flag) == flag) { PunctuationFlag -= flag; }
        }

        //1001 - 0001
    }
}