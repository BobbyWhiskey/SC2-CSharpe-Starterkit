using System.Linq;

namespace Bot
{
    public class ResearchModule
    {
        public void OnFrame()
        {
            var bay = Controller.GetUnits(Units.ENGINEERING_BAY)
                .Where(b => b.order.AbilityId == 0 && !(b.buildProgress < 1));
            var armory = Controller.GetUnits(Units.ARMORY)
                .Where(b => b.order.AbilityId == 0 && !(b.buildProgress < 1));
            
            foreach (var unit in bay)
            {
                // TODO MC This is spammy af
                unit.Ability(Abilities.RESEARCH_UPGRADE_INFANTRY_WEAPON);
                unit.Ability(Abilities.RESEARCH_UPGRADE_INFANTRY_ARMOR);
            }
            
        }
    }
}