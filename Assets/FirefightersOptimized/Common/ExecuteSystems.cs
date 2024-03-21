using FirefightersOptimized.Systems;
using Unity.Entities;
using UnityEngine;

namespace FirefightersOptimized.Common
{
    
    public class ExecuteSystems : MonoBehaviour
    {
        public bool ExecuteHeapSystem = false;

        public void Start()
        {
            if (ExecuteHeapSystem)
            {
                var group = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<HeatSystem>();
                World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SimulationSystemGroup>()
                    .AddSystemToUpdateList(group);
            }
        }
    }
}