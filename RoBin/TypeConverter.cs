using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RoBin
{
    public static class TypeConverter
    {
        public static object ConvertType(Type type, object value)
        {
            if (value == null)
                return null;

            string input = value.ToString();
            if(EvaluateSimpleCondition(input) is (bool result, bool handled)  && handled is true)
            {
                return result;
            }
            // Handle array inputs
            if (IsArray(input))
            {
                try
                {
                    var array = ArrayParser.Parse(input);
                    if (type != null && type.IsArray)
                    {
                        Type elementType = type.GetElementType();
                        return ConvertArray(array, elementType);
                    }

                    // Auto-detect the array type if no type is provided
                    return InferArrayType(array);
                }
                catch (FormatException ex)
                {
                    throw new FormatException($"Invalid array format: {ex.Message}");
                }
            }

            // Auto-pick the best type if `type` is null
            if (type == null)
            {
                return InferType(value.ToString());
            }

            // Handle other types normally
            return ConvertByType(type, value);
        }

        // Helper method to check if the input string is an array
        private static bool IsArray(string value)
        {
            return value.StartsWith("[") && value.EndsWith("]");
        }

        // Convert array elements to the specified type
        private static Array ConvertArray(List<object> elements, Type elementType)
        {
            Array convertedArray = Array.CreateInstance(elementType, elements.Count);

            for (int i = 0; i < elements.Count; i++)
            {
                convertedArray.SetValue(ConvertType(elementType, elements[i]), i);
            }

            return convertedArray;
        }

        // Infer array type if no explicit type is provided
        private static object InferArrayType(List<object> elements)
        {
            if (elements.All(e => e is int))
            {
                return elements.Cast<int>().ToArray();
            }

            if (elements.All(e => e is double))
            {
                return elements.Cast<double>().ToArray();
            }

            if (elements.All(e => e is bool))
            {
                return elements.Cast<bool>().ToArray();
            }

            if (elements.All(e => e is DateTime))
            {
                return elements.Cast<DateTime>().ToArray();
            }

            // If mixed types, return as object array
            return elements.ToArray();
        }

        // Convert specific value types
        private static object ConvertByType(Type type, object value)
        {
            // Remove underscores used for number separation
            var stringValue = value.ToString().Replace("_", "");

            stringValue = stringValue
         .Replace("\\\\", "\uFFFF")   // Temporarily replace double backslash with a unique placeholder
         .Replace("\\n", "\n")         // Replace \n with actual newline
         .Replace("\uFFFF", "\\").Replace("\\r","\r").Replace("\\t","\t");

            if (type == typeof(string))
            {
                return stringValue;
            }
            else if (type == typeof(int))
            {
                return int.TryParse(stringValue, out var result) ? result : 0;
            }
            else if (type == typeof(uint))
            {
                return uint.TryParse(stringValue, out var result) ? result : 0u;
            }
            else if (type == typeof(long))
            {
                return long.TryParse(stringValue, out var result) ? result : 0L;
            }
            else if (type == typeof(ulong))
            {
                return ulong.TryParse(stringValue, out var result) ? result : 0UL;
            }
            else if (type == typeof(short))
            {
                return short.TryParse(stringValue, out var result) ? result : (short)0;
            }
            else if (type == typeof(ushort))
            {
                return ushort.TryParse(stringValue, out var result) ? result : (ushort)0;
            }
            else if (type == typeof(byte))
            {
                return byte.TryParse(stringValue, out var result) ? result : (byte)0;
            }
            else if (type == typeof(sbyte))
            {
                return sbyte.TryParse(stringValue, out var result) ? result : (sbyte)0;
            }
            else if (type == typeof(double))
            {
                return double.TryParse(stringValue, out var result) ? result : 0.0;
            }
            else if (type == typeof(float))
            {
                // Handle 'f' suffix for float values
                if (stringValue.EndsWith("f", StringComparison.OrdinalIgnoreCase))
                {
                    return float.TryParse(stringValue.TrimEnd('f', 'F'), out var result) ? result : 0.0f;
                }
                return float.TryParse(stringValue, out var resultWithoutSuffix) ? resultWithoutSuffix : 0.0f;
            }
            else if (type == typeof(decimal))
            {
                // Handle 'm' suffix for decimal values
                if (stringValue.EndsWith("m", StringComparison.OrdinalIgnoreCase))
                {
                    return decimal.TryParse(stringValue.TrimEnd('m', 'M'), out var result) ? result : 0.0m;
                }
                return decimal.TryParse(stringValue, out var resultWithoutSuffix) ? resultWithoutSuffix : 0.0m;
            }
            else if (type == typeof(bool))
            {
                return bool.TryParse(stringValue, out var result) ? result : false;
            }
            else if (type == typeof(char))
            {
                return char.TryParse(stringValue, out var result) ? result : '\0';
            }
            else if (type == typeof(DateTime))
            {
                return DateTime.TryParse(stringValue, out var result) ? result : DateTime.MinValue;
            }
            else if (type == typeof(Guid))
            {
                return Guid.TryParse(stringValue, out var result) ? result : Guid.Empty;
            }
            else if (type == typeof(TimeSpan))
            {
                return TimeSpan.TryParse(stringValue, out var result) ? result : TimeSpan.Zero;
            }
            else if (type == typeof(object))
            {
                return value;
            }
            else
            {
                throw new InvalidOperationException($"Conversion for type {type.Name} is not supported.");
            }
        }


        private static object InferType(string value)
        {
            // Remove any underscores used for number separation
            value = value.Replace("_", "");

            // Handle numeric suffixes
            if (value.EndsWith("f", StringComparison.OrdinalIgnoreCase) &&
                float.TryParse(value.TrimEnd('f', 'F'), out float floatValue))
            {
                return floatValue;
            }
            else if (value.EndsWith("m", StringComparison.OrdinalIgnoreCase) &&
                     decimal.TryParse(value.TrimEnd('m', 'M'), out decimal decimalValue))
            {
                return decimalValue;
            }
            else if (value.EndsWith("d", StringComparison.OrdinalIgnoreCase) &&
                     double.TryParse(value.TrimEnd('d', 'D'), out double doubleValue))
            {
                return doubleValue;
            }

            // Try parsing as int (with underscores removed)
            if (int.TryParse(value, out int intValue))
            {
                return intValue;
            }
            // Try parsing as long
            else if (long.TryParse(value, out long longValue))
            {
                return longValue;
            }
            // Try parsing as float
            else if (float.TryParse(value, out float parsedFloatValue))
            {
                return parsedFloatValue;
            }
            // Try parsing as double
            else if (double.TryParse(value, out double parsedDoubleValue))
            {
                return parsedDoubleValue;
            }
            // Try parsing as decimal
            else if (decimal.TryParse(value, out decimal parsedDecimalValue))
            {
                return parsedDecimalValue;
            }
            // Try parsing as bool
            else if (bool.TryParse(value, out bool boolValue))
            {
                return boolValue;
            }
            // Try parsing as DateTime
            else if (DateTime.TryParse(value, out DateTime dateTimeValue))
            {
                return dateTimeValue;
            }
            // Try parsing as Guid
            else if (Guid.TryParse(value, out Guid guidValue))
            {
                return guidValue;
            }
            // Try parsing as TimeSpan
            else if (TimeSpan.TryParse(value, out TimeSpan timeSpanValue))
            {
                return timeSpanValue;
            }
            // Handle single character input
            else if (value.Length == 1)
            {
                return value[0];
            }

            // Default to returning the string value if no other types matched
            return value;
        }



        public static bool IsElgiEvaluate(string expression)
            => expression.Any(x=>x is '-' or '/'or '*'or '%'or '^'or '<'or '>'or '&'or '|');
        private static object EvaluateExpression(string expression)
        {

            var expr = new System.Data.DataTable();
            try
            {
                var result = expr.Compute(expression, null);
                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error evaluating expression '{expression}': {ex.Message}");
            }
        }
        public static (bool result, bool handled) EvaluateSimpleCondition(string condition)
        {
            // Regular expression to match conditions like "100 == 100", "True == True", "False != True"
            var regex = new Regex(@"^\s*(?<left>.*?)\s*(?<operator>==|!=|>=|<=|>|<)\s*(?<right>.*?)\s*$");
            var match = regex.Match(condition);

            // If it's not a valid comparison, return false with handled as false
            if (!match.Success)
            {
                return (false, false);
            }

            // Extract the parts of the condition
            string left = match.Groups["left"].Value;
            if(IsElgiEvaluate(left)) 
                left= EvaluateExpression(left).ToString()!;
            string @operator = match.Groups["operator"].Value;
            string right = match.Groups["right"].Value;
            if (IsElgiEvaluate(right))
                right = EvaluateExpression(right).ToString()!;
            // Try to parse the left and right operands into their respective types
            object leftValue = ParseValue(ConvertType(null, left).ToString()!);
            object rightValue = ParseValue(ConvertType(null, right).ToString()!);

            // Ensure both sides could be parsed as the same type
            if (leftValue.GetType() != rightValue.GetType())
            {
                return (false, false); // Not handled if types are incompatible
            }

            // Handle the comparison based on the operator and type
            bool result = @operator switch
            {
                "==" => leftValue.Equals(rightValue),
                "!=" => !leftValue.Equals(rightValue),
                ">" => CompareValues(leftValue, rightValue) > 0,
                "<" => CompareValues(leftValue, rightValue) < 0,
                ">=" => CompareValues(leftValue, rightValue) >= 0,
                "<=" => CompareValues(leftValue, rightValue) <= 0,
                _ => throw new InvalidOperationException("Invalid operator")
            };

            // Return the result along with a flag indicating the condition was handled
            return (result, true);
        }

        // Helper method to parse the string into a value (int, bool, or string)
        private static object ParseValue(string value)
        {
            if (int.TryParse(value, out int intValue))
            {
                return intValue;
            }

            if (bool.TryParse(value, out bool boolValue))
            {
                return boolValue;
            }

            // Fallback to string if neither int nor bool could be parsed
            return value;
        }

        // Helper method to compare values for <, >, <=, >=
        private static int CompareValues(object left, object right)
        {
            // If the values are integers
            if (left is int leftInt && right is int rightInt)
            {
                return leftInt.CompareTo(rightInt);
            }

            // If the values are booleans (true > false)
            if (left is bool leftBool && right is bool rightBool)
            {
                return leftBool.CompareTo(rightBool);
            }

            throw new InvalidOperationException("Comparison not supported for these types");
        }
       


    }


}