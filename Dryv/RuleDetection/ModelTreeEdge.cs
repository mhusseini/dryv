using System.Diagnostics;
using System.Reflection;

namespace Dryv.RuleDetection
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class ModelTreeEdge
    {
        public ModelTreeNode Child { get; set; }
        public string Name => this.Member?.Name;
        public ModelTreeNode Parent { get; set; }
        public MemberInfo Member { get; set; }
    }
}