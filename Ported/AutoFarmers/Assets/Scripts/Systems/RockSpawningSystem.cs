﻿using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace AutoFarmers
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(StoreSpawningSystem))]
    public class RockSpawningSystem : SystemBase
    {
        private EntityCommandBufferSystem m_CommandBufferSystem;
        private Random m_Random;
        private EntityQuery m_Query;

        // Set on spawners to make them go to sleep after init
        public struct RocksAreInitalizedTag : IComponentData
        {
        };

        protected override void OnCreate()
        {
            m_Random = new Random(0x1234567);
            m_CommandBufferSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
            RequireForUpdate(m_Query);
        }

        static bool IsAreaFree(DynamicBuffer<CellTypeElement> buffer, Grid grid, int2 position, int2 areaSize)
        {
            for (int j = 0; j < areaSize.y; j++)
            {
                for (int i = 0; i < areaSize.x; i++)
                {
                    int idx = grid.GetIndexFromCoords(position.x + i, position.y + j);
                    if ( buffer[idx].Value != CellType.Raw)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        protected override void OnUpdate()
        {
            var ecb = m_CommandBufferSystem.CreateCommandBuffer();
            var random = m_Random;

            var entity = GetSingletonEntity<Grid>();
            var typeBuffer = EntityManager.GetBuffer<CellTypeElement>(entity);
            var entityBuffer = EntityManager.GetBuffer<CellEntityElement>(entity);
            Grid grid = GetSingleton<Grid>();

            Entities
            .WithChangeFilter<RockSpawner>()
            .WithStoreEntityQueryInField(ref m_Query)
            .WithNone<RocksAreInitalizedTag>()
            .ForEach((Entity rockSpawner, in RockSpawner spawner) =>
            {
                for (int n = 0; n < spawner.NumRocks; n++)
                {
                    int2 size = new int2();
                    int2 position = new int2();

                    for (int tries =  0; tries < 100; tries++)
                    {
                        size = random.NextInt2(spawner.RandomSizeMin, spawner.RandomSizeMax);
                        position = random.NextInt2(new int2(0, 0), grid.Size - size);

                        if ( IsAreaFree(typeBuffer, grid, position, size))
                        {
                            break;
                        }
                    }

                    // Box position account for the position being the minimum but the model we use having the pivot at the center
                    var translation = new float3(position.x, 0.0f, position.y);
                    var scale = new float3(size.x, 1.0f, size.y);
                    translation += scale * 0.5f;

                    // Inset the box
                    scale.x -= 0.2f;
                    scale.z -= 0.2f;

                    var instance = ecb.Instantiate(spawner.RockPrefab);
                    ecb.SetComponent(instance, new Translation { Value = translation });
                    ecb.SetComponent(instance, new NonUniformScale { Value = scale });
                    ecb.SetComponent(instance, new CellPosition { Value = position });
                    ecb.SetComponent(instance, new CellSize { Value = size });

                    float health = random.NextFloat(spawner.minHeight, spawner.maxHeight);
                    ecb.SetComponent(instance, new Health { Value = health });
   
                    for (int j = 0; j < size.y; j++)
                    {
                        for (int i = 0; i < size.x; i++)
                        {
                            int idx = grid.GetIndexFromCoords(position.x + i, position.y + j);
                            // This write is kind of bad, but needed, and ok since it only happens during init
                            typeBuffer[idx] = new CellTypeElement { Value = CellType.Rock };
                            var cellEntity = entityBuffer[idx].Value;
                            ecb.SetComponent(cellEntity, new Cell { Type = CellType.Rock });
                            ecb.SetComponent(cellEntity, new CellOccupant { Value = instance });
                        }
                    }
                }

                ecb.AddComponent(rockSpawner, new RocksAreInitalizedTag());
            }).Run();

            m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}