using System;

public class RandomCodeInjector
{
    private Random random = new Random();

        public string InjectRandomCode(string code)
    {
                code = code.Replace("{", "{\n" + GenerateUselessRealisticCode());
        return code;
    }

        private string GenerateUselessRealisticCode()
    {
        string[] dummyCodeSnippets = {
            "int temp = 0;             "for (int i = 0; i < 2; i++) { temp += i; }             "bool flag = false;             "if (flag) { temp++; }             "string tempString = \"example\";         };

        int count = random.Next(1, 3);         string result = "";
        for (int i = 0; i < count; i++)
        {
            result += dummyCodeSnippets[random.Next(dummyCodeSnippets.Length)] + "\n";
        }
        return result;
    }
}
