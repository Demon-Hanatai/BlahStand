using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoBin
{
    public class LocalFunc
    {
        public record Parameter(string name);
        public required string Name { get; set; }
        public  Parameter[] Parameters = [];
        public required string Body { get; set; }

    }
}
