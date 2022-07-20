using SC2APIProtocol;

namespace Bot.Micro;

public class MarineMicro : IUnitMicro
{
    private static readonly int StimRangeActivation = 10;
    private static readonly int StimUnitCountThreshhold = 2;
    private static int StimRangeActivationDelay = 500;

    private readonly Dictionary<ulong, ulong> _lastActivationTimeMap = new();

    public void OnFrame()
    {
        var marines = Controller.GetUnits(Units.MARINE);
        var dangerousUnits = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy)
            .Where(x => Controller.CanUnitAttackGround(x.UnitType)).ToList();

        if (Controller.Frame % 10 == 0)
        {
            foreach (var marine in marines)
            {
                var enemy = Controller.GetFirstInRange(marine.Position,
                    dangerousUnits
                    , 3);
                if (enemy != null)
                {
                    marine.Move(marine.Position - enemy.Position + marine.Position);
                }
            }
        }

        // TODO Check if we researched stim
        foreach (var marine in marines)
        {
            var enemyUnits = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy)
                .Where(x => (marine.Position - x.Position).Length() < StimRangeActivation);

            if (enemyUnits.Count() > StimUnitCountThreshhold)
            {
                var found = _lastActivationTimeMap.TryGetValue(marine.Tag, out var lastActivationTime);
                if (!found || lastActivationTime < Controller.Frame - 500)
                {
                    marine.Ability(Abilities.GENERAL_STIMPACK);
                    _lastActivationTimeMap[marine.Tag] = Controller.Frame;
                }

            }
        }
    }
}