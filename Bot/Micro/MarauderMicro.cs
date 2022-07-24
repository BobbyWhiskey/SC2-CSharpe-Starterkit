using Bot.Micro.Shared;
using SC2APIProtocol;

namespace Bot.Micro;

public class MarauderMicro : IUnitMicro
{
    private static readonly int StimRangeActivation = 10;
    private static int StimRangeActivationDelay = 500;

    private readonly Dictionary<ulong, ulong> _lastActivationTimeMap = new();

    public void OnFrame()
    {
        var marauders = Controller.GetUnits(Units.MARAUDER, includeReservedUnits:true);

        var dangerousUnits = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy)
            .Where(x => Controller.CanUnitAttackGround(x.UnitType)).ToList();

        foreach (var unit in marauders)
        {
            KeepDistanceToEnemyMicro.OnFrame(unit, dangerousUnits,3, 2, (ulong)(Controller.FRAMES_PER_SECOND * 0.2) );
        }
        
        // TODO Check if we researched stim
        foreach (var unit in marauders)
        {
            var enemyUnits = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy)
                .Where(x => (unit.Position - x.Position).Length() < StimRangeActivation);

            if (enemyUnits.Any())
            {
                var found = _lastActivationTimeMap.TryGetValue(unit.Tag, out var lastActivationTime);
// TODO Move this before this if
                if (!found || lastActivationTime < Controller.Frame - 500)
                {
                    unit.Ability(Abilities.GENERAL_STIMPACK);
                    _lastActivationTimeMap[unit.Tag] = Controller.Frame;
                }
            }
        }
    }
}