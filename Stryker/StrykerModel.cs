using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGenerator.Stryker
{
    public class StrykerModel
    {
        public string ClassName { get; set; }
        public double MutationScore { get; set; }
        public int TotalMutations { get; set; }
        public int Killed { get; set; }
        public int Survived { get; set; }
    }
}
