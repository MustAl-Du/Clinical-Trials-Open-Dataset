namespace SchemaFromXsd
{
    internal class Field
    {
        public string Name { get; }
        public string Type { get; }
        public string Path { get; }

        public Field(string name, string type, string path)
        {
            this.Name = name;
            this.Type = type;
            this.Path = path;
        }
    }
}
