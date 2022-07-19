using SC2APIProtocol;

namespace Bot.Micro;

public class VikingsMicro : IUnitMicro
{
    public void OnFrame()
    {
        var fighters = Controller.GetUnits(Units.VIKING_FIGHTER);

        var dangerousUnits = Controller.GetUnits(Units.All, Alliance.Enemy)
            .Where(x => Controller.CanUnitAttackAir(x.UnitType)).ToList();

        // if (Controller.frame % 10 == 0)
        // {
            foreach (var fighter in fighters)
            {
                var enemy = Controller.GetFirstInRange(fighter.Position, dangerousUnits, 7);
                if (enemy != null)
                {
                    fighter.Move(fighter.Position - enemy.Position + fighter.Position);
                }
            }
        //}
    }
}