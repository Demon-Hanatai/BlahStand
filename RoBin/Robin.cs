
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace RoBin
{
    public class BoBin<TBased> where TBased : new()
    {

        protected TBased _TBasedObject = new();
        public List<DataInfo> LocalData = new();
        public List<LocalFunc> LocalFuncs = new();
        public List<New> LocalObjectClasses = new();
        private List<object> TObjects { get; set; } = new List<object>();
        private string SetValue_Regex = @"(\$.*?)\s*(=|\+=|-=|\*=|\/=|\\=)\s*(.*)";
        public BoBin()
        {
            TObjects.Add(new System<TBased>(this));
        }
        private string GetValue_Regex = @"\$(.*)";
        public void AddObject(object ob)
        {
            TObjects.Add(ob);
        }
        public void AddObject<TObject>() where TObject : new()
        {
            TObjects.Add(new TObject());
        }

        private List<string> SplitCommandsRespectingQuotesAndBraces(string input)
        {
            List<string> result = new List<string>();
            StringBuilder currentCommand = new StringBuilder();
            int braceCount = 0;
            bool inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    currentCommand.Append(c);
                }
                else if (c == '{' && !inQuotes)
                {
                    braceCount++;
                    currentCommand.Append(c);
                }
                else if (c == '}' && !inQuotes && braceCount > 0)
                {
                    braceCount--;
                    currentCommand.Append(c);
                }
                else if (c == ';' && braceCount == 0 && !inQuotes)
                {
                    result.Add(currentCommand.ToString().Trim());
                    currentCommand.Clear();
                }
                else
                {
                    currentCommand.Append(c);
                }
            }


            if (currentCommand.Length > 0)
            {
                result.Add(currentCommand.ToString().Trim());
            }

            return result;
        }

        public void Execute(string cmds)
        {
            if (cmds == "" || cmds == null)
                return;

            var commands = SplitCommandsRespectingQuotesAndBraces(cmds);

            string last_cmd = "";
            try
            {
                foreach (var cmd in commands)
                {

                    last_cmd = cmd;

                    if (cmd == "")
                        continue;
                    if (Regex.Match(cmd, @"@(.*)\s*\((.*)\)").Success)
                    {
                        callLocalFunc(cmd);

                    }
                    else if (cmd.StartsWith("@"))
                    {
                        CallMethod(
                                    cmd.Remove(0, 1).Split(' ')[0],
                                    string.Join(" ", cmd.Split(' ').Skip(1))
                        );

                    }
                    else if (Regex.Match(cmd, SetValue_Regex).Success)
                        SetValue(cmd);
                    else if (Regex.Match(cmd, GetValue_Regex).Success)
                        GetValue(cmd);
                    else if (TypeConverter.EvaluateSimpleCondition(last_cmd) is (bool result, bool Ishandled) && Ishandled)
                    {
                        Console.WriteLine(result);
                        continue;
                    }
                   
                    else
                    {
                        throw new InvalidOperationException(cmd);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("At : " + last_cmd);
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine(e.ToString());
                Console.ResetColor();
                Console.WriteLine();
            }
        }
        private bool IsValidName(string name)
        {

            if (!name.StartsWith("$")) return false;


            name = name.Remove(0, 1);


            var isValid = !string.IsNullOrWhiteSpace(name) &&
                          !name.Contains(' ') &&
                          name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.' || c=='[' || c==']');

            return isValid;
        }
        public dynamic GetObjectArrayIndex(dynamic obj,dynamic index)
        {
          return obj[TypeConverter.ConvertType(null, index)];
        }

        private (bool any, object item) get_v(string name)
        {
            if (string.IsNullOrEmpty(name) || !IsValidName(name))
            {

                throw new ArgumentException($"Invalid name format: {name}. Name must start with '$' and contain valid characters.");
            }

            name = name.Remove(0, 1);

            if (name.Split('.').Length > 1)
            {
                object basedObject = _TBasedObject!;
                var GetObject = DeepSearch(name, _TBasedObject!);
                if (!GetObject.found)
                {
                    foreach (var item in TObjects)
                    {
                        GetObject = DeepSearch(name, _TBasedObject!);
                        if (GetObject.found)
                        {
                            basedObject = item;
                            break;
                        }
                    }
                }
                if (GetObject.found)
                {
                    return (true, GetObject.Prop.GetValue(GetObject.Target)!);
                }
                else
                {
                    throw new Exception($"No object named {string.Join("", name.Split('.').Skip(1))}.");
                }
            }
            else if (Regex.Matches(name, @"((?:\[(.*?)\]))") is MatchCollection MC &&
                MC.Count>0 )
            {
                var mewMame = string.Join('\0',name.Take(name.IndexOf('[')));
                var o = LocalData.FirstOrDefault(x => x.name == mewMame);
                dynamic Result = o!.Object;
                for (int i = 0; i < MC.Count; i++)
                {
                    Result = GetObjectArrayIndex(Result, MC[i].Groups[2].Value); 
                }
                return (true, Result);
            }
            else if (LocalData.Any(x => x.name == name))
            {
                var o = LocalData.FirstOrDefault(x => x.name == name);
              
                    return (true, o.Object);
            }

            return (false, name);
        }


        public object GetValue(string name)
        {

            var v = get_v(name);
            if (v.any)
            {
                return v.item;
            }
            else
            {
                if (IsValidName(name))
                {
                    LocalData.Add(new(name, v.item));
                }
                return name;
            }

        }

        public object GetValueWithoutCreatingNew(string name)
        {

            var v = get_v(name);
            if (v.any)
            {
                return v.item;
            }
            return name;


        }
        public object GetValueOnGet(string expression)
        {
            if (string.IsNullOrEmpty(expression)) return null;


            if (expression.StartsWith("@"))
            {
                return CallMethodFromExpression(expression);
            }
            if (!IsElgiEvaluate(expression))
                if (get_v(expression) is (bool any, object item) && any is true)
                    return item;

            return EvaluateExpression(expression);
        }
        public bool IsElgiEvaluate(string expression)
            => TypeConverter.IsElgiEvaluate(expression);
        private object EvaluateExpression(string expression)
        {
            if (!IsElgiEvaluate(expression))
                return expression;
            expression = ReplaceMethodCallsWithResults(expression);


            var expr = new System.Data.DataTable();
            var sanitizedExpression = ReplaceVariablesWithValues(expression);
            try
            {
                var result = expr.Compute(sanitizedExpression, null);
                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error evaluating expression '{expression}': {ex.Message}");
            }
        }
        private string ReplaceMethodCallsWithResults(string expression)
        {

            var methodRegex = new Regex(@"@(?<methodName>\w+)\s*(?<params>[^\""]*\"""" ?| [^{]*\{?[^}]*\})");


            return methodRegex.Replace(expression, match =>
            {
                string methodName = match.Groups["methodName"].Value;
                string parameters = match.Groups["params"].Value;


                var result = CallMethod(methodName, parameters);


                return result.ToString();
            });
        }
        private string ReplaceVariablesWithValues(string expression)
        {

            var regex = new Regex(@"\$(\w+)(?![^{]*\}|[^\""]*\"""")");


            return regex.Replace(expression, match =>
            {
                var variableName = match.Groups[1].Value;


                var variableValue = get_v($"${variableName}").item;

                if (variableValue == null)
                {
                    throw new Exception($"Variable ${variableName} not found.");
                }

                return variableValue.ToString();
            });
        }
        private object CallMethodFromExpression(string expression)
        {

            var methodRegex = new Regex(@"^@(?<methodName>\w+)\s*(?<params>.*)$");
            var match = methodRegex.Match(expression);

            if (match.Success)
            {
                string methodName = match.Groups["methodName"].Value;
                string parameters = match.Groups["params"].Value;
                return CallMethod(methodName, parameters);
            }
            else
            {
                throw new InvalidOperationException($"Invalid method call format: {expression}");
            }
        }
        private bool IsLiteral(string value)
        {
            // Check if the value starts and ends with quotes (string literal)
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                return true; // It's a string literal
            }

            // Check if the value is wrapped in curly braces (block literal)
            if (value.StartsWith("{") && value.EndsWith("}"))
            {
                return true; // It's a block literal
            }

            // Check if the value is an array (enclosed in square brackets)
            if (IsArray(value))
            {
                return true; // It's an array, treat it as a literal
            }

            // Check if the value is a number (integer or decimal)
            if (IsNumber(value))
            {
                return true; // It's a number literal
            }

            // Check if the value is a boolean
            if (IsBoolean(value))
            {
                return true; // It's a boolean literal
            }

            // If none of the above, it's not a literal and should be evaluated as an expression
            return false;
        }

        // Check if the string is an array (in square brackets) and optionally followed by 'as' type
        private bool IsArray(string value)
        {
            return ArrayParser.IsArray(value);
        }

        // Helper function to check if the value is a valid number (integer or decimal)
        private bool IsNumber(string value)
        {
            return int.TryParse(value, out _) || double.TryParse(value, out _);
        }

        // Helper function to check if the value is a valid boolean
        private bool IsBoolean(string value)
        {
            return bool.TryParse(value, out _);
        }

        private void SetValue(string cmd)
        {
            var Regex_Cmd = Regex.Match(cmd, SetValue_Regex);
            string name = Regex_Cmd.Groups[1].Value;
            string opr = Regex_Cmd.Groups[2].Value;
            string value = Regex_Cmd.Groups[3].Value;
            PropertyInfo PropObject = null!;
            object Prop_Owner = null!;
            bool IsStatic = false;


            if (name.StartsWith("$"))
            {
                if (string.IsNullOrEmpty(name) || !IsValidName(name))
                {
                    throw new ArgumentException($"Invalid name format: {name}. Name must start with '$' and contain valid characters.");
                }
                else
                {
                    name = name.Remove(0, 1);
                    if (!LocalData.Any(x => x.name == name))
                        LocalData.Add(new(name, null!));

                    var obj = LocalData.FirstOrDefault(x => x.name == name)!;
                    Prop_Owner = obj;
                    PropObject = obj.GetType().GetProperty("Object")!;
                }
            }
            else
            {

                if (name.Split('.').Length > 1)
                {
                    var Object = DeepSearch(name, _TBasedObject!);
                    if (Object.found)
                    {
                        PropObject = Object.Prop;
                        IsStatic = Object.isStatic;
                        Prop_Owner = Object.Target;
                    }
                    else
                    {
                        foreach (var item in TObjects)
                        {
                            Object = DeepSearch(name, item!);
                            if (Object.found)
                            {
                                PropObject = Object.Prop;
                                IsStatic = Object.isStatic;
                                Prop_Owner = Object.Target;
                                break;
                            }
                        }
                    }
                }
                else
                {

                    PropObject = _TBasedObject!.GetType().GetProperty(name)!;
                    if (PropObject != null)
                    {
                        IsStatic = PropObject.GetMethod!.IsStatic;
                    }
                    Prop_Owner = _TBasedObject;
                }

                if (PropObject == null && LocalData.Any(x => x.name == name))
                {
                    var item = LocalData.FirstOrDefault(x => x.name == name);
                    if (item != default)
                    {
                        PropObject = item.GetType().GetProperty("Object")!;
                        Prop_Owner = item;
                    }
                }

                if (PropObject == null)
                {
                    LocalData.Add(new(name, TypeConverter.ConvertType(null, value)));
                    return;
                }
            }

            object existingValue = IsStatic ? PropObject.GetValue(null) : PropObject.GetValue(Prop_Owner);
            object convertedValue;


            if (value.StartsWith("@"))
            {
                convertedValue = GetValueOnGet(value);
            }

            else if (IsElgiEvaluate(value))
            {
                try
                {
                    convertedValue = EvaluateComplexExpression(value);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Error evaluating expression '{value}': {ex.Message}");
                }
            }
            else
            {
                convertedValue = TypeConverter.ConvertType(PropObject.PropertyType, value);
            }


            switch (opr)
            {
                case "+=":
                    PropObject.SetValue(IsStatic ? null : Prop_Owner, ((dynamic)(TypeConverter.ConvertType(null, existingValue!)) + (dynamic)TypeConverter.ConvertType(null, convertedValue!)));
                    break;
                case "-=":
                    PropObject.SetValue(IsStatic ? null : Prop_Owner, ((dynamic)TypeConverter.ConvertType(null, existingValue!)) - (dynamic)TypeConverter.ConvertType(null, convertedValue!));
                    break;
                case "*=":
                    PropObject.SetValue(IsStatic ? null : Prop_Owner, ((dynamic)TypeConverter.ConvertType(null, existingValue!)) * (dynamic)TypeConverter.ConvertType(null, convertedValue!));
                    break;
                case "/=":
                    PropObject.SetValue(IsStatic ? null : Prop_Owner, ((dynamic)TypeConverter.ConvertType(null, existingValue!)) / (dynamic)TypeConverter.ConvertType(null, convertedValue!));
                    break;
                case "%=":
                    PropObject.SetValue(IsStatic ? null : Prop_Owner, ((dynamic)TypeConverter.ConvertType(null, existingValue!)) % (dynamic)TypeConverter.ConvertType(null, convertedValue!));
                    break;
                default:
                    PropObject.SetValue(IsStatic ? null : Prop_Owner, convertedValue);
                    break;
            }
        }
        bool IsComplexExpression(string input)
        {
            // Check if the input contains arithmetic operators or parentheses, indicating a complex expression
            return input.Contains("+") || input.Contains("-") || input.Contains("*") || input.Contains("/") || input.Contains("(") || input.Contains(")");
        }
        private object EvaluateComplexExpression(string expression)
        {
            if (!IsComplexExpression(expression))
                return expression;
            string sanitizedExpression = ReplaceVariablesWithValues(expression);


            var table = new System.Data.DataTable();
            object result;

            try
            {
                result = table.Compute(sanitizedExpression, null);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error evaluating expression '{expression}': {ex.Message}");
            }

            return result;
        }

        public object CallMethod(string MethodName, string Parms)
        {
            MethodName = MethodName.TrimEnd();
            bool isStatic = false;

            var prs = SplitParamsRespectingBracesAndQuotes(Parms);


            MethodInfo method = null;
            if (MethodName.Contains('.'))
            {
                if (DeepSearch(MethodName, _TBasedObject) is (PropertyInfo Prop, object Target, bool _isStatic, bool found)
                    && found)
                {
                    method = Prop.PropertyType.GetMethod(MethodName.Split('.').Last());
                    isStatic = method.IsStatic;
                }
            }
            else
            {
                var methods = _TBasedObject!.GetType().GetMethods().Where(x => x.Name == MethodName).ToList();

                if (methods.Count > 1)
                {
                    method = methods.FirstOrDefault(x => x.GetParameters().Length == prs.Count);
                }
                else
                {
                    method = methods.FirstOrDefault();
                }
                if (method != null)
                    isStatic = method.IsStatic;
            }
            if (string.IsNullOrEmpty(Parms))
            {
                prs = [];
            }

            object method_owner = _TBasedObject;
            List<object> args = new List<object>();

            if (method == null)
            {
                foreach (var item in TObjects)
                {
                    var _Obj = (item is Type ? (item as Type)! : item.GetType()!);
                    MethodInfo met = null!;
                    if (MethodName.Contains('.'))
                    {
                        if (DeepSearch(MethodName, _Obj) is (PropertyInfo Prop, object Target, bool _isStatic, bool found) && found)
                        {
                            if (Prop is object)
                                continue;
                            MethodName = MethodName.Split('.').Last();
                            met = Prop.PropertyType.GetMethod(MethodName)!;
                            isStatic = met.IsStatic;

                        }
                    }
                    else
                    {

                        var methods = _Obj!.GetMethods().Where(x => x.Name == MethodName).ToList();

                        if (methods.Count > 1)
                        {
                            met = methods.FirstOrDefault(x => x.GetParameters().Length == prs.Count);
                        }
                        else
                        {
                            met = methods.FirstOrDefault();
                        }

                        if (met != null)
                            isStatic = met.IsStatic;

                    }

                    if (met != null)
                    {
                        method = met;
                        method_owner = item;
                        break;
                    }
                }
            }

            if (method == null)
            {

                throw new MethodAccessException($"Unable to find Method {MethodName} that has the same parm count or does not exist at all.");
            }
            else
            {
                var method_param = method.GetParameters();
                if (method_param.Length != prs.Count())
                {
                    throw new MethodAccessException($"Can't find the method that matches this parameter count {{reason of error: {MethodName}}}.");
                }

                for (int i = 0; i < prs.Count(); i++)
                {
                    string param = prs[i].Trim();
                    object paramValue = null!;


                    var expectedType = method_param[i].ParameterType;


                    if (param.StartsWith("\"") && param.EndsWith("\""))
                    {

                        paramValue = param.Trim('"');
                    }
                    else if (param.StartsWith('[') && param.EndsWith(']'))
                    {
                        paramValue = TypeConverter.ConvertType(expectedType, AdvCall(param));
                    }
                    else if (param.StartsWith("{") && param.EndsWith("}"))
                    {

                        paramValue = param.Trim('{', '}');
                    }
                    else if (expectedType == typeof(string) || expectedType == typeof(object))
                    {
                        foreach (var item in Regex.Matches(param, @"\$([\w-]+\s*((?:\[(.*?)\]))*)").Select(x => x.Groups[1].Value))
                        {
                            var get = get_v("$" + item);
                            if (get.any)
                            {
                                param = param.Replace('$' + item, TypeConverter.ConvertType(typeof(string), get.item) as string);
                            }
                        }
                        paramValue = TypeConverter.ConvertType(typeof(string), param);
                    }
                    else if (param.StartsWith("$"))
                    {
                        paramValue = GetValueOnGet(param);

#if DEBUG
#endif
                    }
                    else
                    {
                        if (expectedType == typeof(Type))
                        {
                            paramValue = Type.GetType(param) ?? throw new ArgumentException($"Invalid type: {param}");
                        }
                        else if (!IsComplexExpression(param))
                        {
                            paramValue = TypeConverter.ConvertType(expectedType, param);
                        }
                        else
                        {

                            try
                            {
                                paramValue = EvaluateExpression(param);


                                paramValue = ConvertToExpectedType(paramValue, expectedType);
                            }
                            catch (Exception ex)
                            {
                                throw new ArgumentException($"Error evaluating expression '{param}': {ex.Message}");
                            }
                        }
                    }

                    args.Add(paramValue);
                }
            }

#if DEBUG
            
            for (int j = 0; j < args.Count; j++)
            {
                Console.WriteLine($"[DEBUG] Final argument {j}: {args[j]}");
            }
#endif


            return method.Invoke(isStatic ? null : method_owner, args.ToArray())!;
        }
        private string AdvCall(string str)
        {
            if (str == null)
                return null;
            str = str.TrimEnd();
            var newStr = "";
            if ((str.StartsWith('[') && !str.EndsWith("]"))
                || !str.StartsWith('[') && str.EndsWith("]"))
                throw new InvalidOperationException($"Invalid token : {str}");
            if(str.StartsWith("[") && str.EndsWith("]"))
            {
                newStr = string.Join("",str.TrimStart('[').ToCharArray().Take(str.Length-2));
            }
            if (newStr.Length == 0)
                return null!;
            var split = str.Split(' ');
            foreach (var item in split)
            {
                
                var item_infp = item.Split(' ');
                
                if (item_infp.Length == 0)
                    newStr = newStr.Replace(item, "");
                else if (item_infp.Length == 1)
                {
                    if(item_infp[0].StartsWith("@"))
                    {
                        newStr = newStr.Replace(item, CallMethod(string.Join("", item_infp[0].Skip(1)), null!).ToString());
                    }
                    else
                    {
                        newStr = newStr.Replace(item, TypeConverter.ConvertType(null, item_infp[0]).ToString());
                    }
                }
                else
                {
                    item_infp = [item_infp[0],string.Join("",item_infp.Skip(1))];
                    if (item_infp[0].StartsWith("@"))
                        newStr = newStr.Replace($"{item}", CallMethod(string.Join("", item_infp[0].Skip(1)), item_infp[1]).ToString());
                    else
                    {
                        throw new Exception($"Invalid token : {item}.");
                    }
                }

            }


            return newStr;
        }
        public void function(string Head, string body)
        {

            Regex HeadRegex = new Regex(@"(\w+[\w\-_]*)\s*{\s*((\$\w+\s*(?:,\s*\$\w+\s*)*)*)\s*}");
            Regex FunctionRegex = new(@"(\w+\D*)\s*{\s*((\$\w+\s*(?:,\s*\$\w+\s*)*)*)\s*}\s*,\s*{\s*(.*)\s*}");
            var FunctionInfo = FunctionRegex.Match(Head + ',' + '{' + body + '}');
            if (FunctionInfo.Success)
            {
                var HeadRegexResult = HeadRegex.Match(Head);
                if (HeadRegexResult.Success)
                {
                    string FunctionName = HeadRegexResult.Groups[1].Value;
                    string FunctionParms = HeadRegexResult.Groups[2].Value;
                    if (RoBin.System<TBased>.IsAlphanumeric(FunctionName))
                    {
                        Regex ParmsRegex = new(@"\$\w+");
                        var ParmsMatch = RoBin.System<TBased>.ParseAndValidateParameters(FunctionParms) ?? [];
                        if (LocalFuncs.Any(x => x.Name == FunctionName && ParmsMatch.Count == x.Parameters.Length))
                        {
                            throw new Exception($"Already exist function with the same parms count. use @delfunc {FunctionName} or @del {FunctionName} first.");
                        }
                        else
                        {

                            LocalFunc func = new()
                            {
                                Name = FunctionName,
                                Body = FunctionInfo.Groups[FunctionInfo.Groups.Count - 1].Value,

                            };
                            if (ParmsMatch.Count >= 1)
                            {

                                LocalFunc.Parameter[] parameters = new LocalFunc.Parameter[ParmsMatch.Count];
                                for (int i = 0; i < ParmsMatch.Count; i++)
                                {
                                    if (parameters.Where(x => x is not null).Any(x => x.name == ParmsMatch[i]))
                                    {
                                        throw new Exception($"Parameter can't have the same name {parameters[i]}");
                                    }
                                    parameters[i] = new(ParmsMatch[i]);
                                }
                                func.Parameters = parameters;




                            }
                            LocalFuncs.Add(func);
                        }
                    }
                    else
                    {
                        throw new Exception($"Invalid name for function {FunctionName}.");
                    }
                }
                else
                {
                    throw new Exception("Invalid function head token.");
                }
            }
            else
            {
                throw new InvalidOperationException("Invalid Function Token syntax.");
            }


        }
        public void QuickRefFunCall()
        {

        }
        public void callLocalFunc(string function)
        {
            var FunctionInfo = Regex.Match(function, @"@(.*)\s*\((.*)\)");
            if (FunctionInfo.Success)
            {
                string FunctionName = FunctionInfo.Groups[1].Value;
                string Parms = FunctionInfo.Groups[2].Value;

                var GetParms = SplitAsParm(Parms);
                if (LocalFuncs.FirstOrDefault(x => x.Name == FunctionName && x.Parameters.Length == GetParms.Count) is LocalFunc func)
                {



                    int count_check = 0;
                    GetParms.ForEach(x =>
                    {
                        if (x.StartsWith('$'))
                        {
                            if (LocalData.Any(x => "$"+x.name == GetParms[count_check]))
                                count_check++;
                            else
                            {
                                throw new AccessViolationException($"no data named {x}.");
                            }
                        }
                        else
                        {
                            count_check++;
                        }


                    });
                    string[] temp_ids = new string[GetParms.Count];
                    ref var LocalValues = ref LocalData;
                    var FunctionBody = func.Body;
                    for (int i = 0; i < temp_ids.Length; i++)
                    {
                        var temp_id = RoBin.System<TBased>.GenerateTempId();
                        if (GetParms[i].StartsWith('$'))
                        {
                            temp_ids[i] = temp_id;
                            LocalData.Add(new DataInfo(temp_id, LocalValues.First(x =>"$"+ x.name == GetParms[i])?.Object ?? throw new Exception($"No data named {GetParms[i]}")));
                        }
                        else if (GetParms[i].StartsWith('@'))
                        {
                            temp_ids[i] = temp_id;
                            LocalData.Add(new DataInfo(temp_id, CallMethod(
                                GetParms[i].Remove(0, 1).Split(' ')[0],
                                string.Join(" ", GetParms[i].Split(' ').Skip(1))
                            )));
                        }
                        else
                        {
                            
                            FunctionBody = Regex.Replace(FunctionBody, $@"(?<!\w){Regex.Escape(func.Parameters[i].name)}(?!\w)", TypeConverter.ConvertType(null, GetParms[i]).ToString()!);
                            continue;
                        }
                        FunctionBody = Regex.Replace(FunctionBody, $@"(?<!\w){Regex.Escape(func.Parameters[i].name)}(?!\w)", $"${temp_id}");

                    }
                                      
                    Execute(FunctionBody);
                    foreach (var temp_id in temp_ids)
                    {
                        var t = LocalValues.FirstOrDefault(x => x.name == temp_id);
                        if (t != null)
                        {
                            LocalData.Remove(t);
                        }
                    }
                }
                else
                {
                    throw new Exception($"Unable to find Method Named {FunctionName} or with the same Parms count.");
                }
            }
            else
            {
                throw new Exception($"Invalid Call token.{function}");
            }
        }
        public List<string> SplitAsParm(string parm)
        {
            // Regular expression pattern to match either:
            // - strings inside double quotes ("HELLO")
            // - braces ({HELLO})
            // - function-like calls with parameters (@add 1,2)
            // - individual elements separated by commas
            var pattern = @"(""[^""]*"")|(\{[^}]*\})|(@\w+\s*\d*,\d*)|([^,]+)|(\[.*\])";

            // Use Regex to find matches according to the pattern
            var matches = Regex.Matches(parm, pattern);

            // Extract the matched items into a string array
            string[] result = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                result[i] = matches[i].Value;
            }

            return result.ToList<string>();
        }
        private List<string> SplitParamsRespectingBracesAndQuotes(string input)
        {
            List<string> result = new List<string>();
            StringBuilder currentParam = new StringBuilder();
            int braceCount = 0;
            bool inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    currentParam.Append(c);
                }
                else if (c == '{' ||  c =='[' && !inQuotes )
                {
                    braceCount++;
                    currentParam.Append(c);
                }
                else if (c == '}' || c == ']' && !inQuotes)
                {
                    braceCount--;
                    currentParam.Append(c);
                }
                else if (c == ',' && braceCount == 0 && !inQuotes)
                {
                    result.Add(currentParam.ToString().Trim());
                    currentParam.Clear();
                }
                else
                {
                    currentParam.Append(c);
                }
            }


            if (currentParam.Length > 0)
            {
                result.Add(currentParam.ToString().Trim());
            }

            return result;
        }
        private object ConvertToExpectedType(object value, Type expectedType)
        {
            if (value == null || expectedType.IsAssignableFrom(value.GetType()))
            {
                return value;
            }

            try
            {

                return Convert.ChangeType(value, expectedType);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException($"Cannot convert '{value}' to expected type '{expectedType.Name}'.");
            }
        }
        private (PropertyInfo Prop, object Target, bool isStatic, bool found) DeepSearch(string Search, object IN)
        {

            if (IN == null)
            {
                return (null, null, false, false);
            }

            PropertyInfo propertyInfo = null;
            var steps = Search.Split('.');
            Type currentObject = (IN is Type ? IN as Type : IN.GetType())!;
            object targetObject = null;

            foreach (var step in steps)
            {
                if (step == steps.Last())
                    break;
                var properties = currentObject!.GetProperties();
                propertyInfo = properties.FirstOrDefault(prop => prop.Name == step)!;

                if (propertyInfo == null)
                {
                    if (currentObject.Name == step.Trim())
                    {
                        targetObject = currentObject;
                        propertyInfo = new GetAsProperty().ToProperty((currentObject as Type)!);
                        continue; 
                    }
                    else
                    {
                        // If neither property nor type name match, return not found
                        return (null, null, false, false);
                    }
                }

                bool isStatic = propertyInfo.GetMethod?.IsStatic ?? false;

                if (!isStatic)
                {
                    targetObject = currentObject;

                    try
                    {
                        var ob = propertyInfo.GetValue(currentObject);
                        currentObject = (ob is Type ? ob as Type : ob.GetType());
                    }
                    catch (TargetException ex)
                    {
                        Console.WriteLine($"Error accessing property '{propertyInfo.Name}': {ex.Message}");
                        return (null, null, false, false);
                    }
                }
                else
                {
                    targetObject = null;
                    currentObject = null;
                }
            }

            if (propertyInfo != null)
            {
                return (propertyInfo, targetObject, propertyInfo.GetMethod?.IsStatic ?? false, true)!;
            }

            return (null, null, false, false);
        }



    }
}
