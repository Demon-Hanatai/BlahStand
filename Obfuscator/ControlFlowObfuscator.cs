using System;
using System.Text.RegularExpressions;

public class ControlFlowObfuscator
{
    private Random random = new Random();

    // Obfuscate control flow to make the code more complex
    public string ObfuscateControlFlow(string code)
    {
        // Add false conditions around return statements
        code = Regex.Replace(code, @"return (\w+);", m =>
        {
            string varName = m.Groups[1].Value;
            return $@"
            if ({varName} == {varName} && {RandomCondition()})
            {{
                {varName} += 0; // Unnecessary operation
            }}
            return {varName};";
        });

        // Add useless loops that don’t affect the logic
        code = Regex.Replace(code, @"(\s+){", m =>
        {
            return $@"{m.Groups[1].Value}
            while ({RandomCondition()})
            {{
                // Confusing loop
                break;
            }}
            {m.Groups[1].Value}{{";
        });

        return code;
    }

    // Helper method to generate a random condition
    private string RandomCondition()
    {
        string[] conditions = { "1 == 1", "0 != 1", "true == true", "false != true" };
        return conditions[random.Next(conditions.Length)];
    }
}
