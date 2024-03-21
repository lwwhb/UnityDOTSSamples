using Unity.Burst;
using Unity.Collections;


namespace FirefightersOptimized.SharedStaticData
{
    public struct SharedHeapMap
    {
        public static readonly SharedStatic<SharedHeapMap> SharedValue = SharedStatic<SharedHeapMap>.GetOrCreate<SharedHeapMap>();
        
        public SharedHeapMap(int capacity)
        {
            heapmap = new NativeArray<float>(capacity, Allocator.Persistent);
        }

        public void Dispose()
        {
            heapmap.Dispose();
        }

        public NativeArray<float> heapmap; 
    }
}