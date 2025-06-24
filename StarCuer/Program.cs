using System.Reflection.Metadata.Ecma335;
using MQTTnet.Server;
using StarMqttClient.Commands;
using WxStarManager;
using WxStarManager.Models;

namespace StarCuer;

public class Program
{
    private static StarMqttClient.StarMqttClient StarMqtt { get; set; }
    private static string LastPresId { get; set; } = string.Empty;
    private static bool PresentationLoaded { get; set; }

    public static async Task Main(string[] args)
    {
        Config config = Config.Load();
        List<int> lfLoadIntervals = new() { 5, 15, 25, 35, 45, 55 };
        
        var client = new StarMqttClient.StarMqttClient(config.StarMqtt.Host,  
            config.StarMqtt.Port, 
            config.StarMqtt.Username, 
            config.StarMqtt.Password, 
            "testclient");
        
        StarMqtt = client;

        await StarMqtt.Connect();
        Console.WriteLine(DateTime.Now.Hour);

        while (true)
        {
            if (PresentationLoaded)
            {
                var t = 0;

                if (Enumerable.Range(0, 9).Contains(DateTime.Now.Minute))
                    t = 8 - DateTime.Now.Minute;
                
                if (Enumerable.Range(10, 19).Contains(DateTime.Now.Minute))
                    t = 18 - DateTime.Now.Minute;
                
                if (Enumerable.Range(20, 29).Contains(DateTime.Now.Minute))
                    t = 28 - DateTime.Now.Minute;
                
                if (Enumerable.Range(30, 39).Contains(DateTime.Now.Minute))
                    t = 38 - DateTime.Now.Minute;
                
                if (Enumerable.Range(40, 49).Contains(DateTime.Now.Minute))
                    t = 48 - DateTime.Now.Minute;
                
                if (Enumerable.Range(50, 59).Contains(DateTime.Now.Minute))
                    t = 58 - DateTime.Now.Minute;
                
                var startTime = DateTime.UtcNow + TimeSpan.FromMinutes(t) - TimeSpan.FromSeconds(DateTime.UtcNow.Second);
                Console.WriteLine($"Presentation will be ran @ {startTime.ToString("HH:mm:00")} local time.");

                await StarMqtt.PublishRunCue(LastPresId, startTime);
                PresentationLoaded = false;
                LastPresId = string.Empty;
                
                await Task.Delay(TimeSpan.FromSeconds(60));
                
                continue;
            }

            if (lfLoadIntervals.Contains(DateTime.Now.Minute))
            {
                var presentationId = RandomString(24);
                var cues = await GenerateLoads(CueType.LocalForecast);

                await StarMqtt.PublishLoadCue(cues, presentationId);
                PresentationLoaded = true;
                LastPresId = presentationId;

                Console.WriteLine($"Loaded a presentation for {cues.Count()} units with id {presentationId}");

                await Task.Delay(10 * 1000);
            }
            else
            {
                await Task.Delay(10 * 1000);
            }

            if (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0)
            {
                var presentationId = RandomString(24);
                var cues = await GenerateLoads(CueType.LowerDisplayLine);
                await StarMqtt.PublishLoadCue(cues, presentationId);
                
                PresentationLoaded = true;
                LastPresId = presentationId;
                await Task.Delay(10 * 1000);
            }

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