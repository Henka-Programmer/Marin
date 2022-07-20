using System.Collections.Generic;

namespace DynamicFilter
{
    public partial class Expression
    {
        private sealed class TermStack : Stack<(object leaf, Model model, string? alias)>
        {
            public new void Push((object leaf, Model model, string? alias) leaf)
            {
                SearchDomain.CheckLeaf(leaf.leaf);
                base.Push(leaf);
            }
        }
    }
}
