using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RoBin
{
    /// <summary>
    /// A helper class that builds a dynamic class from a provided name and body.
    /// </summary>
    public class DynamicClassBuilder
    {
        public Type BuildDynamicClass(string className)
        {
            TypeBuilder typeBuilder = CreateClass(className);

            // Add a parameterless constructor
            DefineDefaultConstructor(typeBuilder);

            return typeBuilder.CreateTypeInfo().AsType();
        }

        private TypeBuilder CreateClass(string className)
        {
            AssemblyName assemblyName = new AssemblyName("DynamicAssembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

            // Define the class using Reflection.Emit
            TypeBuilder typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Public);
            return typeBuilder;
        }

        private void DefineDefaultConstructor(TypeBuilder typeBuilder)
        {
            // Define a public, parameterless constructor
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                Type.EmptyTypes);

            ILGenerator ilGenerator = constructorBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret); // Simply return, no logic needed for the constructor
        }
    }



}
