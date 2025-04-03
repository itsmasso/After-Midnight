using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;
using Unity.Netcode;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
	
	[SerializeField] private GameObject monster;
	[SerializeField] private GameObject MQMonsterPrefab;
	private List<NetworkObject> spawnedEnemies = new List<NetworkObject>();
	[SerializeField] private HouseMapGenerator houseMapGenerator;
	[SerializeField] private float minSpawnDistFromPlayers;
	[SerializeField] private float maxSpawnDistFromPlayers;

	void Start()
	{
		GameManager.Instance.onGameStart += SpawnStartEnemies;
	}
		
	private Vector3 GetPositionInRangeOfPlayers()
	{
		Vector3 bestPositionToSpawn = transform.position;
		float maxMinDistance = 0f;
		
		foreach(Vector3 hallwayPos in houseMapGenerator.GetHallwayList().ToList()){
			float minDistToPlayers = float.MaxValue;
			
			// Calculate the minimum distance from this hallway position to all players
			foreach(Transform player in GameManager.Instance.playerTransforms)
			{
				float dist = Vector3.Distance(hallwayPos, player.transform.position);
				if(dist < minDistToPlayers)
				{
					minDistToPlayers = dist;
				}
			}
			// If this position meets the minimum distance requirement, return it immediately
			if(minDistToPlayers >= minSpawnDistFromPlayers && minDistToPlayers <= maxSpawnDistFromPlayers)
			{
				return hallwayPos;
			}	
			// Otherwise, track the position that is farthest from the nearest player
			if(minDistToPlayers > maxMinDistance)
			{
				maxMinDistance = minDistToPlayers;
				bestPositionToSpawn = hallwayPos;
			}
		}
		Debug.Log("No valid positions");
		// If no valid position was found, return the farthest possible one
		return bestPositionToSpawn;
	}
	
	private void SpawnStartEnemies()
	{
		SpawnMonster(GetPositionInRangeOfPlayers());
		SpawnMannequinMonsters();
	}

	public void SpawnMonster(Vector3 position)
	{	
		GameObject monsterObject = Instantiate(monster, new Vector3(position.x, position.y + monster.GetComponent<FollowerEntity>().height/2, position.z), Quaternion.identity);
		
		monsterObject.GetComponent<NetworkObject>().Spawn(true);
		
	}

	/*****************************************************************
	* SpawnMannequinMonsters
	*****************************************************************
	* Author: Dylan Werelius
	*****************************************************************
	* Description:
		This function will spawn a mannequin monster in each of the
		rooms. It uses functions from the HouseMapGenerator to get
		the positions of all the rooms and then it creates instances
		of the MQMonster objects in each room.
	*****************************************************************/
	private void SpawnMannequinMonsters()
	{
		foreach(Vector3 roomPos in houseMapGenerator.GetRoomsList().ToList())
		{
			// Spawn the monster on the first floor
			if (roomPos.y == -8.00)
			{
				GameObject mannequinObject = Instantiate(MQMonsterPrefab, new Vector3(roomPos.x, roomPos.y + MQMonsterPrefab.GetComponent<CapsuleCollider>().height/16, roomPos.z), Quaternion.identity);
				mannequinObject.GetComponent<NetworkObject>().Spawn(true);
				Debug.Log("Mannequin Spawned at: " + roomPos);
			}
		}
	}
	
	public void DespawnAllEnemies()
	{
		foreach (var enemy in spawnedEnemies)
		{
			if (enemy != null && enemy.IsSpawned)
			{
				enemy.Despawn(true);
			}
		}
		spawnedEnemies.Clear();
		Debug.Log("All enemies despawned.");
	}

}
