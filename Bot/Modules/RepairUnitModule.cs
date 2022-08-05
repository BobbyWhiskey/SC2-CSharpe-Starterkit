namespace Bot.Modules;

public class RepairUnitModule
{
    private const float RepairThreshold = 1f;
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
        var damagedUnits = mechanicals.Where(x => x.Integrity < RepairThreshold && x.BuildProgress == 1);
        var scvs = Controller.GetUnits(Units.SCV);
        var freeScvs = scvs.Where(x => x.Order.AbilityId != Abilities.REPAIR).ToList();

        foreach (var damagedUnit in damagedUnits)
        {
            var nbScvToSend = 1;
            if (damagedUnit.UnitType == Units.BUNKER)
            {
                if (damagedUnit.Integrity > 0.75)
                {
                    nbScvToSend = 2;
                }
                else
                {
                    nbScvToSend = 4;
                }
            }

            var scvsAlreadyRepairing = scvs.Where(x => x.Order.TargetUnitTag == damagedUnit.Tag && x.Order.AbilityId == Abilities.REPAIR).ToList();
            
            if (scvsAlreadyRepairing.Count < nbScvToSend)
            {
                var freeScvsInRange = Controller.GetInRange(damagedUnit.Position, freeScvs, 40)
                    .Except(scvsAlreadyRepairing)
                    .OrderBy(x => (x.Position - damagedUnit.Position).LengthSquared())
                    .ToList();
                
                if (freeScvsInRange.Any())
                {
                    var scvsToOrder = freeScvsInRange.Take(nbScvToSend);
                    foreach (var repairScv in scvsToOrder)
                    {
                        repairScv.Ability(Abilities.REPAIR, damagedUnit);
                    }

                }
                //ReservedUnits.Add(scv.Tag);
            }
        }
    }
}