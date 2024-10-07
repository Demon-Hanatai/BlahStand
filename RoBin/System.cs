using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RoBin
{
    public class System<T> where T : new()
    {
        private BoBin<T> boBin;
        public System(RoBin.BoBin<T> BoBin)
        {
            this.boBin = BoBin;
            BoBin.AddObject(boBin);
        }
        public string[] getalldata_name()
        {
            return boBin.LocalData.Select(x => x.name).ToArray();
        }
        public string[] getallfunc_name()
        {
            return boBin.LocalFuncs.Select(x => x.Name).ToArray();
        }
        public void del(string name)
        {
            var @object = boBin.LocalData.FirstOrDefault(x => x.name == name);
            if (@object is not null)
                boBin.LocalData.Remove(@object);
            else if (boBin.LocalFuncs.FirstOrDefault(x => x.Name == name) is LocalFunc locaf)
                boBin.LocalFuncs.Remove(locaf);
            else
                Console.WriteLine($"No object/function was deleted because no object named {name}.");
        }
        public void delfunc(string name)
        {
            if (boBin.LocalFuncs.FirstOrDefault(x => x.Name == name) is LocalFunc locaf)
                boBin.LocalFuncs.Remove(locaf);
            else
                Console.WriteLine($"No function was deleted because no function named {name}.");
        }
        /// <summary>
        /// Creates a new class with a name and body.
        /// </summary>
        /// <param name="name">Name of the class to be created</param>
        /// <param name="body">The body containing methods and fields</param>
        /// <returns></returns>
        public bool @class(string name, string body)
        {
            if (boBin.LocalObjectClasses.Any(x => x.Name == name))
            {
                throw new Exception($"Class '{name}' already exists.");
            }

            // Create a new dynamic class using Reflection.Emit or a predefined template for this purpose
            var dynamicClass = new DynamicClassBuilder().BuildDynamicClass(name);

            // Store the new class in LocalObjectClasses
            boBin.LocalObjectClasses.Add(new New { Name = name, Class = dynamicClass });
            return true;
        }//dont use under ctor

        /// <summary>
        /// Creates an instance of a dynamically created class by name.
        /// </summary>
        /// <param name="name">The name of the class to instantiate</param>
        /// <returns></returns>
        private object @new(string name) //dont use under ctor
        {
            var newClass = boBin.LocalObjectClasses.FirstOrDefault(x => x.Name == name);
            if (newClass != null)
            {
                // Check if newClass.Class is a Type
                var classType = newClass.Class as Type;

                if (classType == null)
                {
                    throw new Exception($"'{name}' is not a valid class type.");
                }

                // Instantiate the dynamic class using the default constructor
                try
                {
                    var typ  = Activator.CreateInstance(classType);
                    Type dynamicType = typ.GetType();

                    // Get the class name
                    Console.WriteLine($"Class Name: {dynamicType.Name}");

                    // Get constructors
                    var constructors = dynamicType.GetConstructors();
                    foreach (var constructor in constructors)
                    {
                        Console.WriteLine($"Constructor: {constructor}");
                    }

                    // Get methods
                    var methods = dynamicType.GetMethods();
                    foreach (var method in methods)
                    {
                        Console.WriteLine($"Method: {method.Name}");
                    }

                    // Get properties
                    var properties = dynamicType.GetProperties();
                    foreach (var property in properties)
                    {
                        Console.WriteLine($"Property: {property.Name}");
                    }

                    // Get fields
                    var fields = dynamicType.GetFields();
                    foreach (var field in fields)
                    {
                        Console.WriteLine($"Field: {field.Name}");
                    }

                    // Get implemented interfaces
                    var interfaces = dynamicType.GetInterfaces();
                    foreach (var iface in interfaces)
                    {
                        Console.WriteLine($"Implemented Interface: {iface.Name}");
                    }

                    return typ;
                }
                catch (MissingMethodException ex)
                {
                    throw new Exception($"No parameterless constructor defined for class '{name}'.", ex);
                }
            }
            else
            {
                throw new Exception($"Class '{name}' not found.");
            }
        }


        public void dataof(string type)
        {
            var matchingData = boBin.LocalData
                .Where(x => x.Object.GetType().Name.Equals(type, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingData.Any())
            {

                foreach (var data in matchingData)
                {
                    Console.WriteLine((data.name, data.Object));
                }
            }
            else if (type == "*")
            {
                foreach (var data in boBin.LocalData)
                {
                    Console.WriteLine((data.name, data.Object));
                }
            }
            else
            {
                Console.WriteLine($"No variables found of type '{type}'.");
            }
        }

        public void @while(string condition, string action)
        {
            var regex = new Regex(@"^(?<left>\S+)\s*(?<operator>==|!=|>|<|>=|<=)\s*(?<right>\S+)$");
            var match = regex.Match(condition);

            if (match.Success)
            {
                string left = match.Groups["left"].Value;
                string @operator = match.Groups["operator"].Value;
                string right = match.Groups["right"].Value;

                bool left_isStatic = false;
                bool right_isStatic = false;
                var leftResolved = (left_isStatic=left.StartsWith('$'))
                    ? ResolveFromLocalData(left.Substring(1)) 
                    : left;

                var rightResolved = (right_isStatic=right.StartsWith('$'))
                    ? ResolveFromLocalData(right.Substring(1))
                    : right;

                // Ensure both left and right operands are resolved
                if (leftResolved == null || rightResolved == null)
                {
                    throw new AccessViolationException($"Could not resolve left or right operand. Left: {leftResolved}, Right: {rightResolved}");
                }

                // Function to check if the condition is met
                Func<bool> conditionMet = () => @operator switch
                {
                    "==" => leftResolved.ToString() == rightResolved.ToString(),
                    "!=" => leftResolved.ToString() != rightResolved.ToString(),
                    ">" => Convert.ToDouble(leftResolved) > Convert.ToDouble(rightResolved),
                    "<" => Convert.ToDouble(leftResolved) < Convert.ToDouble(rightResolved),
                    ">=" => Convert.ToDouble(leftResolved) >= Convert.ToDouble(rightResolved),
                    "<=" => Convert.ToDouble(leftResolved) <= Convert.ToDouble(rightResolved),
                    _ => false
                };

                // Execute the action while the condition is met
                while (conditionMet())
                {
                    boBin.Execute(action);
                    if (left_isStatic)
                    {
                        leftResolved = ResolveFromLocalData(left.Substring(1));
                                            
                                            
                    }
                    if (right_isStatic)
                    {
                        rightResolved = ResolveFromLocalData(right.Substring(1));
                    }
                }
            }
        }

        public void _(string ars)
        {
     
            return;
        }

        public void @foreach(string logic, string body)
        {
            Regex foreachRegex = new Regex(@"\s*(\$\w+)\s+in\s+(\[.*\]|@?\w+.*|\$\w+)\s*");

            var match = foreachRegex.Match(logic);
            if (!match.Success)
            {
                throw new Exception("Invalid foreach syntax.");
            }

            // Get the item variable and collection expression
            string itemVar = match.Groups[1].Value.Trim();  // e.g., "$i"
            string collectionExp = match.Groups[2].Value.Trim();  // e.g., "[1,2,3,4]", "$users", or "@GetArray 1,2"

            // Handle the collection expression
            dynamic collection = ProcessCollection(collectionExp);
            
            // Ensure the collection is a valid IEnumerable
            if (collection is Array enumerableCollection)
            {
                // Iterate over the collection
                foreach (var element in enumerableCollection)
                {
                    // Replace the item variable in the body with the current element
                    string iterationBody = Regex.Replace(body, $@"(?<!\w){Regex.Escape(itemVar)}(?!\w)", element.ToString()!);

                    // Execute the modified body for this iteration
                    boBin.Execute(iterationBody);
                }
            }
            else
            {
                throw new Exception($"The expression {collectionExp} is not a valid collection.");
            }
        }

        private dynamic ProcessCollection(string collectionExp)
        {
            if (collectionExp.StartsWith("$"))
            {
                return boBin.GetValue(collectionExp);
            }
            else if (collectionExp.StartsWith("@"))
            {
                // Handle method calls without parentheses, like @GetArray 1,2
                string[] parts = collectionExp.Remove(0, 1).Split(' ');
                string methodName = parts[0];  // The method name (e.g., "GetArray")
                string parameters = string.Join(",", parts.Skip(1));  // The parameters (e.g., "1,2")

                // Call the method using the parsed name and parameters
                return boBin.CallMethod(methodName, parameters);
            }
            else if (IsArray(collectionExp))
            {
                // Handle direct array literals like [1,2,3,4]
                return TypeConverter.ConvertType(typeof(object[]), collectionExp);
            }

            throw new Exception("Unsupported collection expression.");
        }

        // Helper function to detect if the string is an array
        private bool IsArray(string value)
        {
            return value.StartsWith("[") && value.EndsWith("]");
        }

        public void @if(string condition, string action)
        {
            // Regular expression to capture the two sides of the comparison and the operator.
            var regex = new Regex(@"^(?<left>\S+)\s*(?<operator>==|!=|>|<|>=|<=)\s*(?<right>\S+)$");
            var match = regex.Match(condition);

            if (match.Success)
            {
                string left = match.Groups["left"].Value;
                string @operator = match.Groups["operator"].Value;
                string right = match.Groups["right"].Value;

                // Resolve left operand if it's a reference (starts with $)
                var leftResolved = left.StartsWith('$')
                    ? ResolveFromLocalData(left.Substring(1))
                    : TypeConverter.ConvertType(null, RemoveQuotes(left));

                // Resolve right operand if it's a reference (starts with $)
                var rightResolved = right.StartsWith('$')
                    ? ResolveFromLocalData(right.Substring(1))
                    : TypeConverter.ConvertType(null, RemoveQuotes(right));

                // If either left or right couldn't be resolved, skip execution
                if (leftResolved == null || rightResolved == null)
                {
                    throw new AccessViolationException($"Could not resolve left or right operand. Left: {leftResolved}, Right: {rightResolved}");
                }

                // Perform the comparison based on the operator
                Func<bool> conditionMet = () => @operator switch
                {
                    "==" => leftResolved.ToString() == rightResolved.ToString(),
                    "!=" => leftResolved.ToString() != rightResolved.ToString(),
                    ">" => Convert.ToDouble(leftResolved) > Convert.ToDouble(rightResolved),
                    "<" => Convert.ToDouble(leftResolved) < Convert.ToDouble(rightResolved),
                    ">=" => Convert.ToDouble(leftResolved) >= Convert.ToDouble(rightResolved),
                    "<=" => Convert.ToDouble(leftResolved) <= Convert.ToDouble(rightResolved),
                    _ => false
                };

                // If the condition is met, execute the action
                if (conditionMet())
                {
                    boBin.Execute(action);
                }
            }
        }
        public string RemoveQuotes(string input)
        {
            if (input.StartsWith("\"") && input.EndsWith("\""))
            {
                // Remove the first and last character (quotes)
                return input.Substring(1, input.Length - 2);
            }
            return input; // Return the original input if no surrounding quotes are found
        }

        // Helper function to resolve values from LocalData if the name starts with $
        private object ResolveFromLocalData(string variableName)
        {
            var ob = boBin.LocalData.FirstOrDefault(x => x.name == variableName);
            return ob?.Object;
        }


        public static string GenerateTempId()
        {
            // Get current time down to milliseconds
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 5;  // Adjust the resolution

            // Generate a random number for uniqueness
            Random random = new Random();
            int randomValue = random.Next(0, 9999); // You can adjust this range if needed

            // Combine timestamp and random value
            long combinedValue = (timestamp << 13) | randomValue;  // Left shift and combine

            // Convert to Base36 for a short, alphanumeric representation
            string base36Value = Base36Encode(combinedValue);

            // Ensure it's exactly 5 characters long
            return base36Value.Length > 5 ? base36Value.Substring(0, 5) : base36Value.PadLeft(5, '0');
        }

        // Base36 encoding (0-9, A-Z)
        public static string Base36Encode(long value)
        {
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string result = string.Empty;

            while (value > 0)
            {
                result = chars[(int)(value % 36)] + result;
                value /= 36;
            }

            return result;
        }
        public static List<string> ParseAndValidateParameters(string input)
        {
            var parameters = new List<string>();

            // Split by commas
            string[] parts = input.Split(',');

            if (parts.Length == 1 && parts[0] == null)
            {
                return [];
            }
            if (input.Length == 0)
            {
                return [];
            }
            foreach (var part in parts)
            {
                // Trim leading and trailing spaces for each part
                string trimmedPart = part.Trim();

                // Ensure that each part starts with "$" and consists of word characters
                if (string.IsNullOrEmpty(trimmedPart) || !trimmedPart.StartsWith("$") || !IsAlphanumeric(trimmedPart.Substring(1)))
                {
                    throw new ArgumentException($"Invalid Parameters token {part}.");
                }

                // Add valid parameter to the list
                parameters.Add(trimmedPart);
            }

            return parameters; // All parameters are valid
        }

        public static bool IsAlphanumeric(string str)
        {
            foreach (char c in str)
            {
                if (c is '-' or '_' || char.IsLetterOrDigit(c))
                    continue;
                else if (!char.IsLetterOrDigit(c))
                {
                    return false;
                }
            }
            return true;
        }
        public string Join(string chot,string[] strings) => string.Join(chot,strings);
        public void Exit() => Exit(0);
        public void Exit(int code=0)
            =>Environment.Exit(code);

        public int add(int parm1, int parm2)
            => parm1 + parm2;
        public void @typeof(object ob)
        {

            if (ob is not null or "")
            {
                printf((type: TypeConverter.ConvertType(null,ob)!.GetType(), name: ob));
                return;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public void printf(object value)
        {
            Console.WriteLine(value);
        }
        public void cd(string value)
        {
            Environment.CurrentDirectory = value;
        }
        public void clear()
        {
            Console.Clear();
        }
        //Importing C# types
        public void @import(string type_name_or_namespace)
        {
            if (string.IsNullOrEmpty(type_name_or_namespace))
                throw new ArgumentNullException(nameof(type_name_or_namespace));

            Type? type = Type.GetType(type_name_or_namespace, false);

            if (type != null)
            {
                AddTypeObject(type);
                return;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var typesFound = new List<Type>();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes()
                    .Where(t => t.Name == type_name_or_namespace || t.FullName == type_name_or_namespace ||
                                t.Namespace == type_name_or_namespace)
                    .ToList();

                if (types.Any())
                {
                    typesFound.AddRange(types);
                }
            }

            if (typesFound.Any())
            {
                foreach (var t in typesFound)
                {
                    AddTypeObject(t);
                }
                return;
            }

            throw new TypeLoadException($"Type or namespace '{type_name_or_namespace}' not found.");
        }

        private void AddTypeObject(Type type)
        {
            if (!type.IsAbstract || !type.IsSealed) // If the type is not static
            {
                var constructors = type.GetConstructors();

                if (constructors.Length > 0)
                {
                    // Try parameterless constructor
                    var constructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
                    if (constructor != null)
                    {
                        try
                        {
                            var instance = constructor.Invoke(null); // Invoke the parameterless constructor
                            boBin.AddObject(instance!);  // Add the instance to boBin
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to instantiate {type.FullName}: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"No parameterless constructor available for {type.FullName}. Adding type itself.");
                        boBin.AddObject(type); // Add the type itself for static member access
                    }
                }
                else
                {
                    Console.WriteLine($"No available constructors found for {type.FullName}");
                    boBin.AddObject(type); // Add the type itself for static member access
                }
            }
            else // Handle static classes and members
            {
                boBin.AddObject(type); // Add the type itself for static member access
            }
        }


    }
}
