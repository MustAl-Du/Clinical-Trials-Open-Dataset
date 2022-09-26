namespace XmlToParquet
{
    internal class Settings
    {
        public string SchemaFile { get; set; } = "Schema.csv";
        public string Source { get; set; } = "Source.xml";
        public string OutputFile { get; set; } = "Output.parquet";
        public string RowIdField { get; set; } = "IdInfoNctId";

    }
}
