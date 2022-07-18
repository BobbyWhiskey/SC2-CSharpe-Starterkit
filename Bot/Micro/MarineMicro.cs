using SC2APIProtocol;

namespace Bot.Micro;

public class MarineMicro : IUnitMicro
{
    private static readonly int StimRangeActivation = 10;
    private static int StimRangeActivationDelay = 500;

    private readonly Dictionary<ulong, ulong> _lastActivationTimeMap = new();

    public void OnFrame()
    {
        var marines = Controller.GetUnits(Units.MARINE);

        if (Controller.frame % 10 == 0)
        {
            foreach (var marine in marines)
            {
                var enemy = Controller.GetFirstInRange(marine.position,
                    Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy)
                        .Where(x => Controller.CanUnitAttackGround(x.unitType)).ToList()
                    , 3);
                if (enemy != null)
                {
                    marine.Move(marine.position - enemy.position + marine.position);
                }
            }
        }


        // TODO Check if we researched stim
        foreach (var marine in marines)
        {
            var enemyUnits = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy)
                .Where(x => (marine.position - x.position).Length() < StimRangeActivation);

            if (enemyUnits.Any())
            {
                var found = _lastActivationTimeMap.TryGetValue(marine.tag, out var lastActivationTime);
// TODO Move this before this if
                if (!found || lastActivationTime < Controller.frame - 500)
                {
                    marine.Ability(Abilities.GENERAL_STIMPACK);
                    _lastActivationTimeMap[marine.tag] = Controller.frame;
                }

            }
        }
    }
}