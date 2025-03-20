using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
	public Vector3Int position;
	public Vector3Int size;
	public bool isStairs;
	public bool isMainRoom;
	public List<Transform> doorNode;
	public Transform lightsTransform;
	
	public GameObject roomPrefab;
	
	public Vector3Int Min => position;
	public Vector3Int Max => position + size;
	
	public Vector3 Center
	{
		get
		{
			return position + size / 2;
		}
	}
	
	public bool PositionInBounds(Vector3 pos)
	{
		int posFloor = Mathf.FloorToInt(pos.y / size.y);
		int boxFloor = Mathf.FloorToInt(position.y / size.y);

		// Only return true if they're on the same floor
		if (Mathf.Abs(posFloor - boxFloor) > 1) return false;
    
	    return pos.x >= position.x && pos.x <= position.x + size.x &&
            pos.y >= position.y && pos.y <= position.y + size.y &&
            pos.z >= position.z && pos.z <= position.z + size.z;

	}
	
}
