using FirefightersOptimized.Authorings;
using FirefightersOptimized.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Bot = FirefightersOptimized.Authorings.Bot;
using BotState = FirefightersOptimized.Authorings.BotState;
using Bucket = FirefightersOptimized.Authorings.Bucket;
using Config = FirefightersOptimized.Authorings.Config;
using Pond = FirefightersOptimized.Authorings.Pond;
using Random = Unity.Mathematics.Random;
using RepositionLine = FirefightersOptimized.Components.RepositionLine;
using Team = FirefightersOptimized.Components.Team;

namespace FirefightersOptimized.Systems
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(SpawnSystem))]
    public partial struct LineSystem : ISystem
    {
        private uint seed;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<RepositionLine>();
            state.RequireForUpdate<Team>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();
            //var rand = new Random(123 +
            //                      seed++); // seed is incremented to get different random values in different frames
            var rand = SystemAPI.GetSingletonRW<RandomSingleton>();
            var pondQuery = SystemAPI.QueryBuilder().WithAll<Pond, LocalTransform>().Build();
            var pondPositions = pondQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            
            var botQuery = SystemAPI.QueryBuilder().WithAll<Bot, TeamID>().Build();
            
            int numBotsPerTeam = config.NumPassersPerTeam + 1;
            foreach (var (team, respositionLineState,teamEntity) in SystemAPI.Query<RefRO<Team>, EnabledRefRW<RepositionLine>>().WithEntityAccess())
            {
                respositionLineState.ValueRW = false; // disable RepositionLine
                // set LinePos of the team's bots and set their bot state
                {
                    botQuery.SetSharedComponentFilter(new TeamID { team = teamEntity });
                    var members = botQuery.ToEntityArray(Allocator.Temp);
                    var randomPondPos = pondPositions[rand.ValueRW.random.NextInt(pondPositions.Length)].Position.xz;
                    var nearestFirePos = HeatSystem.NearestFire(randomPondPos, config.GroundNumRows, config.GroundNumColumns, config.HeatDouseTargetMin);

                    int douserIdx = members.Length / 2;
                    var vec = nearestFirePos - randomPondPos;
                    var vecNorm = math.normalize(vec);
                    var offsetVec = new float2(-vecNorm.y, vecNorm.x);
                    
                    int passerIdx = 0;
                    for (int i = 0; i < members.Length; i++)
                    {
                        var botEntity = members[i];
                        if (team.ValueRO.Douser.Equals(botEntity))
                        {
                            passerIdx++;
                            var ratio = (float) douserIdx / (douserIdx+1);
                            var offset = math.sin(math.lerp(0, math.PI, ratio)) * offsetVec * config.LineMaxOffset;
                            var pos = math.lerp(randomPondPos, nearestFirePos, ratio);
                            
                            var douser = SystemAPI.GetComponentRW<Bot>(botEntity);
                            douser.ValueRW.State = BotState.MOVE_TO_LINE;
                            douser.ValueRW.LinePos = pos + offset;
                            douser.ValueRW.TargetPos = nearestFirePos;
                        }
                        else if (team.ValueRO.Filler.Equals(botEntity))
                        {
                            var filler = SystemAPI.GetComponentRW<Bot>(botEntity);
                            filler.ValueRW.LinePos = randomPondPos;

                            var bucket = SystemAPI.GetComponentRW<Bucket>(team.ValueRO.Bucket);
                            if (bucket.ValueRO.CarryingBot == Entity.Null)
                            {
                                filler.ValueRW.TargetPos = SystemAPI.GetComponent<LocalTransform>(team.ValueRO.Bucket).Position.xz;
                                filler.ValueRW.State = BotState.MOVE_TO_BUCKET;
                            }
                            else
                                filler.ValueRW.State = BotState.MOVE_TO_LINE;
                        }
                        else
                        {
                            passerIdx++;
                            if (passerIdx < douserIdx)
                            {
                                int percent = passerIdx;
                                var ratio = (float) percent / (douserIdx+1);
                                var offset = math.sin(math.lerp(0, math.PI, ratio)) * offsetVec * config.LineMaxOffset;
                                var pos = math.lerp(randomPondPos, nearestFirePos, ratio);

                                var bot = SystemAPI.GetComponentRW<Bot>(botEntity);
                                bot.ValueRW.State = BotState.MOVE_TO_LINE;
                                bot.ValueRW.LinePos = pos + offset;
                            }
                            else
                            {
                                int percent = passerIdx - douserIdx;
                                var ratio = (float) (percent+1) / (douserIdx+1);
                                var offset = math.sin(math.lerp(math.PI, math.PI*2, ratio)) * offsetVec * config.LineMaxOffset;
                                var pos = math.lerp(nearestFirePos, randomPondPos, ratio);
                                
                                var otherBot = SystemAPI.GetComponentRW<Bot>(botEntity);
                                otherBot.ValueRW.State = BotState.MOVE_TO_LINE;
                                otherBot.ValueRW.LinePos = pos + offset;
                            }
                        }
                    }
                }
            }
        }
    }
}