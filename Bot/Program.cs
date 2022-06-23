using System;
using SC2APIProtocol;

namespace Bot {
    public class Program {
        // Settings for your bot.
        private static readonly Bot bot = new RaxBot();
        private const Race race = Race.Terran;

        // Settings for single player mode.
//        private static string mapName = "AbyssalReefLE.SC2Map";
//        private static string mapName = "AbiogenesisLE.SC2Map";
//        private static string mapName = "FrostLE.SC2Map";
        //private static readonly string mapName = "BlackburnAIE.SC2Map"; //"WorldofSleepersLE.SC2Map";// "ThunderbirdLE.SC2Map";
        //private static readonly string mapName = "WorldofSleepersLE.SC2Map";// "ThunderbirdLE.SC2Map";
        //private static readonly string mapName = "ThunderbirdLE.SC2Map";
        //private static readonly string mapName = "CuriousMindsAIE.SC2Map";

        private static readonly Race opponentRace = Race.Random;
        private static readonly Difficulty opponentDifficulty = Difficulty.VeryEasy;

        public static GameConnection gc;

        public static void Main(string[] args) {
            Logger.Info("Staring bot!!! pew pew");
            var mapName = PickRandomMap();
            try {
                gc = new GameConnection();
                if (args.Length == 0){
                    gc.readSettings();
                    gc.RunSinglePlayer(bot, mapName, race, opponentRace, opponentDifficulty).Wait();
                }
                else
                    gc.RunLadder(bot, race, args).Wait();
            }
            catch (Exception ex) {
                Logger.Info(ex.ToString());
            }

            Logger.Info("Terminated.");
        }

        private static string PickRandomMap()
        {
            // D:\Games\BattleNet\StarCraft II\Maps
            var files = Directory.GetFiles(@"D:\Games\BattleNet\StarCraft II\Maps");
            var random = new Random();
            var randomFileName = files[random.NextInt64(0, files.Length)];
            return randomFileName.Substring(randomFileName.LastIndexOf("\\") + 1);
        }
    }
}