using SC2APIProtocol;

namespace Bot.Micro;

public class RavenMicro : IUnitMicro
{
    public void OnFrame()
    {
        var medivacs = Controller.GetUnits(Units.RAVEN);

        foreach (var medivac in medivacs)
        {
            var enemy = Controller.GetFirstInRange(medivac.position,
                Controller.GetUnits(Units.All, Alliance.Enemy)
                    .Where(x => Controller.CanUnitAttackAir(x.unitType)).ToList()
                , 9);
            if (enemy != null)
            {
                medivac.Move(medivac.position - enemy.position + medivac.position);
            }
        }
    }
}