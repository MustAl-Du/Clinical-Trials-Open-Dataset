namespace SchemaFromXsd
{
    internal class Settings
    {
        public const string TextFormat = "TEXT";
        public const string CsvFormat = "CSV";

        public string SchemaFile { get; set; } = "public.xsd";
        public string OutputFile { get; set; } = @"Fields.txt";
        public string OutputFormat { get; set; } = Settings.TextFormat;
    }
}
