using SC2APIProtocol;

namespace Bot.Micro;

public class MarineMicro : IUnitMicro
{
    private static int StimRangeActivation = 10;
    private static int StimRangeActivationDelay = 500;

    private Dictionary<ulong, ulong> _lastActivationTimeMap = new Dictionary<ulong, ulong>();

    public void OnFrame()
    {
        var marines = Controller.GetUnits(Units.MARINE);

        // TODO Check if we researched stim
        foreach (var marine in marines)
        {
            
            // TODO Check if we didnt activate stim recently for 
            
            
            var enemyUnits = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy)
                .Where(x => (marine.position - x.position).Length() < StimRangeActivation);

            if (enemyUnits.Any())
            {
                // TODO Activate marine stim
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