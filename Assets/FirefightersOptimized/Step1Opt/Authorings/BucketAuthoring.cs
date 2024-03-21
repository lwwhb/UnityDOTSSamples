using FirefightersOptimized.Components;
using Unity.Entities;
using UnityEngine;

namespace FirefightersOptimized.Authorings
{
    public class BucketAuthoring : MonoBehaviour
    {
        private class Baker : Baker<BucketAuthoring>
        {
            public override void Bake(BucketAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent<Bucket>(entity);
                AddSharedComponent(entity, new TeamID {teamId = -1});
            }
        }
    }
    
    public struct Bucket : IComponentData
    {
        public float Water;  // 0 = empty, 1 = full
        public Entity CarryingBot;
    }
}