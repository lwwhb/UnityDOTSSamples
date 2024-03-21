using FirefightersOptimized.SharedStaticData;
using FirefightersOptimized.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Config = FirefightersOptimized.Authorings.Config;
using GroundCell = FirefightersOptimized.Authorings.GroundCell;

namespace FirefightersOptimized.Systems
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(SpawnSystem))]
    public partial struct HeatSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<GroundCell>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();

            // simulate the heat spreading
            {
                HeatSpread_MainThread(ref state, config);
                //state.Dependency = HeatSpread_SingleThreadedJob(state.Dependency, ref state, config);
                //state.Dependency = HeatSpread_ParallelJob(state.Dependency, ref state, config);
            }

            // update the colors and heights of the ground cells from the heat data
            {
                GroundCellUpdate_MainThread(ref state, config);
                //state.Dependency = GroundCellUpdate_SingleThreadedJob(state.Dependency, ref state, config);
                //state.Dependency = GroundCellUpdate_ParallelJob(state.Dependency, ref state, config);
            }
            
            /*JobHandle handle1 = HeatSpread_SingleThreadedJob(state.Dependency, ref state, config);
            JobHandle handle2 = GroundCellUpdate_SingleThreadedJob(state.Dependency, ref state, config);*/
            
            /*JobHandle handle1 = HeatSpread_ParallelJob(state.Dependency, ref state, config);
            JobHandle handle2 = GroundCellUpdate_ParallelJob(state.Dependency, ref state, config);*/
            
            //state.Dependency = JobHandle.CombineDependencies(handle1, handle2);
        }
        
        private void HeatSpread_MainThread(ref SystemState state, Config config)
        {
            var speed = SystemAPI.Time.DeltaTime * config.HeatSpreadSpeed;
            int numColumns = config.GroundNumColumns;
            int numRows = config.GroundNumRows;

            for (int index = 0; index < numColumns * numRows; index++)
            {
                int row = index / numColumns;
                int col = index % numRows;

                var prevCol = col - 1;
                var nextCol = col + 1;
                var prevRow = row - 1;
                var nextRow = row + 1;

                float increase = 0;

                increase += Index(row, nextCol, numColumns, numRows);
                increase += Index(row, prevCol, numColumns, numRows);

                increase += Index(prevRow, prevCol, numColumns, numRows);
                increase += Index(prevRow, col, numColumns, numRows);
                increase += Index(prevRow, nextCol, numColumns, numRows);

                increase += Index(nextRow, prevCol, numColumns, numRows);
                increase += Index(nextRow, col, numColumns, numRows);
                increase += Index(nextRow, nextCol, numColumns, numRows);

                increase *= speed;

                SharedHeapMap.SharedValue.Data.heapmap[index] = math.min(1, SharedHeapMap.SharedValue.Data.heapmap[index] + increase);
            }
        }

        private static float Index(int row, int col, int numColumns, int numRows)
        {
            if (col < 0 || col >= numColumns ||
                row < 0 || row >= numRows)
            {
                return 0;
            }

            return SharedHeapMap.SharedValue.Data.heapmap[row * numColumns + col];
        }

        private JobHandle HeatSpread_SingleThreadedJob(JobHandle dependency, ref SystemState state, Config config)
        {
            var heatSpreadJob = new HeatSpreadJob_SingleThreaded()
            {
                HeatSpreadSpeed = SystemAPI.Time.DeltaTime * config.HeatSpreadSpeed,
                NumColumns = config.GroundNumColumns,
                NumRows = config.GroundNumRows
            };
            return heatSpreadJob.Schedule(dependency);
        }

        [BurstCompile]
        public struct HeatSpreadJob_SingleThreaded : IJob
        {
            public float HeatSpreadSpeed;
            public int NumColumns;
            public int NumRows;

            public void Execute()
            {
                for (int index = 0; index < NumColumns * NumRows; index++)
                {
                    int row = index / NumColumns;
                    int col = index % NumColumns;

                    var prevCol = col - 1;
                    var nextCol = col + 1;
                    var prevRow = row - 1;
                    var nextRow = row + 1;

                    float increase = 0;

                    increase += Index(row, nextCol);
                    increase += Index(row, prevCol);

                    increase += Index(prevRow, prevCol);
                    increase += Index(prevRow, col);
                    increase += Index(prevRow, nextCol);

                    increase += Index(nextRow, prevCol);
                    increase += Index(nextRow, col);
                    increase += Index(nextRow, nextCol);

                    increase *= HeatSpreadSpeed;

                    SharedHeapMap.SharedValue.Data.heapmap[index] = math.min(1, SharedHeapMap.SharedValue.Data.heapmap[index] + increase);
                }
            }

            private float Index(int row, int col)
            {
                if (col < 0 || col >= NumColumns ||
                    row < 0 || row >= NumRows)
                {
                    return 0;
                }

                return SharedHeapMap.SharedValue.Data.heapmap[row * NumColumns + col];
            }
        }

        private JobHandle HeatSpread_ParallelJob(JobHandle dependency, ref SystemState state, Config config)
        {
            var heatSpreadJob = new HeatSpreadJob_Parallel
            {
                HeatSpreadSpeed = SystemAPI.Time.DeltaTime * config.HeatSpreadSpeed,
                NumColumns = config.GroundNumColumns,
                NumRows = config.GroundNumRows
            };
            return heatSpreadJob.Schedule(config.GroundNumColumns*config.GroundNumRows, 100, dependency);
        }

        [BurstCompile]
        public struct HeatSpreadJob_Parallel : IJobParallelFor
        {
            public float HeatSpreadSpeed;
            public int NumColumns;
            public int NumRows;

            public void Execute(int index)
            {
                int row = index / NumColumns;
                int col = index % NumColumns;

                var prevCol = col - 1;
                var nextCol = col + 1;
                var prevRow = row - 1;
                var nextRow = row + 1;

                float increase = 0;

                increase += Index(row, nextCol);
                increase += Index(row, prevCol);

                increase += Index(prevRow, prevCol);
                increase += Index(prevRow, col);
                increase += Index(prevRow, nextCol);

                increase += Index(nextRow, prevCol);
                increase += Index(nextRow, col);
                increase += Index(nextRow, nextCol);

                increase *= HeatSpreadSpeed;

                SharedHeapMap.SharedValue.Data.heapmap[index] = math.min(1, SharedHeapMap.SharedValue.Data.heapmap[index] + increase);
            }

            private float Index(int row, int col)
            {
                if (col < 0 || col >= NumColumns ||
                    row < 0 || row >= NumRows)
                {
                    return 0;
                }

                return SharedHeapMap.SharedValue.Data.heapmap[row * NumColumns + col];
            }
        }


        private void GroundCellUpdate_MainThread(ref SystemState state, Config config)
        {
            int idx = 0;
            var minY = -(config.GroundCellYScale / 2);
            var maxY = minY + config.GroundCellYScale;
            var elapsedTime = (float)SystemAPI.Time.ElapsedTime;

            foreach (var (trans, color) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<URPMaterialPropertyBaseColor>>()
                         .WithAll<GroundCell>())
            {
                var heat = SharedHeapMap.SharedValue.Data.heapmap[idx];

                // oscillate the displayed heat so that the fire looks a little more organic
                {
                    var radians = Random.CreateFromIndex((uint)idx).NextFloat(math.PI * 2) + elapsedTime;
                    var oscillationOffset =
                        math.sin(radians) * heat * config.HeatOscillationScale; // the more heat, the more oscillation
                    heat += oscillationOffset;
                }

                trans.ValueRW.Position.y = math.lerp(minY, maxY, heat);
                color.ValueRW.Value = math.lerp(config.MinHeatColor, config.MaxHeatColor, heat);

                idx++;
            }
        }

        private JobHandle GroundCellUpdate_SingleThreadedJob(JobHandle dependency, ref SystemState state, Config config)
        {
            var minY = -(config.GroundCellYScale / 2);
            var groundCellUpdateJob = new GroundCellUpdate
            {
                Config = config,
                ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                MinY = minY,
                MaxY = minY + config.GroundCellYScale,
            };

            return groundCellUpdateJob.Schedule(dependency);
        }

        private JobHandle GroundCellUpdate_ParallelJob(JobHandle dependency, ref SystemState state, Config config)
        {
            var minY = -(config.GroundCellYScale / 2);
            var groundCellUpdateJob = new GroundCellUpdate
            {
                Config = config,
                ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                MinY = minY,
                MaxY = minY + config.GroundCellYScale,
            };

            return groundCellUpdateJob.ScheduleParallel(dependency);
        }

        [WithAll(typeof(GroundCell))]
        [BurstCompile]
        public partial struct GroundCellUpdate : IJobEntity
        {
            public float ElapsedTime;
            public Config Config;
            public float MinY;
            public float MaxY;

            public void Execute(ref LocalTransform trans, ref URPMaterialPropertyBaseColor color, [EntityIndexInQuery] int entityIdx)
            {
                var heat = SharedHeapMap.SharedValue.Data.heapmap[entityIdx];

                // oscillate the displayed heat so that the fire looks a little more organic
                {
                    var radians = Random.CreateFromIndex((uint)entityIdx).NextFloat(math.PI * 2) + ElapsedTime;
                    var oscillationOffset =
                        math.sin(radians) * heat * Config.HeatOscillationScale; // the more heat, the more oscillation
                    heat += oscillationOffset;
                }

                trans.Position.y = math.lerp(MinY, MaxY, heat);
                color.Value = math.lerp(Config.MinHeatColor, Config.MaxHeatColor, heat);

                entityIdx++;
            }
        }

        // douse a cell and all surrounding cells
        /*public static void DouseFire(float2 location, DynamicBuffer<Heat> heatBuffer, int numRows, int numCols)
        {
            int col = (int)location.x;
            int row = (int)location.y;

            DouseCell(row, col, heatBuffer, numRows, numCols);
            DouseCell(row, col + 1, heatBuffer, numRows, numCols);
            DouseCell(row, col - 1, heatBuffer, numRows, numCols);
            DouseCell(row - 1, col, heatBuffer, numRows, numCols);
            DouseCell(row - 1, col + 1, heatBuffer, numRows, numCols);
            DouseCell(row - 1, col - 1, heatBuffer, numRows, numCols);
            DouseCell(row + 1, col, heatBuffer, numRows, numCols);
            DouseCell(row + 1, col + 1, heatBuffer, numRows, numCols);
            DouseCell(row + 1, col - 1, heatBuffer, numRows, numCols);
        }

        private static void DouseCell(int row, int col, DynamicBuffer<Heat> heatBuffer, int numRows, int numCols)
        {
            if (col < 0 || col >= numCols ||
                row < 0 || row >= numRows)
            {
                return;
            }

            heatBuffer[row * numCols + col] = new Heat { Value = 0 };
        }

        public static float2 NearestFire(float2 location, DynamicBuffer<Heat> heatBuffer, int numRows, int numCols, float minHeat)
        {
            var closestFirePos = new float2(0.5f, 0.5f);
            var closestDistSq = float.MaxValue;

            // check every cell
            for (int col = 0; col < numCols; col++)
            {
                for (int row = 0; row < numRows; row++)
                {
                    if (heatBuffer[row * numCols + col].Value > minHeat) // is cell on fire
                    {
                        var firePos = new float2(col + 0.5f, row + 0.5f);
                        var distSq = math.distancesq(location, firePos);

                        if (distSq < closestDistSq)
                        {
                            closestFirePos = firePos;
                            closestDistSq = distSq;
                        }
                    }
                }
            }

            return closestFirePos;
        }*/
    }
}