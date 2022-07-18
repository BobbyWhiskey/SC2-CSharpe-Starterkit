using SC2APIProtocol;

namespace Bot.Micro;

public class VikingsMicro : IUnitMicro
{
    public void OnFrame()
    {
        var fighters = Controller.GetUnits(Units.VIKING_FIGHTER);


        if (Controller.frame % 10 == 0)
        {
            foreach (var fighter in fighters)
            {
                var enemy = Controller.GetFirstInRange(fighter.position,
                    Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy)
                        .Where(x => Controller.CanUnitAttackAir(x.unitType)).ToList()
                    , 8);
                if (enemy != null)
                {
                    fighter.Move(fighter.position - enemy.position + fighter.position);
                }
            }
        }
    }
}