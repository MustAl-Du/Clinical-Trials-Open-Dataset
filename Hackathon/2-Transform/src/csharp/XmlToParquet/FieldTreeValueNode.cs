namespace XmlToParquet
{
    internal class FieldTreeValueNode : FieldTreeNode
    {
        public int Index { get; }

        public FieldTreeValueNode(string name, int index)
            : base(name)
        {
            this.Index = index;
        }
    }
}
