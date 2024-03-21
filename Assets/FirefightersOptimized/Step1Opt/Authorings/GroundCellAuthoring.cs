using Unity.Entities;
using UnityEngine;

namespace FirefightersOptimized.Authorings
{
    public class GroundCellAuthoring : MonoBehaviour
    {
        private class Baker : Baker<GroundCellAuthoring>
        {
            public override void Bake(GroundCellAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.WorldSpace);
                AddComponent<GroundCell>(entity);
            }
        }
    }
    
    public struct GroundCell : IComponentData
    {

    }
}