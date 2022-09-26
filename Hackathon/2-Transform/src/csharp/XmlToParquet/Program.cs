using Microsoft.Extensions.Configuration;
using XmlToParquet;

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

Settings settings = config.GetRequiredSection(nameof(Settings)).Get<Settings>()
    ?? throw new InvalidOperationException("Failed to read app settings.");

Converter converter = new(settings);
await converter.ConvertAsync();