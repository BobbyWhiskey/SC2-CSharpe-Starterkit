namespace Bot.Modules;

public class RepairUnitModule
{
    private const float RepairThreshold = 0.9f;
    private List<ulong> ReservedUnits = new List<ulong>();
    
    public void OnFrame()
    {
        // TODO Add same thing for buildings
        var mechanicals = Controller.GetUnits(Units.Mechanical, includeReservedUnits: true);
        RepairUnits(mechanicals);
        
        var buildings = Controller.GetUnits(Units.Structures, includeReservedUnits: true);
        RepairUnits(buildings);
    }

    private static void RepairUnits(List<Unit> mechanicals)
    {

        var damagedUnits = mechanicals.Where(x => x.Integrity < RepairThreshold);
        var scvs = Controller.GetUnits(Units.SCV);
        var freeScv = scvs.Where(x => x.Order.AbilityId != Abilities.REPAIR).ToList();

        foreach (var damagedUnit in damagedUnits)
        {
            if (scvs.All(x => x.Order.TargetUnitTag != damagedUnit.Tag))
            {
                var scv = Controller.GetFirstInRange(damagedUnit.Position, freeScv, 40);
                if (scv != null)
                {
                    scv.Ability(Abilities.REPAIR, damagedUnit);
                }

                //ReservedUnits.Add(scv.Tag);
            }
        }
    }
}