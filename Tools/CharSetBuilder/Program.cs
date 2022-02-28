
var sourceFolder = "/Users/jackil/Desktop/Pal3 Dialogues";

var charSet = new Dictionary<char, bool>();

foreach (var textFile in new DirectoryInfo(sourceFolder).GetFiles("*.txt", SearchOption.AllDirectories))
{
    var text = File.ReadAllText(textFile.FullName);

    foreach (var ch in text.Where(ch => !char.IsControl(ch)))
    {
        charSet[ch] = true;
    }
}

// Append all ASCII chars
foreach (var ascii in Enumerable.Range('\x1', 127).ToArray())
{
    char ch = (char) ascii;
    if (!char.IsControl(ch)) charSet[ch] = true;
}

var charset = charSet.Keys.ToList();
charset.Sort();

File.WriteAllText($"{sourceFolder}{Path.DirectorySeparatorChar}charset.txt",
    new string(charset.ToArray()));

