using System.Xml;
using System.Xml.Serialization;

namespace StarCuer;

[XmlRoot("ServerConfig")]
public class Config
{
    public static string ConfigPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "StarCuer.config");

    [XmlElement] public string StarApiEndpoint { get; set; } = "http://127.0.0.1:8000";
    [XmlElement] public bool IgnoreDesiredFlavors { get; set; } = false;
    [XmlElement] public string OverrideLfi2 { get; set; } = "domestic/v";
    [XmlElement] public string OverrideLdli2 { get; set; } = "domestic/ldlC";
    [XmlElement] public string OverrideLfi1 { get; set; } = "local E";
    [XmlElement] public StarMqttConfig StarMqtt { get; set; } = new();

    public static Config config = new();
    
    public static Config Load()
    {
        XmlSerializer serializer = new XmlSerializer(typeof(Config));
        XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
        ns.Add("", "");

        if (!File.Exists(ConfigPath))
        {
            config = new Config();
            serializer.Serialize(File.Create(ConfigPath), config, ns);

            return config;
        }

        using (FileStream fs = new FileStream(ConfigPath, FileMode.Open))
        {
            var deserializedConfig = serializer.Deserialize(fs);

            if (deserializedConfig != null && deserializedConfig is Config cfg)
            {
                config = cfg;
                return config;
            }

            return new Config();
        }
    }
    
}

[XmlRoot("MqttConfig")]
public class StarMqttConfig
{
    [XmlElement] public string Host { get; set; } = "127.0.0.1";
    [XmlElement] public int Port { get; set; } = 1883;
    [XmlElement] public string Username { get; set; } = "REPLACE_ME"
    [XmlElement] public string Password { get; set; } = "REPLACE_ME"
}