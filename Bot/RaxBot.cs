using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

namespace Bot {
    internal class RaxBot : Bot
    {
        public RaxBot()
        {
            _buildingModule = new BuildingModule();
            _spawnerModule = new SpawnerModule();
            _researchModule = new ResearchModule();
        }

        private BuildingModule _buildingModule;
        private ResearchModule _researchModule;
        private readonly SpawnerModule _spawnerModule;
        private Vector3? _lastAttackPosition;
        private ulong _lastAttackPositionUpdate;

        //the following will be called every frame
        //you can increase the amount of frames that get processed for each step at once in Wrapper/GameConnection.cs: stepSize  
        public IEnumerable<Action> OnFrame() {
            Controller.OpenFrame();

            if (Controller.frame == 0) {
                Logger.Info("RaxBot");
                Logger.Info("--------------------------------------");
                Logger.Info("Map: {0}", Controller.gameInfo.MapName);
                Logger.Info("--------------------------------------");
            }

            if (Controller.frame == Controller.SecsToFrames(1)) 
                Controller.Chat("gl hf");

            var structures = Controller.GetUnits(Units.Structures);
            if (structures.Count == 1) {
                //last building                
                if (structures[0].integrity < 0.4) //being attacked or burning down                 
                    if (!Controller.chatLog.Contains("gg"))
                        Controller.Chat("gg");                
            }

            var resourceCenters = Controller.GetUnits(Units.ResourceCenters);
            foreach (var rc in resourceCenters) {
                // Bad condition
                if (rc.assignedWorkers < rc.idealWorkers && Controller.CanConstruct(Units.SCV))
                {
                    rc.Train(Units.SCV);
                }
            }

            if (Controller.frame % 50 == 0)
                Controller.DistributeWorkers();
            
            this._researchModule.OnFrame();

            //if (Controller.frame % 50 == 0)
                this._buildingModule.OnFrame();
            
            //if (Controller.frame % 50 == 0)
                this._spawnerModule.OnFrame();

            //attack when we have enough units
            var army = Controller.GetUnits(Units.ArmyUnits);
            
            if (Controller.frame % 20 == 0
             && army.Count > 25) {
                if (Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy).Any())
                {
                    _lastAttackPosition = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy).First().position;
                    Controller.Attack(army, _lastAttackPosition.Value);
                }
                else if (_lastAttackPosition.HasValue)
                {
                    if (Controller.frame - _lastAttackPositionUpdate > 500)
                    {
                        _lastAttackPosition = _lastAttackPosition.Value.MidWay(Controller.enemyLocations[0]);
                        _lastAttackPositionUpdate = Controller.frame;
                    }
                    Controller.Attack(army, _lastAttackPosition.Value);
                }
                else if (Controller.enemyLocations.Count > 0)
                {
                    _lastAttackPosition = Controller.enemyLocations[0].MidWay(resourceCenters.First().position);
                }
            }            

            return Controller.CloseFrame();
        }
    }
}