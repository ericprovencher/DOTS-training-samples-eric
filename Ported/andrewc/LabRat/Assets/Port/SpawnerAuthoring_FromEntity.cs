using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[RequiresEntityConversion]
public class SpawnerAuthoring_FromEntity : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject RatPrefab;
    public float RatFrequency;
    public float RatMaxSpawn;
    public eDirection RatSpawnDirection;

    public GameObject CatPrefab;
    public float CatFrequency;
    public int   CatMaxSpawn;
    public eDirection CatSpawnDirection;

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(CatPrefab);
        referencedPrefabs.Add(RatPrefab);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var spawnerData = new Spawner_FromEntity
        {
            RatPrefab = conversionSystem.GetPrimaryEntity(RatPrefab),
            RatFrequency = RatFrequency,
            RatMaxSpawn = RatMaxSpawn,
            RatSpawnDirection = RatSpawnDirection,
            CatPrefab = conversionSystem.GetPrimaryEntity(CatPrefab),
            CatFrequency = CatFrequency,
            CatMaxSpawn = CatMaxSpawn,
            CatSpawnDirection = CatSpawnDirection,
            SpawnPos = transform.position,
            //runtime data
            RatSpawned = 0,
            CatSpawned = 0,
            RatCounter = 0,
            CatCounter = 0
        };

        dstManager.AddComponentData(entity, spawnerData);
    }
}