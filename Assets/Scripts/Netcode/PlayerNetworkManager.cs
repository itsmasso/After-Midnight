using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public class PlayerNetworkManager : NetworkSingleton<PlayerNetworkManager>
{
	public const int MAX_PLAYERS = 4;
	private int currentSpectateIndex = 0;
	[SerializeField] private GameObject playerPrefab;
	public List<NetworkObject> alivePlayers = new List<NetworkObject>();
	public NetworkVariable<int> alivePlayersCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private Dictionary<ulong, List<ItemData>> savedInventories = new();
	public NetworkVariable<Vector3> spawnPosition = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	public NetworkVariable<int> spawnIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
	void Start()
	{
		if (IsServer)
		{
			//GameManager.Instance.onGameStart += SpawnPlayers;
			GameManager.Instance.onNextLevel += DespawnPlayers;
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
	}

	[Rpc(SendTo.Everyone)]
	public void RegisterPlayerClientRpc(NetworkObjectReference playerRef)
	{
		if (playerRef.TryGet(out NetworkObject player))
		{
			if (!alivePlayers.Contains(player))
			{
				alivePlayers.Add(player);
				if (IsServer) alivePlayersCount.Value++;
			}
		}
	}

	[Rpc(SendTo.Server)]
	public void UnregisterPlayerServerRpc(NetworkObjectReference playerRef)
	{
		alivePlayersCount.Value--;
		UnregisterPlayerClientRpc(playerRef);
	}

	[Rpc(SendTo.Everyone)]
	public void UnregisterPlayerClientRpc(NetworkObjectReference playerRef)
	{
		if (playerRef.TryGet(out NetworkObject player))
		{
			if (player.IsOwner)
			{
				//Debug.Log("Removing Player");
				alivePlayers.Remove(player);
				Debug.Log(alivePlayersCount.Value);
			}
		}
		// Debug.Log(alivePlayersCount.Value + " players remaining");
		// Debug.Log(alivePlayers.Count + " players in the alivePlayers list");
		if (IsServer && alivePlayersCount.Value <= 0)
		{
			Debug.Log("The last player has died, the game is over");
			alivePlayersCount.Value = 0;
			GameManager.Instance.ChangeGameState(GameState.GameOver);
		}
	}

	public NetworkObject GetRandomPlayer()
	{
		if (alivePlayers.Count > 0)
		{
			int index = UnityEngine.Random.Range(0, alivePlayers.Count);
			NetworkObject selectedPlayer = alivePlayers[index];

			if (selectedPlayer == null)
			{
				Debug.LogError($"Selected player at index {index} is null!");
			}
			else
			{
				Debug.Log($"Selected random player at index {index}: {selectedPlayer.name} (IsSpawned: {selectedPlayer.IsSpawned})");
			}

			return selectedPlayer;
		}
		else
		{
			Debug.LogError("No alive players, the game should have ended?");
			return null;
		}
	}

	public NetworkObject GetNextPlayerToSpectate(bool reset = false)
	{
		if (alivePlayers.Count == 0)
		{
			Debug.LogWarning("No alive players to spectate.");
			return null;
		}

		if (reset)
			currentSpectateIndex = 0;
		else
			currentSpectateIndex = (currentSpectateIndex + 1) % alivePlayers.Count;

		NetworkObject selectedPlayer = alivePlayers[currentSpectateIndex];

		if (selectedPlayer == null)
		{
			Debug.LogError($"Spectate player at index {currentSpectateIndex} is null!");
			return null;
		}

		Debug.Log($"Spectating player at index {currentSpectateIndex}: {selectedPlayer.name} (IsSpawned: {selectedPlayer.IsSpawned})");

		return selectedPlayer;
	}



	public void SpawnPlayers(Vector3 position)
	{
		if (IsServer)
		{
			spawnPosition.Value = position;
			foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
			{
				GameObject player = Instantiate(playerPrefab, DetermineSpawnPosition(position), Quaternion.identity);
				NetworkObject playerNetworkObject = player.GetComponent<NetworkObject>();
				playerNetworkObject.SpawnAsPlayerObject(clientId, true);
				RegisterPlayerClientRpc(playerNetworkObject);
				player.GetComponent<PlayerHealth>().ResetHealth();
				var inventory = LoadInventory(playerNetworkObject.OwnerClientId);
				player.GetComponent<PlayerInventory>().EnsureEmptyInventorySlots(4);
				foreach (var itemData in inventory)
				{
					player.GetComponent<PlayerInventory>().RepopulateItem(itemData);
				}
				player.GetComponent<PlayerInventory>().RequestServerToSelectNewItemRpc(0);
				Debug.Log($"Spawned player {clientId} at {playerNetworkObject.transform.position}");
			}
		}
	}
	public Vector3 DetermineSpawnPosition(Vector3 position)
	{
		Vector3[] offsets = new Vector3[]
		{
			new Vector3(2, 0, 0),
			new Vector3(-2, 0, 0),
			new Vector3(0, 0, 2),
			new Vector3(0, 0, -2)
		};

		Vector3 spawnPos = new Vector3(position.x + offsets[spawnIndex.Value].x, position.y + 2, position.z + offsets[spawnIndex.Value].z);
		IncrementSpawnIndexRpc();
		return spawnPos;

	}
	[Rpc(SendTo.Server)]
	private void IncrementSpawnIndexRpc()
	{
	    spawnIndex.Value = (spawnIndex.Value + 1) % MAX_PLAYERS;
	}

	public void DespawnPlayers()
	{
		if (IsServer)
		{
			foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
			{
				if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
				{
					NetworkObject playerObject = client.PlayerObject;

					if (playerObject != null)
					{
						SaveInventory(playerObject.OwnerClientId, playerObject.GetComponent<PlayerInventory>().SyncedInventory);
						playerObject.Despawn(true);
						ClearAlivePlayersClientRpc();
						Debug.Log($"Despawned {clientId}: {playerObject.name}");
					}
				}
			}
			alivePlayersCount.Value = 0;
		}
	}
	public void SaveInventory(ulong clientId, NetworkList<ItemData> inventoryList)
	{
		List<ItemData> copy = new List<ItemData>();
		foreach (var item in inventoryList)
		{
			copy.Add(item);
		}

		savedInventories[clientId] = copy;
		//Debug.Log($"[SaveInventory] Saved {copy.Count} items for clientId: {clientId}");
		foreach (var item in copy)
		{
			//Debug.Log($"[SaveInventory] Item saved - ID: {item.id}, UID: {item.uniqueId}, Charge: {item.itemCharge}, Uses: {item.usesRemaining}");
		}
	}

	public List<ItemData> LoadInventory(ulong clientId)
	{
		if (!savedInventories.ContainsKey(clientId))
		{
			//Debug.Log($"[LoadInventory] No saved inventory found for clientId: {clientId}, returning empty inventory.");
			ItemData emptyItem = new ItemData { id = -1, itemCharge = 0, usesRemaining = 0, uniqueId = -1 };
			List<ItemData> newEmptyInventory = new List<ItemData>();
			while (newEmptyInventory.Count < 4)
				newEmptyInventory.Add(emptyItem);
			return newEmptyInventory;
		}

		List<ItemData> copy = new List<ItemData>();
		foreach (var item in savedInventories[clientId])
		{
			copy.Add(item);
		}

		Debug.Log($"[LoadInventory] Loaded {copy.Count} items for clientId: {clientId}");
		foreach (var item in copy)
		{
			Debug.Log($"[LoadInventory] Item loaded - ID: {item.id}, UID: {item.uniqueId}, Charge: {item.itemCharge}, Uses: {item.usesRemaining}");
		}

		return copy;
	}
	public void ClearInventory(ulong clientId)
	{
		savedInventories.Remove(clientId); // Safe even if key doesn't exist
	}

	[ClientRpc]
	private void ClearAlivePlayersClientRpc()
	{
		alivePlayers.Clear();
	}



	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (IsServer)
		{
			//GameManager.Instance.onGameStart -= SpawnPlayers;
			GameManager.Instance.onNextLevel -= DespawnPlayers;
		}

	}

}
