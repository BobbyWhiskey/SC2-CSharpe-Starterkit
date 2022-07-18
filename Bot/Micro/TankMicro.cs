using SC2APIProtocol;

namespace Bot.Micro;

public class TankMicro : IUnitMicro
{
    public void OnFrame()
    {
        if (Controller.frame % 10 != 0)
        {
            // This seems to be needed because if we span Abilities.SIEGE_TANK too much the tank never actually sieges
            return;
        }
        
        var tanks = Controller.GetUnits(Units.SIEGE_TANK);
        var siegedTanks = Controller.GetUnits(Units.SIEGE_TANK_SIEGED);

        foreach (var tank in tanks)
        {
            if (Controller.GetFirstInRange(tank.position, Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy), 13 - 1) != null)
            {
                tank.Ability(Abilities.SIEGE_TANK);
            }
        }

        foreach (var tank in siegedTanks)
        {
            if (Controller.GetFirstInRange(tank.position, Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy), 13 + 1) == null)
            {
                tank.Ability(Abilities.UNSIEGE_TANK);
            }
        }
    }
}