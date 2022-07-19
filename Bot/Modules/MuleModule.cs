using SC2APIProtocol;

namespace Bot.Modules;

public class MuleModule
{
    public void OnFrame()
    {
        var ocs = Controller.GetUnits(Units.ORBITAL_COMMAND);
        foreach (var unit in ocs)
        {
            if (unit.Energy > 100
                && unit.AssignedWorkers < unit.IdealWorkers + 2
                && unit.IdealWorkers > 10)
            {
                var minerals = Controller.GetUnits(Units.MineralFields.ToHashSet(), Alliance.Neutral);
                var target = Controller.GetFirstInRange(unit.Position, minerals, 10);
                if (target != null)
                {
                    unit.Ability(Abilities.CALL_DOWN_MULE, target);
                }
            }
        }
    }
}