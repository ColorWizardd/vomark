using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using static vomark.app.VomarkUtil;

namespace vomark.app
{
    public static class YTIntegration
    {

        private static readonly OptionSet DlOpts = new()
        {
            WriteSubs = true,
            SubFormat = "srt"
        };

        public static async Task<bool> SetupPackages(string path)
        {
            try
            {
                await Utils.DownloadFFmpeg(path);
                await Utils.DownloadYtDlp(path);
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
        }

        public static async Task<string> FetchSubtitles(YoutubeDL yt, string url)
        {
            StringBuilder sb = new();
            var res = await yt.RunVideoDataFetch(url, default, true, false, DlOpts);
            var subList = res.Data.Subtitles ?? res.Data.AutomaticCaptions ??
                throw new ArgumentException("Failed to extract subtitle data from URL");
            foreach (var sub in subList)
            {
                foreach (var next in sub.Value)
                {
                    // Assuming data is in SRT format
                    // TODO: Extend regex to other formats
                    string sanitized = Regex.Replace(next.Data, "/^\\d+\\n(\\d{2}:\\d{2}:\\d{2},\\d{3} --> \\d{2}:\\d{2}:\\d{2},\\d{3})\\n/gm","");
                    sanitized = VomarkReader.ReadTxt(sanitized);
                    sb.Append(sanitized + ' ');
                }
            }
            return sb.ToString();
        }

        public static async Task<bool> YTAppendGraph(YoutubeDL yt, string url, VomGraph vg)
        {
            string data = await FetchSubtitles(yt, url);
            try
            {
                VomarkReader.AppendGraph(data, vg);
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
        }
    }
}
