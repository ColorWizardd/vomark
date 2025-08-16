# vomark
A markov-chain based random sentence generator library. Create a "brain" that you can feed a vocabulary and reproduce sentences!
**NOTE**: This is **NOT** an LLM, and does not use any other data than what you specifically answer. It literally only uses the words you give it and reproduces patterns given a vocabulary.
### Features
- Each entity comprises of a **VomBrain**, **VomContext**, and **VomSettings**.
	- A VomBrain maps a label to a weighted graph representing an associated vocabulary.
	- A VomContext can be used to represent style and emotion, notably through capitalization and punctuation.
	- VomSettings are used to manage a max word-count per sentence, as well as settings for YouTube integration.
- Entities can be fed data from raw strings, .txt files, or YouTube videos/playlists.
### Installation
Vomark can be installed as a NuGet package:
`Install-Package vomark`
YouTube integration features are dependent on [YoutubeDLSharp](https://github.com/Bluegrams/YoutubeDLSharp), and conversely require [yt-dlp](https://github.com/yt-dlp/yt-dlp) and [ffmpeg](https://ffmpeg.org/download.html).
Both packages can be installed within the library by running the following:
```csharp
bool success = await YTIntegration.SetupPackages(path);
```
### Usage
Creating a fresh VomBrain:
```csharp
VomBrain vb = new();
```
Adding a graph to a VomBrain:
```csharp
string ex = "This is an example string."
vb.AddNewGraph("example", ex);
// Existing graphs can append data as such:
string ex2 = "This is a second string."
vb.AddThoughtRaw("example", ex2);
```
When using .txt files, instead use `AddThoughtTxt()` and use the path instead of the data.

Returning a sentence from the VomBrain:
```csharp
Console.WriteLine(vb.FormThought("example"));
```
When forming sentences from a VomBrain, it uses the maximum word length from the associated settings object as well as capitalization/punctuation from the context object.

Changing the settings and context:
```csharp
vb.Settings = new(
	ytDl: "../ytlibs",
	ytOut = "../vidOut",
	subLang = "en",
	maxLen = 16
);

VomContext Angry = new(
	"angry", Punctuation.TRIPEXC, Capitalization.ALLCAPS
);
vb.Context = Angry;
```
### YouTube Integration
If YoutubeDLSharp and all dependencies are installed, then you can use YouTube videos and playlists as data sources for VomBrains. When using YT features, ensure that your VomBrain settings are applied correctly. This ensures that your ytDl points to the folder containing yt-dlp.exe and ffmpeg.exe.
#### A Quick Warning on Subtitles
The vomark library requires that a YouTube video have at least one SRT file in the given language in order to be read. Many automated subtitles will work fine with vomark, but custom-made subtitles are not forced to follow any particular standard. All text between brackets and parentheses are removed, so sentence structures may become awkward. YouTube automatically censors certain language, so it can create gaps in speech and unexpected connections between words.

Adding a YouTube video to a graph:
```csharp
await vb.AddThoughtYt("example", "example_url_");
// Adding a playlist follows the same format, but uses
// await vb.AddThoughtPlaylistYt("example", "example_url_");
```
### Tests
Testing is implemented locally in a separate project using xUnit.

### License
This project is licensed under the [BSD-3 Claude License](LICENSE.txt)