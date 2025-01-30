using System;
using UnityEngine;

public class UnitManager : Singleton<UnitManager>
{
	[SerializeField] private GameObject playerPrefab;
	//change to spawn through scriptable objects later/change to allow and work with fishnet
	public void SpawnPlayer(Vector3 pos){
		Debug.Log("Player is being spawned from UnitManager.cs");
		GameObject player = Instantiate(playerPrefab, pos, Quaternion.identity);
	}
}
