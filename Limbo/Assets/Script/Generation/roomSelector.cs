using System.Collections.Generic;
using UnityEngine;

public class RoomSelector : MonoBehaviour
{
    public enum RoomType
    {
        Hallway,
        //DeadEnd,
        //Treasure,
        //Boss
    }

    [Header("All Room Prefabs")]
    public GameObject[] roomPrefabs;

    [Header("Room Type Weights")]
    public Dictionary<RoomType, float> roomTypeWeights = new Dictionary<RoomType, float>()
    {
        { RoomType.Hallway, 1f },
        //{ RoomType.DeadEnd, 1f },
        //{ RoomType.Treasure, 1f },
        //{ RoomType.Boss, 1f }
    };

    /// <summary>
    /// Picks a RoomType based on the weights in roomTypeWeights.
    /// </summary>
    public RoomType PickRoomType()
    {
        float totalWeight = 0f;
        foreach (var pair in roomTypeWeights)
            totalWeight += pair.Value;

        float randomValue = Random.Range(0f, totalWeight);
        float currentSum = 0f;

        foreach (var pair in roomTypeWeights)
        {
            currentSum += pair.Value;
            if (randomValue <= currentSum)
                return pair.Key;
        }

        return RoomType.Hallway; // fallback
    }

    /// <summary>
    /// Picks a room prefab from all available, based on prefab weights in RoomMetadata.
    /// </summary>
    public GameObject PickRoom()
    {
        float totalWeight = 0f;

        foreach (GameObject prefab in roomPrefabs)
        {
            RoomMetadata meta = prefab.GetComponent<RoomMetadata>();
            if (meta != null)
                totalWeight += meta.spawnWeight;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentSum = 0f;

        foreach (GameObject prefab in roomPrefabs)
        {
            RoomMetadata meta = prefab.GetComponent<RoomMetadata>();
            if (meta != null)
            {
                currentSum += meta.spawnWeight;
                if (randomValue <= currentSum)
                    return prefab;
            }
        }

        return roomPrefabs[0]; // fallback
    }

    /// <summary>
    /// Picks a room prefab of a specific RoomType, based on prefab weights in RoomMetadata.
    /// </summary>
    public GameObject PickRoomOfType(RoomType type)
    {
        List<GameObject> filtered = new List<GameObject>();
        float totalWeight = 0f;

        foreach (GameObject prefab in roomPrefabs)
        {
            RoomMetadata meta = prefab.GetComponent<RoomMetadata>();
            if (meta != null && meta.roomType == type)
            {
                filtered.Add(prefab);
                totalWeight += meta.spawnWeight;
            }
        }

        if (filtered.Count == 0)
        {
            Debug.LogWarning("No prefabs found for room type: " + type);
            return null;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentSum = 0f;

        foreach (GameObject prefab in filtered)
        {
            RoomMetadata meta = prefab.GetComponent<RoomMetadata>();
            currentSum += meta.spawnWeight;

            if (randomValue <= currentSum)
                return prefab;
        }

        return filtered[0]; // fallback
    }
}
