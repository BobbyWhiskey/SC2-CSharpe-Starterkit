using SC2APIProtocol;

namespace Bot.Modules;

public class AntiChangelingModule
{
    public void OnFrame()
    {
        var changelings = Controller.GetUnits(Units.Changelings, Alliance.Enemy);

        foreach (var changeling in changelings)
        {
            var unitInRange = Controller.GetFirstInRange(changeling.position, Controller.GetUnits(Units.ArmyUnits), 10);
            if (unitInRange != null)
            {
                unitInRange.Ability(Abilities.ATTACK, changeling);
            }
        }
    }
}