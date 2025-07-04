using UnityEngine;
using Unity.Netcode;
using System;

public class FlashLightUseable : NetworkBehaviour, IUseableItem<ItemData>
{
    [SerializeField] private float batteryLevel;
    public event System.Action<ItemData> OnDataChanged;
    private bool isOn;
    [SerializeField] private Light spotLight;
    [SerializeField] private float batteryDrainRate;
    [SerializeField] private DetectEnemyInLights lightScript;
    private bool isDead;
    public ItemData itemData { private set; get; }
    private bool visuallyTurnedOff;
    public static event Action<bool> onFlashLightToggle;
    [SerializeField] private Sound3DSO flashlightSFX;
    public override void OnNetworkSpawn()
    {

    }
    void Start()
    {
        isOn = false;
        visuallyTurnedOff = false;
    }
    void Update()
    {
        if (IsServer)
        {
            HandleFlashLightLogic();
            HandleBatteryLife();
        }
        if (IsOwner && transform.parent != null)
        {
            transform.position = transform.parent.position;
            transform.rotation = transform.parent.rotation;
        }
    }
    public void InitializeData(ItemData data)
    {
        if (!IsServer) return;
        itemData = data;
        batteryLevel = data.itemCharge;
    }

    private void HandleFlashLightLogic()
    {
        if (isOn && !isDead && batteryLevel > 0f)
        {
            if (visuallyTurnedOff)
            {
                TurnOnRpc();
            }
            lightScript.TrackEnemiesInLight(true);
        }
        else
        {
            if (!visuallyTurnedOff)
            {
                TurnOffRpc();
            }

        }
    }
    [Rpc(SendTo.Everyone)]
    private void TurnOnRpc()
    {
        spotLight.enabled = true;
        onFlashLightToggle?.Invoke(true);
        visuallyTurnedOff = false;
    }
    [Rpc(SendTo.Everyone)]
    private void TurnOffRpc()
    {
        spotLight.enabled = false;
        onFlashLightToggle?.Invoke(false);
        visuallyTurnedOff = true;


    }

    private void HandleBatteryLife()
    {
        if (!IsServer || itemData.id == -1 || isDead || !isOn) return;

        batteryLevel -= batteryDrainRate * Time.deltaTime;

        if (batteryLevel <= 0f)
        {
            batteryLevel = 0f;
            AllSeeFlashlightDeadRpc();
        }

        var data = itemData;
        data.itemCharge = batteryLevel;
        itemData = data;

        OnDataChanged?.Invoke(itemData);
    }

    [Rpc(SendTo.Everyone)]
    private void AllSeeFlashlightDeadRpc()
    {
        isDead = true;
    }

    public void UseItem()
    {
        RequestServerToToggleRpc();
        AudioManager.Instance.Play3DSoundServerRpc(AudioManager.Instance.Get3DSoundFromList(flashlightSFX), transform.position, true, 1f, 1f, 10f, false, GetComponent<NetworkObject>());
    }
    public ItemData GetData()
    {
        return itemData;
    }

    [Rpc(SendTo.Server)]
    private void RequestServerToToggleRpc()
    {
        AllSeeFlashlightSwitchRpc();
        lightScript.enabled = isOn;
    }

    [Rpc(SendTo.Everyone)]
    private void AllSeeFlashlightSwitchRpc()
    {
        isOn = !isOn;
    }
}

