using FirefightersOptimized.Authorings;
using FirefightersOptimized.Components;
using FirefightersOptimized.SharedStaticData;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;
namespace FirefightersOptimized.Systems
{
    public partial struct SpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            var config = SystemAPI.GetSingleton<Config>();
            var rand = new Random(123);
            
            var bucketEntities = new NativeArray<Entity>(config.NumBuckets, Allocator.Temp);
            // 生成桶
            {
                // struct components are returned and passed by value (as copies)!
                var bucketTransform = state.EntityManager.GetComponentData<LocalTransform>(config.BucketPrefab);
                bucketTransform.Position.y = (bucketTransform.Scale / 2); // will be same for every bucket

                for (int i = 0; i < config.NumBuckets; i++)
                {
                    var bucketEntity = state.EntityManager.Instantiate(config.BucketPrefab);
                    bucketEntities[i] = bucketEntity;

                    bucketTransform.Position.x = rand.NextFloat(0.5f, config.GroundNumColumns - 0.5f);
                    bucketTransform.Position.z = rand.NextFloat(0.5f, config.GroundNumRows - 0.5f);
                    bucketTransform.Scale = config.BucketEmptyScale;

                    state.EntityManager.SetComponentData(bucketEntity, bucketTransform);
                }
            }
            // 生成队伍
            {
                int numBotsPerTeam = config.NumPassersPerTeam + 1;
                int douserIdx = (config.NumPassersPerTeam / 2);
                for (int teamIdx = 0; teamIdx < config.NumTeams; teamIdx++)
                {
                    var teamEntity = state.EntityManager.CreateEntity();
                    var team = new Team
                    {
                        Id = teamIdx,
                        Bucket = bucketEntities[teamIdx],
                        NumFiresDoused = 0
                    };
                    state.EntityManager.AddSharedComponent(bucketEntities[teamIdx], new TeamID(){ team = teamEntity});
                    state.EntityManager.AddComponent<RepositionLine>(teamEntity);
                    
                    var teamColor = new float4(rand.NextFloat3(), 1);
                    var botEntities = new NativeArray<Entity>(numBotsPerTeam, Allocator.Temp);
                    // 生成队伍中的机器人
                    for (int botIdx = 0; botIdx < numBotsPerTeam; botIdx++)
                    {
                        var botEntity = state.EntityManager.Instantiate(config.BotPrefab);
                        botEntities[botIdx] = botEntity;
                        var x = rand.NextFloat(0.5f, config.GroundNumColumns - 0.5f);
                        var z = rand.NextFloat(0.5f, config.GroundNumRows - 0.5f);

                        state.EntityManager.SetComponentData(botEntity, LocalTransform.FromPosition(x, 1, z));
                        state.EntityManager.SetComponentData(botEntity, new URPMaterialPropertyBaseColor
                        {
                            Value = teamColor
                        });
                        state.EntityManager.AddSharedComponent(botEntity, new TeamID(){ team = teamEntity });
                        
                        if (botIdx == 0)
                        {
                            // 指定队伍的取水者
                            state.EntityManager.AddComponent<Filler>(botEntity);
                        }
                        else
                        {
                            //构建索引链
                            Entity preBot = botEntities[botIdx - 1];
                            Entity currentBot = botEntities[botIdx];
                            if (botIdx == numBotsPerTeam - 1)
                            {
                                state.EntityManager.SetComponentData(currentBot, new Bot
                                {
                                    NextBot = botEntities[0]
                                });
                                state.EntityManager.SetComponentData(preBot, new Bot
                                {
                                    NextBot = currentBot
                                });
                            }
                            else
                            {
                                state.EntityManager.SetComponentData(preBot, new Bot
                                {
                                    NextBot = currentBot
                                });
                            }
                            
                        }

                        //指定灭火者
                        if(botIdx == douserIdx)
                        {
                            state.EntityManager.AddComponent<Douser>(botEntity);
                        }
                    }
                    state.EntityManager.AddComponentData(teamEntity, team);
                }
            }
            
            // 生成水塘
            {
                var bounds = new NativeArray<float4>(4, Allocator.Temp);

                const float innerMargin = 2; // margin between ground edge and pond area
                const float outerMargin = innerMargin + 3;
                float width = config.GroundNumColumns;
                float height = config.GroundNumRows;

                // 4 sides around the field of ground cells
                // x, y is bottom-left corner; z, w is top-right corner
                bounds[0] = new float4(0.5f, -outerMargin, width - 0.5f, -innerMargin); // bottom
                bounds[1] = new float4(0.5f, height + innerMargin, width - 0.5f, height + outerMargin); // top
                bounds[2] = new float4(-outerMargin, 0.5f, -innerMargin, height - 0.5f); // left
                bounds[3] = new float4(width + innerMargin, 0.5f, width + outerMargin, height - 0.5f); // right

                var pondTransform = state.EntityManager.GetComponentData<LocalTransform>(config.PondPrefab);
                for (int i = 0; i < 4; i++)
                {
                    var bottomLeft = bounds[i].xy;
                    var topRight = bounds[i].zw;

                    for (int j = 0; j < config.NumPondsPerEdge; j++)
                    {
                        var pondEntity = state.EntityManager.Instantiate(config.PondPrefab);

                        var pos = rand.NextFloat2(bottomLeft, topRight);
                        pondTransform.Position = new float3(pos.x, 0, pos.y);
                        state.EntityManager.SetComponentData(pondEntity, pondTransform);
                    }
                }
            }
            
            // 生成场地
            {
                var groundCellTransform = state.EntityManager.GetComponentData<LocalTransform>(config.GroundCellPrefab);
                groundCellTransform.Position.y = -(config.GroundCellYScale / 2);

                for (int column = 0; column < config.GroundNumColumns; column++)
                {
                    for (int row = 0; row < config.GroundNumRows; row++)
                    {
                        var groundCellEntity = state.EntityManager.Instantiate(config.GroundCellPrefab);
                        groundCellTransform.Position.x = column + 0.5f;
                        groundCellTransform.Position.z = row + 0.5f;
                        state.EntityManager.SetComponentData(groundCellEntity, groundCellTransform);
                        state.EntityManager.SetComponentData(groundCellEntity, new URPMaterialPropertyBaseColor
                        {
                            Value = config.MinHeatColor
                        });
                    }
                }
            }
            
            // 生成热力图
            {
                int groundNum = config.GroundNumColumns * config.GroundNumRows;
                SharedHeapMap.SharedValue.Data = new SharedHeapMap(config.GroundNumColumns * config.GroundNumRows);
                for (int i = 0; i < config.NumInitialCellsOnFire; i++)
                {
                    var randomIdx = rand.NextInt(0, groundNum);
                    SharedHeapMap.SharedValue.Data.heapmap[randomIdx] = 1.0f;
                }
            }
            
            // 设置地面位置
            {
                var x = 0;
                var z = 0;

                foreach (var trans in
                         SystemAPI.Query<RefRW<LocalTransform>>()
                             .WithAll<GroundCell>())
                {
                    trans.ValueRW.Position.x = x + 0.5f;
                    trans.ValueRW.Position.z = z + 0.5f;

                    x++;
                    if (x >= config.GroundNumColumns)
                    {
                        x = 0;
                        z++;
                    }
                }
            }
        }
    }
}