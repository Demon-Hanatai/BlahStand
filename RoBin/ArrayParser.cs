namespace RoBin
{
    public static class ArrayParser
    {
        // Parses an input string into a list of objects, handling nested arrays
        public static List<object> Parse(string input)
        {
            input = input.Trim('[', ']');  // Remove outer brackets
            var elements = new List<object>();
            int bracketDepth = 0;
            int start = 0;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '[')
                {
                    bracketDepth++;
                }
                else if (input[i] == ']')
                {
                    bracketDepth--;
                }
                else if (input[i] == ',' && bracketDepth == 0)
                {
                    string element = input.Substring(start, i - start).Trim();
                    elements.Add(ParseElement(element));
                    start = i + 1;
                }
            }

            // Add the last element
            if (start < input.Length)
            {
                string element = input.Substring(start).Trim();
                elements.Add(ParseElement(element));
            }

            return elements;
        }

        // Parses an individual element, which may be a nested array or a value
        private static object ParseElement(string element)
        {
            if (IsArray(element))
            {
                // Handle nested arrays recursively
                return Parse(element);
            }

            // Handle individual values
            if (int.TryParse(element, out int intValue))
            {
                return intValue;
            }

            if (double.TryParse(element, out double doubleValue))
            {
                return doubleValue;
            }

            if (bool.TryParse(element, out bool boolValue))
            {
                return boolValue;
            }

            // Return as a string if nothing else matches
            return element;
        }

        // Helper method to check if a string is an array
        public static bool IsArray(string value)
        {
            return value.StartsWith("[") && value.EndsWith("]");
        }
    }
}