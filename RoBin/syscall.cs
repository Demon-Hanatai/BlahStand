using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RoBin
{
    file static class Win
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadLibrary(string dllName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);
    }
    public  class Winsyscall<TB>  where TB : new()
    {


        private RoBin.BoBin<TB> _rb;
        public Winsyscall(RoBin.BoBin<TB> boBin)
        {
            _rb=boBin;


        }



        dynamic CallWindowsApi(string dllName,Type returnType, string functionName, params object[] args)
        {
            IntPtr hModule = Win.LoadLibrary(dllName);
            if (hModule == IntPtr.Zero)
            {
                throw new Exception($"Failed to load library: {dllName}");
            }

            try
            {
                IntPtr procAddress = Win.GetProcAddress(hModule, functionName);
                if (procAddress == IntPtr.Zero)
                {
                    throw new Exception($"Failed to get function address: {functionName}");
                }

             
                DynamicMethod dymamicDelegate = new(functionName + "Delegate", returnType, args.Select
                    (x => x.GetType()).ToArray(), typeof(Win));
                ILGenerator il = dymamicDelegate.GetILGenerator();

                for (int i = 0; i < args.Length; i++)
                    il.Emit(OpCodes.Ldarg, i);//Loading all the args 
                il.Emit(OpCodes.Ldc_I8, procAddress.ToInt64());
                il.Emit(OpCodes.Conv_I);
                il.EmitCalli(OpCodes.Calli, CallingConvention.StdCall, returnType, args.Select
                    (x => x.GetType()).ToArray());

                il.Emit(OpCodes.Ret);
                // Assuming Convertedprams contains parameter values and you want their types for creating the delegate type
                Type[] parameterTypes = args.Select(x => x.GetType()).ToArray();

                // Get the delegate type using Expression.GetDelegateType, which includes parameter types and a return type
                Type delegateType = Expression.GetDelegateType(parameterTypes.Concat(new[] { returnType }).ToArray());

                // Create the delegate
                var wincall = dymamicDelegate.CreateDelegate(delegateType);
                return wincall.DynamicInvoke(args)!;
            }
            finally
            {
                Win.FreeLibrary(hModule);
            }
        }
        /// <summary>
        /// @WINAPI("User32.dll", "MessageBoxA", "int", "int", "string", "string", "int");

        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public  dynamic WINAPI(string input)
        {
            Regex regex = new Regex(@"""(?<dll>[^""]+)"",\s*""(?<name>[^""]+)"",\s*(?<parms>[^)]+)\s*\[(?<returnType>.*)\]");
            Match match = regex.Match(input);

            if (match.Success)
            {
                string dll = match.Groups["dll"].Value;
                string name = match.Groups["name"].Value;
                string parms = match.Groups["parms"].Value;
                string returnType = match.Groups["returnType"].Value;

                if (string.IsNullOrEmpty(returnType))
                {
                    throw new Exception("No return type specified for the function.");
                }

              
                var returnTypeAsType  = Type.GetType(returnType);
                if (returnTypeAsType == null)
                {
                    throw new Exception($"Invalid return type '{returnType}'.");
                }
                return  CallWindowsApi(dll,returnTypeAsType!,name,PassValue(SplitRespectingQuotesAndVariables(parms)).ToArray());
            }
            else
            {
                throw new Exception("Invalid WINAPI declaration. Ensure the format is correct: @WINAPI {\"dll\", \"function\", \"parms\"  [returnType] }");
            }
            
        }
        List<object> PassValue(List<string> values)
        {
            List<object> result = new List<object>();
            foreach (var item in values)
            {
                if(item.StartsWith("$") || item.StartsWith("@"))
                {
                    
                    result.Add(_rb.GetValueOnGet(TypeConverter.ConvertType(null!, item).ToString()!));
                }
                else
                {
                    result.Add(TypeConverter.ConvertType(null!, item));
                }
            }
            return result;
        }
        static List<string> SplitRespectingQuotesAndVariables(string input)
        {
            List<string> result = new List<string>();
            StringBuilder currentPart = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes; // Toggle inQuotes
                    currentPart.Append(c);
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentPart.ToString().Trim());
                    currentPart.Clear();
                }
                else
                {
                    currentPart.Append(c);
                }
            }

            if (currentPart.Length > 0)
            {
                result.Add(currentPart.ToString().Trim());
            }

            return result;
        }
        public nint allocate(string size)
        {
            return Marshal.AllocCoTaskMem(int.TryParse(size, out int result)?result:0);
        }
    }
}
