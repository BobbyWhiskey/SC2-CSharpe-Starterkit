using System.Numerics;
using SC2APIProtocol;

namespace Bot.Modules;

public class ScoutingModule
{
    private static ulong ScoutingInterval = Controller.SecsToFrames(75);
    private static ulong ScoutingStartingFrame = Controller.SecsToFrames(60);
    private ulong LastScoutingFrame = ulong.MinValue;
    private Random _random = new Random();
    
    private List<MineralOwnershipInfo>? _mineralLinesInfo;

    private ulong CurrentScoutingUnit = 0;

    public void OnFrame()
    {
        // TODO Check if that was causing bad games
        // return;
        
        CheckOnScout();

        if (_mineralLinesInfo == null)
        {
            _mineralLinesInfo = new List<MineralOwnershipInfo>();
            
            var infos = Controller.MineralClusters.Select(x => new MineralOwnershipInfo()
            {
                CenterPosition = x,
            });

            foreach (var mineralOwnershipInfo in infos)
            {
                _mineralLinesInfo.Add(mineralOwnershipInfo);
            }
        }
        
        
        // if (CurrentScoutingUnit != 0)
        // {
        //     // TODO Is that a duplicate of whats in 
        //     var unit = Controller.GetUnits(Units.All, includeReservedUnits:true).FirstOrDefault(x => x.Tag == CurrentScoutingUnit);
        //     if (unit == null)
        //     {
        //         Controller.ReleaseUnit(CurrentScoutingUnit);
        //         CurrentScoutingUnit = 0;
        //     }
        //     else if (unit.Order.AbilityId == 0)
        //     {
        //         Controller.ReleaseUnit(unit.Tag);
        //         CurrentScoutingUnit = 0;
        //     }
        // }

        if (CurrentScoutingUnit == 0
            && (Controller.Frame > LastScoutingFrame + ScoutingInterval
             || Controller.Frame > ScoutingStartingFrame && LastScoutingFrame == 0))
        {
            var vikings = Controller.GetUnits(Units.VIKING_FIGHTER);
            if (vikings.Any())
            {
                var viking = vikings.First();
                
                Controller.ReserveUnit(viking.Tag);
                CurrentScoutingUnit = viking.Tag;

                var mineralToScout = this._mineralLinesInfo[_random.Next(0, this._mineralLinesInfo.Count)];
                Controller.Attack(new List<Unit>(){viking}, mineralToScout.CenterPosition);
                
                mineralToScout = this._mineralLinesInfo[_random.Next(0, this._mineralLinesInfo.Count)];
                Controller.Attack(new List<Unit>(){viking}, mineralToScout.CenterPosition, true);
                
                mineralToScout = this._mineralLinesInfo[_random.Next(0, this._mineralLinesInfo.Count)];
                Controller.Attack(new List<Unit>(){viking}, mineralToScout.CenterPosition, true);
                
                mineralToScout = this._mineralLinesInfo[_random.Next(0, this._mineralLinesInfo.Count)];
                Controller.Attack(new List<Unit>(){viking}, mineralToScout.CenterPosition, true);

                //viking.Ability(Abilities.ATTACK, mineralToScout.CenterPosition);

                LastScoutingFrame = Controller.Frame;
            }
            
            if (LastScoutingFrame == ulong.MinValue)
            {
                // Just scout with SCV at the begining
                var scvs = Controller.GetUnits(Units.SCV);
                if (scvs.Any())
                {
                    var scv = scvs.First();

                    Controller.ReserveUnit(scv.Tag);
                    CurrentScoutingUnit = scv.Tag;

                    if (LastScoutingFrame == 0)
                    {
                        scv.Move(Controller.EnemyLocations.First()); 
                    
                    }
                    else
                    {
                        var mineralToScout = this._mineralLinesInfo[_random.Next(0, this._mineralLinesInfo.Count)];
                        scv.Move(mineralToScout.CenterPosition);
                    }

                    LastScoutingFrame = Controller.Frame;
                }
            }
            
        }

    }

    private void CheckOnScout()
    {
        if (CurrentScoutingUnit == 0)
        {
            return;
        }
        
        var unit = Controller.GetUnits(Units.All, includeReservedUnits: true).FirstOrDefault(x => x.Tag == CurrentScoutingUnit);
        if (unit == null)
        {
            Controller.ReleaseUnit(CurrentScoutingUnit);
            CurrentScoutingUnit = 0;
        }
        else if (Controller.GetFirstInRange(unit.Position, Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy), 14) != null || unit.Order.AbilityId == 0)
        {
            Controller.ReleaseUnit(unit.Tag);
            CurrentScoutingUnit = 0;
            
            unit.Move(Controller.StartingLocation);
        }
    }

    public class MineralOwnershipInfo
    {
        public bool EnemyExtensionDetected { get; set; }
        public Vector3 CenterPosition { get; set; }
        public ulong LastUpdateFrame { get; set; }
    }
}