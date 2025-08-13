using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using YoutubeDLSharp.Metadata;
using System.Configuration;
using System.Collections.Specialized;
using static vomark.app.VomarkUtil;

namespace vomark.app
{
    public static class YTIntegration
    {

        private readonly static OptionSet subExtractOptions = new()
        {
            WriteSubs = true,
            WriteAutoSubs = true,
            SubLangs = ConfigurationManager.AppSettings.Get("SubLang") ?? "en",
            SubFormat = "srt",
            SkipDownload = true
        };

        public static async Task<bool> SetupPackages(string path)
        {
            string[] files = Directory.GetFiles(path);
            try
            {
                if (!files.Contains($"{path}\\yt-dlp.exe")) { await Utils.DownloadYtDlp(path); Debug.WriteLine("Downloaded yt-dlp!"); }
                if (!files.Contains($"{path}\\ffmpeg.exe")) { await Utils.DownloadFFmpeg(path); Debug.WriteLine("Downloaded ffmpeg!"); }
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
            string text = "";
            await yt.RunVideoDownload(url, overrideOptions: subExtractOptions);
            string file = Directory.GetFiles(yt.OutputFolder, "*.srt").FirstOrDefault()
                ?? throw new ArgumentException("No file found in default output folder");
            text = SRTParser.SRTToString(file);
            return text;
        }

        public static async Task<bool> YTAppendGraph(YoutubeDL yt, string url, VomGraph vg)
        {
            bool complete = false;
            try
            {
            string data = await FetchSubtitles(yt, url);
            complete = VomarkReader.AppendGraph(data, vg);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            return complete;
        }
    }

    internal static class SRTParser
    {
        private static List<string>? Parse(string path)
        {
            string[] lines = File.ReadAllLines(path);
            List<string> parsedLines = [];
            int lineIdx = 1;
            int count;
            int oldCount = 0;
            bool isGenerated = false;
            for(int i = 0; i <  lines.Length; i++)
            {
                if (lines[i] == lineIdx.ToString())
                {
                    ++lineIdx;
                    int j = i + 2;
                    
                    // To avoid the one massive run-on caused by older auto-captions,
                    // we'll spend extra time on all generated captions and risk awkward sentence fragments.
                    // Shouldn't be a problem with newer YT vids with auto-captions.
                    
                    while (!string.IsNullOrWhiteSpace(lines[j]))
                    {

                        // Assuming all SRT brackets are single-lined.
                        if (lines[j].First() != '[') { parsedLines.Add(lines[j]); }
                        ++j;
                    }
                    // If the end of a block doesn't include punctuation, assume the entire file is auto-generated.
                    count = parsedLines.Count;
                    Debug.WriteLine($"Output count: {count}");
                    if (count > oldCount && (isGenerated || !Char.IsPunctuation(parsedLines[count - 1].Last()))) 
                    {
                        Debug.WriteLine($"Curr line {parsedLines[count - 1]} at idx {count - 1}");
                        parsedLines[count - 1] += '.'; 
                        isGenerated = true; 
                    }
                    oldCount = count;
                    i = j;
                }
            }
            return parsedLines;
        }

        public static string SRTToString(string path)
        {
            List<string> lines = Parse(path)
                ?? throw new ArgumentException("Could not produce string list from given path");
            // Housekeeping to ensure that there is only one file in the directory at any given time
            // MAY NEED TO BE AMENDED FOR ASYNC PLAYLIST READING
            File.Delete(path);
            return VomarkReader.SanitizeText(String.Join(" ", lines));
        }


    }
}
