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

        public VomContext(string label,  int puncFlag = (int)Punctuation.PERIOD, int capFlag = (int)Capitalization.SMALL_CAPS) 
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
            TRIP_EXC = 8
        }
        [Flags]
        public enum Capitalization
        {
            SMALL_CAPS = 1,
            FIRST_CAP = 2,
            ALL_CAPS = 4,
        }

        // Probably the "easiest" way to stack punctuation

        public string GetPunctuation()
        {
            StringBuilder sb = new();
            sb.Append(((int)Punctuation.PERIOD & PunctuationFlag) == (int)Punctuation.PERIOD ? '.' : "");
            sb.Append(((int)Punctuation.QUEST & PunctuationFlag) == (int)Punctuation.QUEST ? '?' : "");
            sb.Append(((int)Punctuation.EXC & PunctuationFlag) == (int)Punctuation.EXC ? '!' : "");
            sb.Append(((int)Punctuation.TRIP_EXC & PunctuationFlag) == (int)Punctuation.TRIP_EXC ? "!!!" : "");
            return sb.ToString();
        }

        public string ApplyCapitalization(string inp)
        {
            inp = ((int)Capitalization.SMALL_CAPS & CapitalizationFlag) == (int)Capitalization.SMALL_CAPS ? inp.ToLower() : inp;
            inp = ((int)Capitalization.FIRST_CAP & CapitalizationFlag) == (int)Capitalization.FIRST_CAP ? 
                (char.ToUpper(inp[0]) + inp.Substring(1)) : inp;
            inp = ((int)Capitalization.ALL_CAPS & CapitalizationFlag) == (int)Capitalization.ALL_CAPS ? inp.ToUpper() : inp;
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