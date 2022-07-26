namespace Bot.Modules;

public class RepairUnitModule
{
    private const float RepairThreshold = 0.9f;
    private List<ulong> ReservedUnits = new List<ulong>();
    
    public void OnFrame()
    {
        // TODO Check if that was causing bad games
        //return;
        
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
                var scvsInRange = Controller.GetInRange(damagedUnit.Position, scvs, 40)
                    .OrderBy(x => (x.Position - damagedUnit.Position).LengthSquared());
                
                if (scvsInRange.Any())
                {
                    if (damagedUnit.UnitType == Units.BUNKER)
                    {
                        var repairScvs = scvsInRange.Take(2);
                        foreach (var repairScv in repairScvs)
                        {
                            repairScv.Ability(Abilities.REPAIR, damagedUnit);
                        }
                    }
                    else
                    {
                        scvsInRange.First().Ability(Abilities.REPAIR, damagedUnit);
                    }
                    
                }

                //ReservedUnits.Add(scv.Tag);
            }
        }
    }
}