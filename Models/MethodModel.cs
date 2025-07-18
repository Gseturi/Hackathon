using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGenerator.Models
{
    public class MethodModel
    {
        public string Name { get; set; }
        public string Parameters { get; set; }
        public string ReturnType { get; set; }
        public string Body { get; set; }
        public string ContainingClass { get; set; }
        public string Namespace { get; set; }
        public bool IsAsync { get; internal set; }
    }
}
