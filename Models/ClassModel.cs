using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGenerator.Models
{
    public class ClassModel
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public List<MethodModel> Methods { get; set; } = new();
    }

}
