using FirefightersOptimized.Authorings;
using FirefightersOptimized.Components;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;
namespace FirefightersOptimized.Systems
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(SpawnSystem))]
    public partial struct UISystem : ISystem
    {
        private bool initialized;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            initialized = false;
        }
        
        public void OnUpdate(ref SystemState state)
        {
            var configEntity = SystemAPI.GetSingletonEntity<Config>();
            var configManaged = state.EntityManager.GetComponentObject<ConfigManaged>(configEntity);
            
            if (!initialized)
            {
                initialized = true;

                configManaged.UIController = Object.FindObjectOfType<UIController>();
            }
            
            var shouldReposition = configManaged.UIController.ShouldReposition();
            var totalFiresDoused = 0;
            
            foreach (var (team, entity) in SystemAPI.Query<RefRO<Team>>().WithEntityAccess())
            {
                totalFiresDoused += team.ValueRO.NumFiresDoused;
                
                if (shouldReposition)
                {
                    SystemAPI.SetComponentEnabled<RepositionLine>(entity, true);
                }    
            }
            
            configManaged.UIController.SetNumFiresDoused(totalFiresDoused);
        }
    }
}