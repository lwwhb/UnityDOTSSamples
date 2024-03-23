using FirefightersOptimized.Systems;
using Unity.Entities;
using UnityEngine;

namespace FirefightersOptimized.Common
{
    
    public class ExecuteSystems : MonoBehaviour
    {
        public bool ExecuteHeapSystem = false;
        public bool ExecuteBotSystem = false;
        public bool ExecuteBucketSystem = false;
        public bool ExecuteTeamSystem = false;
        public bool ExecuteUISystem = false;
        public bool ExecuteAnimationSystem = false;

        public void Start()
        {
            if (ExecuteHeapSystem)
            {
                var heatSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<HeatSystem>();
                World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SimulationSystemGroup>()
                    .AddSystemToUpdateList(heatSystem);
            }

            if (ExecuteBotSystem)
            {
                var botSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BotSystem>();
                World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SimulationSystemGroup>()
                    .AddSystemToUpdateList(botSystem);
            }

            if (ExecuteBucketSystem)
            {
                var bucketSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BucketSystem>();
                World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SimulationSystemGroup>()
                    .AddSystemToUpdateList(bucketSystem);
            }

            if (ExecuteTeamSystem)
            {
                var lineSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<LineSystem>();
                World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SimulationSystemGroup>()
                    .AddSystemToUpdateList(lineSystem);
            }
        }
    }
}