using Bot.Queries;
using SC2APIProtocol;

namespace Bot.Modules;

public class BunkerModule
{
    public void OnFrame()
    {
        var bunkers = Controller.GetUnits(Units.BUNKER);

        if (!bunkers.Any() && BuildOrderQueries.IsBuildOrderCompleted())
        {
            // TODO Build some bunkers?
        }
        else
        {
            foreach (var bunker in bunkers)
            {
                var enemies = Controller.GetInRange(bunker.Position, Controller.GetUnits(Units.All, Alliance.Enemy), 13);
                if (enemies.Any() && bunker.Original.Passengers.Count < 4)
                {
                    var marines = Controller.GetInRange(bunker.Position, Controller.GetUnits(Units.MARINE), 15);

                    foreach (var unit in marines.Take(4))
                    {
                        unit.Smart(bunker);
                    }
                }
            }
        }
    }
}