using System;
using System.Text.RegularExpressions;

public class NameObfuscator
{
    private Random random = new Random();

        public string ObfuscateNames(string code)
    {
                code = Regex.Replace(code, @"class (\w+)", m => $"class {RandomString(7)}");

                code = Regex.Replace(code, @"public (\w+) (\w+)\(", m => $"public {m.Groups[1].Value} {RandomString(6)}(");

                code = Regex.Replace(code, @"\((\w+) (\w+),? (\w+)? (\w+)?\)", m =>
        {
            string[] obfuscatedParams = {
                RandomString(5),
                RandomString(5)
            };
            return $"({m.Groups[1].Value} {obfuscatedParams[0]}, {m.Groups[3]?.Value} {obfuscatedParams[1]})";
        });

                code = Regex.Replace(code, @"int (\w+)", m => $"int {RandomString(5)}");
        code = Regex.Replace(code, @"string (\w+)", m => $"string {RandomString(5)}");
        code = Regex.Replace(code, @"bool (\w+)", m => $"bool {RandomString(5)}");

        return code;
    }

        private string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }
        return new string(result);
    }
}
