using System.Collections.Generic;
using UnityEngine;

public class RecursiveDungeonGenerator : MonoBehaviour
{
    public GameObject wallFillPrefab;
    public int maxRooms = 20;
    public float roomCheckRadius = 4f;

    private int currentRoomCount = 1;
    private List<GameObject> spawnedRooms = new List<GameObject>();

    public Transform startingRoom;
    private RoomSelector roomSelector;

    void Start()
    {
        spawnedRooms.Add(startingRoom.gameObject);
        roomSelector = GetComponent<RoomSelector>();
        foreach (Door door in GetDoors(startingRoom.gameObject))
        {
            if (!door.isConnected)
                GenerateFromDoor(door);
        }

        SealUnconnectedDoors();
    }

    void GenerateFromDoor(Door door)
    {
        if (currentRoomCount >= maxRooms || door.isConnected)
            return;

        const int maxAttempts = 10;
        bool spawnedRoom = false;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            RoomSelector.RoomType roomType = roomSelector.PickRoomType();
            GameObject roomPrefab = roomSelector.PickRoomOfType(roomType);
            GameObject newRoom = Instantiate(roomPrefab);

            Door[] newRoomDoors = GetDoors(newRoom);
            if (newRoomDoors.Length == 0)
            {
                Destroy(newRoom);
                continue;
            }

            Door newRoomDoor = newRoomDoors[Random.Range(0, newRoomDoors.Length)];

            AlignDoors(newRoom, newRoomDoor.transform, door.transform);

            if (!RoomOverlaps(newRoom))
            {
                currentRoomCount++;
                spawnedRooms.Add(newRoom);

                // Mark doors as connected to prevent double connections
                door.isConnected = true;
                newRoomDoor.isConnected = true;

                // Recursively generate from all other doors on new room
                foreach (Door otherDoor in newRoomDoors)
                {
                    if (otherDoor != newRoomDoor && !otherDoor.isConnected)
                    {
                        GenerateFromDoor(otherDoor);
                    }
                }

                spawnedRoom = true;
                break;
            }
            else
            {
                Destroy(newRoom);
            }
        }

        if (!spawnedRoom)
        {
            GameObject wallFill = Instantiate(wallFillPrefab, door.transform.position, door.transform.rotation);
            wallFill.transform.SetParent(door.transform.parent);
            door.isConnected = true;
        }
    }

    Door[] GetDoors(GameObject room)
    {
        return room.GetComponentsInChildren<Door>();
    }

    void AlignDoors(GameObject room, Transform roomDoor, Transform targetDoor)
    {
        // Align rotation so roomDoor faces opposite of targetDoor
        room.transform.rotation = Quaternion.LookRotation(-targetDoor.forward, Vector3.up) * Quaternion.Inverse(Quaternion.LookRotation(roomDoor.forward, Vector3.up));

        // Align position so roomDoor coincides with targetDoor
        Vector3 offset = roomDoor.position - room.transform.position;
        room.transform.position = targetDoor.position - offset;
    }

    bool RoomOverlaps(GameObject room)
    {
        Collider[] hits = Physics.OverlapSphere(room.transform.position, roomCheckRadius);
        foreach (Collider col in hits)
        {
            if (spawnedRooms.Contains(col.gameObject))
                return true;
        }
        return false;
    }

    void SealUnconnectedDoors()
    {
        foreach (GameObject room in spawnedRooms)
        {
            Door[] doors = GetDoors(room);
            foreach (Door door in doors)
            {
                if (!door.isConnected)
                {
                    GameObject wallFill = Instantiate(wallFillPrefab, door.transform.position, door.transform.rotation);
                    wallFill.transform.SetParent(door.transform.parent);
                    door.isConnected = true;
                }
            }
        }
    }
}
