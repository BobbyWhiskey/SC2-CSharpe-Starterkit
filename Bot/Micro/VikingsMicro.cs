using Bot.Micro.Shared;
using SC2APIProtocol;

namespace Bot.Micro;

public class VikingsMicro : IUnitMicro
{
    public void OnFrame()
    {
        var fighters = Controller.GetUnits(Units.VIKING_FIGHTER, includeReservedUnits:true);

        var dangerousUnits = Controller.GetUnits(Units.All, Alliance.Enemy)
            .Where(x => Controller.CanUnitAttackAir(x.UnitType)).ToList();

        foreach (var unit in fighters)
        {
            KeepDistanceToEnemyMicro.OnFrame(unit, dangerousUnits, 1, 6, (ulong)(Controller.FRAMES_PER_SECOND * 0.2) );
        }
        //
        // // if (Controller.frame % 10 == 0)
        // // {
        //     foreach (var fighter in fighters)
        //     {
        //         var enemy = Controller.GetFirstInRange(fighter.Position, dangerousUnits, 7);
        //         if (enemy != null)
        //         {
        //             fighter.Move(fighter.Position - enemy.Position + fighter.Position);
        //         }
        //     }
        // //}
    }
}