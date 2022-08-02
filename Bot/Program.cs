using SC2APIProtocol;

namespace Bot;

public class Program
{
    private const Race race = Race.Terran;

    // Settings for your bot.
    private static readonly Bot bot = new RaxBot();

    // Settings for single player mode.
    private static readonly Race opponentRace = Race.Random;
    private static readonly Difficulty opponentDifficulty = Difficulty.VeryHard;

    public static GameConnection gc = null!;

    public static void Main(string[] args)
    {
        Logger.Info("Staring bot!!! pew pew");

        try
        {
            gc = new GameConnection();
            if (args.Length == 0)
            {
                gc.readSettings();
                var mapName = PickRandomMap();
                Controller.IsDebug = true;
                gc.RunSinglePlayer(bot, mapName, race, opponentRace, opponentDifficulty).Wait();
            }
            else
            {
                gc.RunLadder(bot, race, args).Wait();
            }
        }
        catch (Exception ex)
        {
            Logger.Info(ex.ToString());
        }

        Logger.Info("Terminated.");
    }

    private static string PickRandomMap()
    {
        //return "AcropolisLE.SC2Map";
        //return "GlitteringAshesAIE.SC2Map";
        //return "BlackburnAIE.SC2Map";
        //return "WorldofSleepersLE.SC2Map";
        //return "HardwireAIE.SC2Map";
        return "2000AtmospheresAIE.SC2Map";
        
        var files = Directory.GetFiles(@"D:\Games\BattleNet\StarCraft II\Maps");
        var random = new Random();
        var randomFileName = files[random.NextInt64(0, files.Length)];
        return randomFileName.Substring(randomFileName.LastIndexOf("\\") + 1);
    }
}