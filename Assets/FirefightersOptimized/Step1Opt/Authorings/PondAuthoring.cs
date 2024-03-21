using Unity.Entities;
using UnityEngine;

namespace FirefightersOptimized.Authorings
{
    public class PondAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PondAuthoring>
        {
            public override void Bake(PondAuthoring authoring)
            {
				var entity = GetEntity(authoring, TransformUsageFlags.WorldSpace);
                AddComponent<Pond>(entity);
            }
        }
    }

	public struct Pond : IComponentData
    {
    }
}