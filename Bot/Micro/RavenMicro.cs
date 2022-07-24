using Bot.Micro.Shared;
using SC2APIProtocol;

namespace Bot.Micro;

public class RavenMicro : IUnitMicro
{
    public void OnFrame()
    {
        var ravens = Controller.GetUnits(Units.RAVEN, includeReservedUnits:true);

        var dangerousUnits = Controller.GetUnits(Units.All, Alliance.Enemy)
            .Where(x => Controller.CanUnitAttackAir(x.UnitType)).ToList();
        
        foreach (var unit in ravens)
        {
            KeepDistanceToEnemyMicro.OnFrame(unit, dangerousUnits, 1, 9, (ulong)(Controller.FRAMES_PER_SECOND * 0.3) );
        }
        //
        // foreach (var medivac in medivacs)
        // {
        //     var enemy = Controller.GetFirstInRange(medivac.Position,
        //         dangerousUnits
        //         , 9);
        //     if (enemy != null)
        //     {
        //         medivac.Move(medivac.Position - enemy.Position + medivac.Position);
        //     }
        // }
    }
}