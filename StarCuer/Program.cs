using StarMqttClient.Commands;
using WxStarManager;
using WxStarManager.Models;

namespace StarCuer;

public class Program
{
    public static StarMqttClient.StarMqttClient StarMqtt { get; private set; }

    public static async Task Main(string[] args)
    {
        Config config = Config.Load();
        var client = new StarMqttClient.StarMqttClient(config.StarMqtt.Host,  config.StarMqtt.Port, config.StarMqtt.Username, config.StarMqtt.Password, "testclient");
        var presId = RandomString(24);
        StarMqtt = client;

        await StarMqtt.Connect();

        var cues = await GenerateLoads(CueType.LocalForecast);
        
        await client.PublishLoadCue(cues, presId);

        await client.PublishRunCue(presId, DateTime.UtcNow);
    }

    public static async Task<List<LoadCue>> GenerateLoads(CueType cueType)
    {
        var cueList = new List<LoadCue>();
        var api = new Api(Config.config.StarApiEndpoint);
        
        var cueSettings = await api.GetStarCueSettings(WxStarModel.IntelliStar2);

        foreach (StarCueSetting cueSetting in cueSettings)
        {
            var loadCue = new LoadCue();

            loadCue.StarUuid = cueSetting.StarId;
            loadCue.Duration = 3600; // TODO: This needs to be present in the API.

            switch (cueType)
            {
                case CueType.LocalForecast:
                    loadCue.Flavor = cueSetting.GfxPkgLf;
                    break;
                case CueType.LowerDisplayLine:
                    loadCue.Flavor = cueSetting.GfxPkgLdl;
                    break;
            }

            cueList.Add(loadCue);
        }

        return cueList;
    }
    
    private static Random random = new();
    
    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}