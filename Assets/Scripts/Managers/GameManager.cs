using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;

public class GameManager : Singleton<GameManager>
{
	[SerializeField] private ProceduralRoomGeneration levelGenerator;
	void Start()
	{
		levelGenerator.Generate();
		//UnitManager.Instance.SpawnPlayer(levelGenerator.GetPlayerSpawnPosition());
	}

	// Update is called once per frame
	void Update()
	{
		
	}
}
