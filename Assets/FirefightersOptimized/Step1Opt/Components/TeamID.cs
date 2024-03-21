using Unity.Entities;

namespace FirefightersOptimized.Components
{
    public struct TeamID : ISharedComponentData
    {
        public int teamId;
    }
    
    public struct Team : IComponentData
    {
        public int NumFiresDoused;
    }
}