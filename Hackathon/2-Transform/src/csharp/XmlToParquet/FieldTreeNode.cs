namespace XmlToParquet
{
    internal class FieldTreeNode : FieldTreeNodeBase
    {
        public Dictionary<string, FieldTreeNodeBase> Children { get; }

        public FieldTreeNode(string name)
            : base(name)
        {
            this.Children = new();
        }
    }
}
