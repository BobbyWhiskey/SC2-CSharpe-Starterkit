using SC2APIProtocol;

namespace Bot.Modules;

public class MuleModule
{
    public void OnFrame()
    {
        var ocs = Controller.GetUnits(Units.ORBITAL_COMMAND);
        foreach (var unit in ocs)
        {
            if (unit.energy > 50
                && unit.assignedWorkers < unit.idealWorkers + 2
                && unit.idealWorkers > 10)
            {
                var minerals = Controller.GetUnits(Units.MineralFields.ToHashSet(), Alliance.Neutral);
                var target = Controller.GetFirstInRange(unit.position, minerals, 10);
                if (target != null)
                {
                    unit.Ability(Abilities.CALL_DOWN_MULE, target);
                }
            }
        }
    }
}