using MQTTnet.Server;
using StarMqttClient.Commands;
using WxStarManager;
using WxStarManager.Models;

namespace StarCuer;

public class Program
{
    public static StarMqttClient.StarMqttClient StarMqtt { get; private set; }
    public static string LastPresId { get; set; } = string.Empty;
    public static bool PresentationLoaded { get; set; }

    public static async Task Main(string[] args)
    {
        Config config = Config.Load();
        var client = new StarMqttClient.StarMqttClient(config.StarMqtt.Host,  config.StarMqtt.Port, config.StarMqtt.Username, config.StarMqtt.Password, "testclient");
        StarMqtt = client;

        await StarMqtt.Connect();

        while (true)
        {
            if (PresentationLoaded)
            {
                var startTime = DateTime.Now + TimeSpan.FromMinutes(4);
                Console.WriteLine($"Presentation will be ran @ {startTime.ToString("HH:mm:ss")} local time.");

                await StarMqtt.PublishRunCue(LastPresId, startTime);
                PresentationLoaded = false;
                LastPresId = string.Empty;

                continue;
            }

            if (!DateTime.Now.Minute.ToString().Contains('4'))
            {
                await Task.Delay(500);
                continue;
            }

            var presentationId = RandomString(24);
            var cues = await GenerateLoads(CueType.LocalForecast);

            await StarMqtt.PublishLoadCue(cues, presentationId);
            PresentationLoaded = true;
            LastPresId = presentationId;

            Console.WriteLine($"Loaded a presentation for {cues.Count()} units with id {presentationId}");
            await Task.Delay(12 * 1000);
        }
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
    
    private static Random _random = new();
    
    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
}