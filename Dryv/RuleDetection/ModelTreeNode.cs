using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Dryv.RuleDetection
{
    [DebuggerDisplay("{" + nameof(ModelPath) + "}")]
    internal class ModelTreeNode
    {
        public string UniquePath { get; set; }
        public string ModelPath { get; set; }

        public Type ModelType { get; set; }

        public List<ModelTreeEdge> Children { get; set; } = new();
        public bool IsRecursive { get; set; }
        public List<MemberInfo> Hierarchy { get; set; }
    }
}
