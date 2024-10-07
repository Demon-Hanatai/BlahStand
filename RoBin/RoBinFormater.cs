using System.Text.RegularExpressions;

public class RoBinFormater
{
    public string Format(string fullCode)
    {
        // Step 1: Process and remove EQU statements
        fullCode = ProcessEqu(fullCode);

        // Step 2: Remove all comments (single-line and multi-line)
        fullCode = RemoveComments(fullCode);

        // Step 3: Process Imports from files
        fullCode = ProcessImports(fullCode);

        // Step 4: Convert the entire code into one line
        fullCode = ConvertToOneLine(fullCode);
        fullCode = System.Text.RegularExpressions.Regex.Replace(fullCode, @"\b\w+:\s*", "");
        return fullCode;
    }

    private string ProcessEqu(string input)
    {
        var equMatches = Regex.Matches(input, @"EQU\s+(\w+)\s+(\w+)");
        foreach (Match match in equMatches)
        {
            string name = match.Groups[1].Value;
            string value = match.Groups[2].Value;
            input = input.Replace(name, value);
        }
        // Remove all EQU statements
        input = Regex.Replace(input, @"EQU\s+\w+\s+\w+\s*[\r\n]*", "");
        return input;
    }

    private string RemoveComments(string input)
    {
        // Remove single-line comments
        input = Regex.Replace(input, @"//.*?$", "", RegexOptions.Multiline);
        // Remove multi-line comments
        input = Regex.Replace(input, @"/\*.*?\*/", "", RegexOptions.Singleline);
        return input;
    }

    private string ProcessImports(string input)
    {
        var importMatches = Regex.Matches(input, @"import\s+""(.+?)""");
        foreach (Match match in importMatches)
        {
            string fileName = match.Groups[1].Value;
            if (File.Exists(fileName))
            {
                string fileContent = File.ReadAllText(fileName);
                input = fileContent + Environment.NewLine + input;
            }
        }
        return input;
    }

    private string ConvertToOneLine(string input)
    {
        // Replace newlines and excessive spaces with a single space
        input = Regex.Replace(input, @"\s+", " ");
        return input.Trim();
    }
}