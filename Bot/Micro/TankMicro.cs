using SC2APIProtocol;

namespace Bot.Micro;

public class TankMicro : IUnitMicro
{
    private const int UnitCountSiegeThreshold = 2;
    
    public void OnFrame()
    {
        if (Controller.Frame % 5 != 0)
        {
            // This seems to be needed because if we span Abilities.SIEGE_TANK too much the tank never actually sieges
            return;
        }
        
        var tanks = Controller.GetUnits(Units.SIEGE_TANK);
        var siegedTanks = Controller.GetUnits(Units.SIEGE_TANK_SIEGED);

        var enemyArmy = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy).ToList();

        foreach (var tank in tanks) 
        {
            var unitsInRange = Controller.GetInRange(tank.Position,enemyArmy,  13 + 1);
            
            if (unitsInRange.Count() > UnitCountSiegeThreshold)
            {
                tank.Ability(Abilities.SIEGE_TANK);
            }
        }

        foreach (var tank in siegedTanks)
        {
            if (Controller.GetFirstInRange(tank.Position, Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy), 13 + 1) == null)
            {
                tank.Ability(Abilities.UNSIEGE_TANK);
            }
        }
    }
}