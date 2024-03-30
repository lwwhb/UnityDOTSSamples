using FirefightersOptimized.Authorings;
using FirefightersOptimized.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Config = FirefightersOptimized.Authorings.Config;

namespace FirefightersOptimized.Systems
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(LineSystem))]
    public partial struct BotSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();
            var dt = SystemAPI.Time.DeltaTime;
            var moveSpeed = dt * config.BotMoveSpeed;
            var fillRate = dt * config.BucketFillRate;

            BotUpdate_MainThread(ref state, moveSpeed, fillRate, config.GroundNumRows, config.GroundNumColumns);
        }
        
         private void BotUpdate_MainThread(ref SystemState state, float moveSpeed,
            float fillRate, int numRows,
            int numCols)
        {
            foreach (var (bot, botTrans, botEntity) in
                     SystemAPI.Query<RefRW<Bot>, RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                TeamID teamId = state.EntityManager.GetSharedComponent<TeamID>(botEntity);
                var team = SystemAPI.GetComponentRW<Team>(teamId.team);
                switch (bot.ValueRO.State)
                {
                    case BotState.MOVE_TO_BUCKET:
                    {
                        if (MoveToTarget(ref botTrans.ValueRW, bot.ValueRO.TargetPos, moveSpeed))
                        {
                            var bucket = SystemAPI.GetComponentRW<Bucket>(team.ValueRO.Bucket);
                            bucket.ValueRW.CarryingBot = botEntity;
                            
                            bot.ValueRW.Bucket = team.ValueRO.Bucket;
                            bot.ValueRW.TargetPos = bot.ValueRO.LinePos; // was set in TeamSystem
                            bot.ValueRW.State = BotState.MOVE_TO_LINE;
                        }

                        break;
                    }
                    case BotState.FILL_BUCKET:
                    {
                        var bucket = SystemAPI.GetComponentRW<Bucket>(bot.ValueRO.Bucket);
                        var val = fillRate + bucket.ValueRO.Water;
                        if (val < 1.0f) // keep filling
                        {
                            bucket.ValueRW.Water = fillRate + bucket.ValueRO.Water;
                        }
                        else // done filling
                        {
                            bucket.ValueRW.Water = 1;
                            bot.ValueRW.State = BotState.PASS_BUCKET;
                        }

                        break;
                    }
                    case BotState.DOUSE_FIRE:
                    {
                        // (only a douser should be put in this state)
                        if (MoveToTarget(ref botTrans.ValueRW, bot.ValueRO.TargetPos, moveSpeed))
                        {
                            var bucket = SystemAPI.GetComponentRW<Bucket>(bot.ValueRO.Bucket);
                            bucket.ValueRW.Water = 0;

                            HeatSystem.DouseFire(bot.ValueRO.TargetPos, numRows, numCols);

                            SystemAPI.SetComponentEnabled<RepositionLine>(teamId.team, true);
                            
                            team.ValueRW.NumFiresDoused++;

                            bot.ValueRW.State = BotState.MOVE_TO_LINE;
                        }

                        break;
                    }
                    case BotState.PASS_BUCKET:
                    {
                        // the next bot should generally not be moving while we're passing to it, but just in case, we get its current pos
                        var targetPos = SystemAPI.GetComponent<LocalTransform>(bot.ValueRO.NextBot).Position.xz;
                        if (MoveToTarget(ref botTrans.ValueRW, targetPos, moveSpeed))
                        {
                            var bucket = SystemAPI.GetComponentRW<Bucket>(bot.ValueRO.Bucket);
                            bucket.ValueRW.CarryingBot = bot.ValueRO.NextBot;

                            var otherBot = SystemAPI.GetComponentRW<Bot>(bot.ValueRO.NextBot);
                            otherBot.ValueRW.Bucket = bot.ValueRO.Bucket;
                            
                            bot.ValueRW.Bucket = Entity.Null;
                            bot.ValueRW.State = BotState.MOVE_TO_LINE;
                        }

                        break;
                    }
                    case BotState.MOVE_TO_LINE:
                    {
                        if (MoveToTarget(ref botTrans.ValueRW, bot.ValueRO.LinePos, moveSpeed))
                        {
                            bot.ValueRW.State = BotState.WAIT_IN_LINE;
                        }

                        break;
                    }
                    case BotState.WAIT_IN_LINE:
                    {
                        if (bot.ValueRO.Bucket == Entity.Null)
                        {
                            break;
                        }

                        if(team.ValueRO.Filler.Equals(botEntity))
                        {
                            var bucket = SystemAPI.GetComponentRW<Bucket>(bot.ValueRO.Bucket);
                            if (bucket.ValueRO.Water < 1)
                            {
                                bot.ValueRW.State = BotState.FILL_BUCKET;
                            }
                            else // full bucket
                            {
                                bot.ValueRW.State = BotState.PASS_BUCKET;
                            }

                            break;
                        }

                        if(team.ValueRO.Douser.Equals(botEntity))
                        {
                            var bucket = SystemAPI.GetComponentRW<Bucket>(bot.ValueRO.Bucket);
                            if (bucket.ValueRO.Water > 0)
                            {
                                bot.ValueRW.State = BotState.DOUSE_FIRE;
                            }
                            else // empty bucket
                            {
                                bot.ValueRW.State = BotState.PASS_BUCKET;
                            }

                            break;
                        }

                        bot.ValueRW.State = BotState.PASS_BUCKET;
                        break;
                    }
                }
            }
        }
         
        private static bool MoveToTarget(ref LocalTransform botTrans, float2 targetPos, float moveSpeed)
        {
            var pos = botTrans.Position;
            var dir = targetPos - pos.xz;
            var moveVectorNormalized = math.normalizesafe(dir);
            var moveVector = moveVectorNormalized * moveSpeed;
            
            // The the animated model faces up the z axis, so we need to rotate it 90 degrees clockwise.
            var modelRotation = math.radians(90);
            
            // atan2 returns a counter-clockwise angle of rotation, so we negate to make it clockwise
            var facingRotation = -math.atan2(moveVectorNormalized.y, moveVectorNormalized.x);   
            
            botTrans.Rotation = quaternion.RotateY(modelRotation + facingRotation);

            if (math.lengthsq(moveVector) >= math.lengthsq(dir))
            {
                botTrans.Position.x = targetPos.x;
                botTrans.Position.z = targetPos.y;
                return true;
            }

            botTrans.Position += new float3(moveVector.x, 0, moveVector.y);
            return false;
        }
    }
}