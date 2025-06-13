using WebAppTester.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WebAppTester.Services
{
    /// <summary>
    /// Service for loading and parsing YAML test configuration files
    /// </summary>
    public class YamlConfigurationService
    {
        /// <summary>
        /// Loads a test configuration from a YAML file
        /// </summary>
        /// <param name="filePath">Path to the YAML file</param>
        /// <returns>The parsed TestConfiguration object</returns>
        public TestConfiguration LoadConfiguration(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"YAML configuration file not found: {filePath}");
            }

            string yamlContent = File.ReadAllText(filePath);
            return ParseYaml(yamlContent);
        }

        /// <summary>
        /// Parses YAML content into a TestConfiguration object
        /// </summary>
        /// <param name="yamlContent">YAML content as string</param>
        /// <returns>The parsed TestConfiguration object</returns>
        private TestConfiguration ParseYaml(string yamlContent)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var config = deserializer.Deserialize<TestConfiguration>(yamlContent);

            // Initialize collections if they are null
            if (config.Variables == null)
            {
                config.Variables = new Dictionary<string, string>();
            }

            return config;
        }
    }
}
