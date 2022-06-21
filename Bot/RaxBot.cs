using System.Collections.Generic;
using System.Linq;
using SC2APIProtocol;

namespace Bot {
    internal class RaxBot : Bot
    {
        public RaxBot()
        {
            _buildingModule = new BuildingModule();
            _spawnerModule = new SpawnerModule();
        }

        private BuildingModule _buildingModule;
        private readonly SpawnerModule _spawnerModule;

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

            this._buildingModule.OnFrame();
            this._spawnerModule.OnFrame();

            //attack when we have enough units
            var army = Controller.GetUnits(Units.ArmyUnits);
            if (army.Count > 15) {
                if (Controller.enemyLocations.Count > 0)
                {
                    Controller.Attack(army, Controller.enemyLocations[0].MidWay(resourceCenters.First().position).MidWay(Controller.enemyLocations[0]));
                }
            }            

            return Controller.CloseFrame();
        }
    }
}