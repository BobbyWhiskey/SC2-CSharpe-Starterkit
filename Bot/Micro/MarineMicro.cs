using Bot.Micro.Shared;
using SC2APIProtocol;

namespace Bot.Micro;

public class MarineMicro : IUnitMicro
{
    private static readonly int StimRangeActivation = 10;
    private static readonly int StimUnitCountThreshold = 2;
    private static readonly int RangeToFlee = 3;
    private static readonly int StimRangeActivationDelay = 11;

    private readonly Dictionary<ulong, ulong> _lastActivationTimeMap = new();

    public void OnFrame()
    {
        var marines = Controller.GetUnits(Units.MARINE, includeReservedUnits:true);
        var dangerousUnits = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy)
            .Where(x => Controller.CanUnitAttackGround(x.UnitType)).ToList();

        foreach (var marine in marines)
        {
            KeepDistanceToEnemyMicro.OnFrame(marine, dangerousUnits,3, RangeToFlee, (ulong)(Controller.FRAMES_PER_SECOND * 0.2) );
        }
        
    // if (Controller.Frame % 3 == 0)
    // {
    //     foreach (var marine in marines)
    //     {
    //         var enemy = Controller.GetFirstInRange(marine.Position,
    //             dangerousUnits
    //             , RangeToFlee);
    //         if (enemy != null)
    //         {
    //             marine.Move(marine.Position - enemy.Position + marine.Position);
    //         }
    //     }
    // }

    // TODO Check if we researched stim
    foreach (var marine in marines)
        {
            if (marine.Integrity > 0.6f)
            {
                var enemyUnits = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy)
                    .Where(x => (marine.Position - x.Position).Length() < StimRangeActivation);

                if (enemyUnits.Count() > StimUnitCountThreshold && marine.Integrity > 0.6f)
                {
                    var found = _lastActivationTimeMap.TryGetValue(marine.Tag, out var lastActivationTime);
                    if (!found
                        || lastActivationTime < Controller.Frame - Controller.SecsToFrames(StimRangeActivationDelay))
                    {
                        marine.Ability(Abilities.GENERAL_STIMPACK);
                        _lastActivationTimeMap[marine.Tag] = Controller.Frame;
                    }

                }
            }
        }
    }
}