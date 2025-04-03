using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Steamworks;
using Unity.Netcode;
using Netcode.Transports.Facepunch;
using UnityEngine.SceneManagement;
using System;
using Unity.VisualScripting;


public enum GameState
{
	Lobby,
	GeneratingLevel,
	GameStart,
	GamePlaying,
	GameOver,
}

public class GameManager : NetworkSingleton<GameManager>
{
	public const int MAX_PLAYERS = 4;
	//events
	public event Action onGameStart;
	public event Action onGameStateChanged;
	public event Action onGamePlaying;
	
	[SerializeField]
	private GameObject playerPrefab;
	public List<Transform> playerTransforms = new List<Transform>();
	public List<Transform> alivePlayers = new List<Transform>();
	public NetworkVariable<int> seed = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	public NetworkVariable<int> spawnIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private GameState currentState;
	
	

	protected override void Awake()
	{
		base.Awake();
		DontDestroyOnLoad(this.gameObject);
	}

	public override void OnNetworkSpawn()
	{
		if(IsServer)
		{
			NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneLoaded;
		}
	}

	public void ChangeGameState(GameState newState)
	{
		if(IsServer)
		{
			onGameStateChanged?.Invoke();
			currentState = newState;
			switch(currentState)
			{
				case GameState.Lobby:
					seed.Value = UnityEngine.Random.Range(1, 999999);
					Debug.Log("Current seed:" + seed.Value);
					break;
				case GameState.GeneratingLevel:
					
					break;
				case GameState.GameStart:
					onGameStart?.Invoke();
					break;
				case GameState.GamePlaying:
					
					break;
				case GameState.GameOver:
					break;
				default:
					break;
			}
		}
	}
	
	

	private void SceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
	{
		ChangeGameState(GameState.GeneratingLevel);
	}
	
	public void SpawnPlayers(Vector3 position)
	{
		if(IsServer)
		{
			foreach(ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
			{
				GameObject player = Instantiate(playerPrefab, position, Quaternion.identity);
				NetworkObject playerNetworkObject = player.GetComponent<NetworkObject>();
				playerNetworkObject.SpawnAsPlayerObject(clientId, true);	
				playerTransforms.Add(playerNetworkObject.transform);
				alivePlayers.Add(playerNetworkObject.transform);
				spawnIndex.Value = (spawnIndex.Value + 1) % MAX_PLAYERS;
				Debug.Log($"Spawned player {clientId} at {playerNetworkObject.transform.position}");
			}
		}
	}

	public void RemovePlayersFromAliveList(Transform player)
	{
		if(IsServer)
		{
			alivePlayers.RemoveAll(p => p == player);
		}
	}
	

	
	
	




}