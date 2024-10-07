namespace Obfuscator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ObfuscatorPipeline obfuscatorPipeline = new ObfuscatorPipeline();
            Console.WriteLine(obfuscatorPipeline.Obfuscate(@" private string RandomString(int length)
    {
        const string chars = ""ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"";
        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }
        return new string(result);
    }"));
        }
    }
}
