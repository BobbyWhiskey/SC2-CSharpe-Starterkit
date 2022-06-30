using SC2APIProtocol;

namespace Bot.Micro;

public class TankMicro
{
    public void OnFrame()
    {
        var tanks = Controller.GetUnits(Units.SIEGE_TANK);
        var siegedTanks = Controller.GetUnits(Units.SIEGE_TANK_SIEGED);
        
        foreach (var tank in tanks)
        {
            if (Controller.GetFirstInRange(tank.position, Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy), 13 + 2) != null)
            {
                tank.Ability(Abilities.SIEGE_TANK);
            }
        }
        
        foreach (var tank in siegedTanks)
        {
            if (Controller.GetFirstInRange(tank.position, Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy), 13 + 4) == null)
            {
                tank.Ability(Abilities.UNSIEGE_TANK);
            }
        }
    }
}