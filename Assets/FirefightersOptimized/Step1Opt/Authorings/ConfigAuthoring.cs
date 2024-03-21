using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FirefightersOptimized.Authorings
{
    public class ConfigAuthoring : MonoBehaviour
    {
        [Header("Ponds")] 
        [Range(1, 120)]public int NumPondsPerEdge = 12;

        [Header("Bots")] 
        [Range(1, 30)]public int NumTeams = 1;
        [Range(4, 60)]public int NumPassersPerTeam = 6;
        [Range(1, 10)]public int BotMoveSpeed = 3; // units per second
        [Range(1, 10)]public float LineMaxOffset = 4;

        [Header("Buckets")] 
        [Range(0.1f,1.0f)]public float BucketFillRate = 0.2f;
        [Range(1, 150)]public int NumBuckets = 15;
        public Color BucketEmptyColor;
        public Color BucketFullColor;
        [Range(0.5f,0.8f)]public float BucketEmptyScale = 0.5f;
        [Range(1.2f,1.5f)]public float BucketFullScale = 1.5f;

        [Header("Ground")] 
        [Range(10, 200)]public int GroundNumColumns =30;
        [Range(10, 200)]public int GroundNumRows = 50;

        [Header("Heat")] 
        public Color MinHeatColor;
        public Color MaxHeatColor;
        [Range(0.01f,0.05f)]public float HeatSpreadSpeed = 0.03f;
        [Range(0.1f,0.5f)]public float HeatOscillationScale = 0.2f;
        [Range(1, 100)]public int NumInitialCellsOnFire = 1;
        [Range(0.3f, 1.0f)]public float HeatDouseTargetMin = 0.3f;

        [Header("Prefabs")] 
        public GameObject BotPrefab;
        public GameObject BucketPrefab;
        public GameObject PondPrefab;
        public GameObject GroundCellPrefab;
        public GameObject BotAnimatedPrefabGO;
        private class Baker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);
                AddComponent(entity, new Config
                {
                    GroundNumColumns = authoring.GroundNumColumns,
                    GroundNumRows = authoring.GroundNumRows,
                    NumPondsPerEdge = authoring.NumPondsPerEdge,
                    NumTeams = authoring.NumTeams,
                    NumPassersPerTeam = authoring.NumPassersPerTeam,
                    BotMoveSpeed = authoring.BotMoveSpeed,
                    LineMaxOffset = authoring.LineMaxOffset,
                    NumBuckets = math.max(authoring.NumBuckets, authoring.NumTeams), 
                    BucketFillRate = authoring.BucketFillRate,
                    MinHeatColor = (Vector4)authoring.MinHeatColor,
                    MaxHeatColor = (Vector4)authoring.MaxHeatColor,
                    BucketEmptyColor = (Vector4)authoring.BucketEmptyColor,
                    BucketFullColor = (Vector4)authoring.BucketFullColor,
                    BucketEmptyScale = authoring.BucketEmptyScale,
                    BucketFullScale = authoring.BucketFullScale,
                    NumInitialCellsOnFire = authoring.NumInitialCellsOnFire,
                    HeatSpreadSpeed = authoring.HeatSpreadSpeed,
                    HeatDouseTargetMin = authoring.HeatDouseTargetMin,
                    HeatOscillationScale = authoring.HeatOscillationScale,
                    GroundCellYScale = authoring.GroundCellPrefab.transform.localScale.y,
                    BotPrefab = GetEntity(authoring.BotPrefab, TransformUsageFlags.Dynamic),
                    BucketPrefab = GetEntity(authoring.BucketPrefab, TransformUsageFlags.Dynamic),
                    PondPrefab = GetEntity(authoring.PondPrefab, TransformUsageFlags.WorldSpace),
                    GroundCellPrefab = GetEntity(authoring.GroundCellPrefab, TransformUsageFlags.WorldSpace),
                });
                var configManaged = new ConfigManaged();
                configManaged.BotAnimatedPrefabGO = authoring.BotAnimatedPrefabGO;
                AddComponentObject(entity, configManaged);
            }
        }
    }
    public struct Config : IComponentData
    {
        public int GroundNumColumns;
        public int GroundNumRows;
        public int NumPondsPerEdge;
        public int NumTeams;
        public int NumPassersPerTeam;
        public int BotMoveSpeed;
        public float LineMaxOffset;
        public int NumBuckets;

        public float4 MinHeatColor;
        public float4 MaxHeatColor;
        public float HeatSpreadSpeed;
        public float HeatDouseTargetMin;
        public float HeatOscillationScale;
        public int NumInitialCellsOnFire;
        public float GroundCellYScale;

        public float4 BucketEmptyColor;
        public float4 BucketFullColor;
        public float BucketEmptyScale;
        public float BucketFullScale;
        public float BucketFillRate;

        public Entity BotPrefab;
        public Entity BucketPrefab;
        public Entity PondPrefab;
        public Entity GroundCellPrefab;
    }
    public class ConfigManaged : IComponentData
    {
        public GameObject BotAnimatedPrefabGO;
        public UIController UIController;
    }
}