using Bot.Micro.Shared;
using SC2APIProtocol;

namespace Bot.Micro;

public class MedicavMicro : IUnitMicro
{
    public void OnFrame()
    {
        var medivacs = Controller.GetUnits(Units.MEDIVAC, includeReservedUnits:true);
        var dangerousUnits = Controller.GetUnits(Units.All, Alliance.Enemy)
            .Where(x => Controller.CanUnitAttackAir(x.UnitType)).ToList();
        foreach (var medivac in medivacs)
        {
            KeepDistanceToEnemyMicro.OnFrame(medivac, dangerousUnits,3, 6, (ulong)(Controller.FRAMES_PER_SECOND * 0.3) );
        }
        //
        // foreach (var medivac in medivacs)
        // {
        //     var enemy = Controller.GetFirstInRange(medivac.Position,
        //         dangerousUnits
        //         , 6);
        //     if (enemy != null)
        //     {
        //         medivac.Move(medivac.Position - enemy.Position + medivac.Position);
        //     }
        // }
    }
}