using SC2APIProtocol;

namespace Bot.Micro;

public class MarauderMicro : IUnitMicro
{
    private static readonly int StimRangeActivation = 10;
    private static int StimRangeActivationDelay = 500;

    private readonly Dictionary<ulong, ulong> _lastActivationTimeMap = new();

    public void OnFrame()
    {
        var marines = Controller.GetUnits(Units.MARAUDER);

        // TODO Check if we researched stim
        foreach (var marine in marines)
        {
            var enemyUnits = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy)
                .Where(x => (marine.Position - x.Position).Length() < StimRangeActivation);

            if (enemyUnits.Any())
            {
                var found = _lastActivationTimeMap.TryGetValue(marine.Tag, out var lastActivationTime);
// TODO Move this before this if
                if (!found || lastActivationTime < Controller.frame - 500)
                {
                    marine.Ability(Abilities.GENERAL_STIMPACK);
                    _lastActivationTimeMap[marine.Tag] = Controller.frame;
                }
            }
        }
    }
}