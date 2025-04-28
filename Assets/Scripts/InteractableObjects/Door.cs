using UnityEngine;
using Unity.Netcode;
using Pathfinding;
using System.Collections.Generic;
public class Door : NetworkBehaviour, IInteractable
{
    [SerializeField] private float targetYRotation;
    [SerializeField] private float smoothTime;
    public float defaultYRotation;
    [SerializeField] private Transform pivot;
    [SerializeField] private float maxDoorAngle;
    [SerializeField] private bool isOpen;
    public bool isLocked;
    private NetworkVariable<bool> doorIsServerLocked = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private ItemScriptableObject requiredKeySO;
    [SerializeField] private ItemScriptableObject lockpickSO;
    [SerializeField] private Sounds3DSOList soundsSOList;
    [SerializeField] private Sound3DSO doorOpenSO, doorCloseSO, unlockSO;
    [SerializeField] private Transform soundTransform;


    void Start()
    {
        isOpen = false;
        if (IsServer)
        {
            doorIsServerLocked.Value = isLocked;
        }

        defaultYRotation = pivot.eulerAngles.y;
    }

    void Update()
    {
        pivot.rotation = Quaternion.Lerp(pivot.rotation, Quaternion.Euler(0f, defaultYRotation + targetYRotation, 0f), smoothTime * Time.deltaTime);
    }

    [Rpc(SendTo.Server)]
    private void RequestServerToToggleDoorRpc(Vector3 pos)
    {
        AllObserveToggleDoorRpc(pos);
    }

    [Rpc(SendTo.Everyone)]
    private void AllObserveToggleDoorRpc(Vector3 pos)
    {
        ToggleDoor(pos);
    }
    private void ToggleDoor(Vector3 pos)
    {

        isOpen = !isOpen;
        if (isOpen)
        {
            Vector3 dir = pos - transform.position;
            targetYRotation = -Mathf.Sign(Vector3.Dot(transform.right, dir)) * maxDoorAngle;
            if (IsOwner)
            {
                AudioManager.Instance.Play3DSoundServerRpc(Get3DSoundFromList(doorOpenSO), soundTransform.position, true, 1f, 1, 30f, false, GetComponent<NetworkObject>());
            }
      
        }
        else
        {
            if (IsOwner) AudioManager.Instance.Play3DSoundServerRpc(Get3DSoundFromList(doorCloseSO), soundTransform.position, true, 1f, 1, 30f,false, GetComponent<NetworkObject>());
            targetYRotation = 0f;
        }
    }

    public void OpenDoor(Vector3 pos)
    {
        RequestServerToOpenDoorRpc(pos);
    }

    [Rpc(SendTo.Server)]
    public void RequestServerToOpenDoorRpc(Vector3 pos)
    {
        AllObserveOpeningDoorRpc(pos);
    }

    [Rpc(SendTo.Everyone)]
    public void AllObserveOpeningDoorRpc(Vector3 pos)
    {
        if (!isOpen)
        {
            ToggleDoor(pos);
        }
    }

    public void CloseDoor(Vector3 pos)
    {
        RequestServerToCloseDoorRpc(pos);
    }

    [Rpc(SendTo.Server)]
    public void RequestServerToCloseDoorRpc(Vector3 pos)
    {
        AllObserveDoorClosingRpc(pos);
    }

    [Rpc(SendTo.Everyone)]
    public void AllObserveDoorClosingRpc(Vector3 pos)
    {
        if (isOpen)
        {
            ToggleDoor(pos);
        }
    }

    //for players
    public void Interact(NetworkObjectReference playerObjectRef)
    {
        if (playerObjectRef.TryGet(out NetworkObject playerObject))
        {
            if (!doorIsServerLocked.Value)
            {
                RequestServerToToggleDoorRpc(playerObject.transform.position);
            }
            else
            {
                if (playerObject.GetComponent<PlayerInventory>().currentHeldItemData.id != -1 &&
                     playerObject.GetComponent<PlayerInventory>().GetCurrentHeldItemID() == requiredKeySO.id)
                {
                    Debug.Log("Unlocked Door");
                    AudioManager.Instance.Play3DSoundServerRpc(AudioManager.Instance.Get3DSoundFromList(unlockSO), transform.position, true, 1f, 1, 25f, false, GetComponent<NetworkObject>(), 0f);
                    UnlockDoorServerRpc();
                    playerObject.GetComponent<PlayerInventory>().RequestServerToDestroyItemRpc();
                }
                else if (playerObject.GetComponent<PlayerInventory>().currentHeldItemData.id != -1 &&
                playerObject.GetComponent<PlayerInventory>().GetCurrentHeldItemID() == lockpickSO.id)
                {
                    float rand = Random.value;
                    if (rand <= 0.5f)//crochet needle (lock pick) has a 50 chance of opening any locked door
                    {
                        AudioManager.Instance.Play3DSoundServerRpc(AudioManager.Instance.Get3DSoundFromList(unlockSO), transform.position, true, 1f, 1, 25f, false, GetComponent<NetworkObject>(), 0f);
                        UnlockDoorServerRpc();
                    }
                    else
                    {
                        Debug.Log("Failed to lock pick door.");
                    }
                    playerObject.GetComponent<PlayerInventory>().RequestServerToDestroyItemRpc();
                }
                else
                {
                    Debug.Log("Door is locked");
                }

            }
        }
    }
    private int Get3DSoundFromList(Sound3DSO sound3DSO)
    {
        return soundsSOList.sound3DSOList.IndexOf(sound3DSO);
    }

    [Rpc(SendTo.Server)]
    private void UnlockDoorServerRpc()
    {
        doorIsServerLocked.Value = false;
    }
}
