using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RoBin
{
    public class GetAsProperty
    {
        public dynamic _0_0_type { get;  set; }
        public PropertyInfo ToProperty(Type type)
        {
            _0_0_type = type as Type;
           
            Console.WriteLine(_0_0_type.FullName);
            Console.WriteLine(_0_0_type.DeclaringType);
            return this.GetType().GetProperty(nameof(_0_0_type))!;

        }
    }
}
