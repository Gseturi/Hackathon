using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGenerator.Models
{
    /// <summary>
    /// Represents a parsed class from the source code, including metadata and structure.
    /// </summary>
    public class ClassModel
    {
        /// <summary>
        /// The class name (without namespace).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The namespace of the class.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// List of using directives required by the class.
        /// </summary>
        public List<string> Usings { get; set; } = new();

        /// <summary>
        /// List of base types (inheritance or interface implementations).
        /// </summary>
        public List<string> BaseTypes { get; set; } = new();

        /// <summary>
        /// Field declarations (raw code or structured representations).
        /// </summary>
        public List<string> Fields { get; set; } = new();

        /// <summary>
        /// Property declarations (raw code or structured).
        /// </summary>
        public List<string> Properties { get; set; } = new();

        /// <summary>
        /// Parsed methods of the class.
        /// </summary>
        public List<MethodModel> Methods { get; set; } = new();

        /// <summary>
        /// The entire class source code (as it appears in file).
        /// </summary>
        public string FullClassBody { get; set; }

        /// <summary>
        /// List of dependent classes used by this class (recursively gathered).
        /// </summary>
        public List<ClassModel> Dependencies { get; set; } = new();

        /// <summary>
        /// The path to the original file (optional, for debugging).
        /// </summary>
        public string FilePath { get; set; }
    }
}

