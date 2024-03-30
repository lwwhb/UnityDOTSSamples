using Unity.Entities;

namespace FirefightersOptimized.Components
{
    public struct TeamID : ISharedComponentData
    {
        public Entity team;
    }
    
    public struct Team : IComponentData
    {
        public int Id;
        public Entity Bucket;
        public Entity Filler;
        public Entity Douser;
        public int NumFiresDoused;
    }
}