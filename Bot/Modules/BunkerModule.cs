using Bot.Queries;
using SC2APIProtocol;

namespace Bot.Modules;

public class BunkerModule
{
    private const ulong DelayToReleaseUnits = (ulong)(Controller.FRAMES_PER_SECOND * 15);
    private readonly Dictionary<ulong, ulong> _lastEnemySeen = new();

    public void OnFrame()
    {
        var bunkers = Controller.GetUnits(Units.BUNKER);

        foreach (var bunker in bunkers)
        {
            var enemies = Controller.GetInRange(bunker.Position, Controller.GetUnits(Units.All, Alliance.Enemy), 13)
                .ToList();

            if (bunker.Original.Passengers.Count < 4
                && !(bunker.BuildProgress < 1)
                && (enemies.Any() || Controller.Frame < Controller.SecsToFrames(5 * 60)))
            {
                var marines = Controller.GetInRange(bunker.Position, Controller.GetUnits(Units.MARINE), 15);
                _lastEnemySeen[bunker.Tag] = Controller.Frame;
                foreach (var unit in marines.Take(4))
                {
                    unit.Smart(bunker);
                }
            }
            else if (!enemies.Any()
                     && bunker.Original.Passengers.Count > 0
                     && Controller.Frame > Controller.SecsToFrames(5 * 60))
            {
                var found = _lastEnemySeen.TryGetValue(bunker.Tag, out var lastEnemySeen);
                if (found && lastEnemySeen + DelayToReleaseUnits < Controller.Frame)
                {
                    bunker.Ability(Abilities.UNLOAD_BUNKER);
                }
            }
        }
    }
}