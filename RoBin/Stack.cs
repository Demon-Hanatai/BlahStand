using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoBin
{
    public class Stack
    {
        private Stack<object> stacks = new Stack<object>();
        public void Push(object value)
            { stacks.Push(TypeConverter.ConvertType(null!, value)); }
        public object Pop() => stacks.Pop();
        public object Peak() => stacks.Peek();
    }
}
