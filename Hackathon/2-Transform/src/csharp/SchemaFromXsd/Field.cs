namespace SchemaFromXsd
{
    internal class Field
    {
        public string Name { get; }
        public string Type { get; }

        public Field(string name, string type)
        {
            this.Name = name;
            this.Type = type;
        }
    }
}
