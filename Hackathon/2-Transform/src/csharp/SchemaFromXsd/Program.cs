using Microsoft.Extensions.Configuration;
using System;


namespace SchemaFromXsd
{
    public class Program
    {
        static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            Settings settings = config.GetRequiredSection(nameof(Settings)).Get<Settings>() 
                ?? throw new InvalidOperationException("Failed to read app settings.");

            SchemaBuilder schemaBuilder = new(settings);
            schemaBuilder.Run();
        }
    }
}
