using System;
using FirefightersOptimized.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FirefightersOptimized.Authorings
{
    public class BotAuthoring : MonoBehaviour
    {
        private class Baker : Baker<BotAuthoring>
        {
            public override void Bake(BotAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent<Bot>(entity);
                AddSharedComponent(entity, new TeamID {teamId = -1});
            }
        }
    }
    
    public struct Bot : IComponentData
    {
        public BotState State;
        public float2 TargetPos;   // Where the bot is moving to.
        public float2 LinePos;     // The bot's place in line where they stand when idle.
        public Entity NextBot;     // The next bot in line (will pass the bucket to this bot).
        public Entity Bucket;      // The bucket that the bot is carrying.

        public readonly bool IsMoving()
        {
            return !(State == BotState.IDLE
                     || State == BotState.CLAIM_BUCKET
                     || State == BotState.WAIT_IN_LINE
                     || State == BotState.FILL_BUCKET);
        }
    }
    
    public enum BotState : Byte
    {
        IDLE,
        CLAIM_BUCKET,
        MOVE_TO_BUCKET,
        FILL_BUCKET,
        PASS_BUCKET,
        DOUSE_FIRE,
        MOVE_TO_LINE,
        WAIT_IN_LINE,
    }
}