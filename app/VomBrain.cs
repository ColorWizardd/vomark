using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeDLSharp;
using static vomark.app.VomarkUtil;
using static vomark.app.VomContext;
using static vomark.app.YTIntegration;

namespace vomark.app
{
    public class VomBrain
    {
        public Dictionary<string, VomGraph> GraphList { get; set; }
        public VomContext Context { get; set; }

        public VomBrain()
        {
            GraphList = [];
            Context = new("__DBG_CONTEXT_DEFAULT__");
        }

        public void AddNewGraph(string label)
        {
            GraphList.Add(label, new(label));
        }

        public void AddNewGraph(string label, string data)
        {
            GraphList.Add(label, VomarkReader.GraphFromString(data, label)
                ?? throw new ArgumentException("Could not produce graph from data"));
        }

        public void AddNewGraphFromTxt(string label, string path)
        {
            GraphList.Add(label, VomarkReader.GraphFromTxt(path, label)
                ?? throw new ArgumentException("Could not produce graph from data"));
        }

        // CAUTION: New graph label should align with label in JSON.
        public void AddNewGraphFromJson(string label, string path)
        {
            GraphList.Add(label, VomarkReader.GraphFromJson(path, label)
                ?? throw new ArgumentException("Could not produce graph from data"));
        }

        public string FormThought(string label)
        {
            VomGraph vg = GraphList[label];
            string punc = Context.GetPunctuation();
            // TODO: Tests cannot pull from App.config?
            int maxLen = Convert.ToInt32(ConfigurationManager.AppSettings["MaxSentenceLen"]);
            Debug.WriteLine($"MAXLEN: {maxLen}");
            string res = vg.FormSentence(maxLen, punc, null)
                ?? throw new ArgumentException("Could not produce sentence from given graph.");
            return Context.ApplyCapitalization(res);
        }

        public bool AddThoughtRaw(string label, string data)
        {
            VomGraph vg = GraphList[label];
            return VomarkReader.AppendGraph(data, vg);
        }

        public bool AddThoughtTxt(string label, string path)
        {
            VomGraph vg = GraphList[label];
            string data = File.ReadAllText(path);
            return VomarkReader.AppendGraph(data, vg);
        }

        public async Task<bool> AddThoughtYt(string label, string url)
        {
            string YtDir = ConfigurationManager.AppSettings.Get("YtOutPath") ?? "../ytdir";
            YoutubeDL yt = new()
            {
                YoutubeDLPath = $"{YtDir}/yt-dlp.exe",
                FFmpegPath = $"{YtDir}/ffmpeg.exe",
                OutputFolder = $"{YtDir}/out"
            };
            VomGraph vg = GraphList[label];
            return await YTAppendGraph(yt, url, vg);
        }

        public async Task<bool> AddThoughtPlaylistYt(string label, string url)
        {
            string YtDir = ConfigurationManager.AppSettings.Get("YtOutPath") ?? "../ytdir";
            YoutubeDL yt = new()
            {
                YoutubeDLPath = $"{YtDir}/yt-dlp.exe",
                FFmpegPath = $"{YtDir}/ffmpeg.exe",
                OutputFolder = $"{YtDir}/out"
            };
            VomGraph vg = GraphList[label];
            return await YTPlaylistAppendGraph(yt, url, vg);
        }
    }
}
