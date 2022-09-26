namespace XmlToParquet
{
    internal abstract class FieldTreeNodeBase
    {
        public string Name { get; }

        protected FieldTreeNodeBase(string name)
        {
            this.Name = name;
        }

        public FieldTreeValueNode this[string path]
        {
            get
            {
                return this.GetValueNode(path) ??
                    throw new KeyNotFoundException($"Failed to find filed with path {path} in tree.");
            }
        }

        public FieldTreeValueNode? GetValueNode(string path) 
        {
            FieldTreeNodeBase node = this;
            string[] pathParts = path.Split('>');
            int i = 0;
            while (node is FieldTreeNode parentNode
                && i < pathParts.Length
                && parentNode.Children.ContainsKey(pathParts[i]))
            {
                node = parentNode.Children[pathParts[i]];
                i++;
            }

            if (node is FieldTreeValueNode valueNode && i == pathParts.Length)
            {
                return valueNode;
            }

            return null;
        }
    }
}
