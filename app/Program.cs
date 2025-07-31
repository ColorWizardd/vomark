using static vomark.app.VomarkUtil;

string content = "/This 'is <a> test!\\ This is two tests!";
VomGraph ex = VomarkReader.GraphFromString(content, "StringTest");
