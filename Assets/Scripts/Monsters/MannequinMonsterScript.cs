using UnityEngine;
using Unity.Netcode;
using Pathfinding;
using System.Linq;
using System.Collections;
using Unity.VisualScripting;

/*****************************************************************
 * MannequinMonsterScript
 *****************************************************************
 * Author: Dylan Werelius
 *****************************************************************
 * Description:
    The purpose of this script is to give actions to the mannequin
    monsters. This script will always be checking what state the
    monsters are in from the GFClockManager script. The mannequins
    will either be inactive, or they will be chasing the players.
    When they are in the AWAKENED state, they will lock onto the
    nearest player and start chasing them. If they are in the
    PASSIVE or ACTIVATING state, then they will not be moving. 
    Additionally, if they are seen by light, then they will stop
    moving.
    NOTE: THIS SCRIPT DOES NOT CONTROL THEIR STATE
 *****************************************************************/

public class MannequinMonsterScript : NetworkBehaviour, IAffectedByLight
{
    [Header("Initial Information")]
    // This provides a reference to the GFClockManager script which will tell me what state the monsters are in
    // See the GFClockManager script for more detaisl
    private GFClockManager manager;
    // This variable will give the monster the ability to track the players locations
    [SerializeField] private FollowerEntity agent;
    // This variable will control the threat level of the monsters
    // Because the threat level is actually controlled in the GFClockManager and just read here, I may need to add this network variable to GFClockManager.cs
    // Maybe, maybe not, but if we get a bug related, I would start by looking there
    [SerializeField] public NetworkVariable<MQThreatLevel> threatLevelNetworkState = new NetworkVariable<MQThreatLevel>(MQThreatLevel.PASSIVE);

    [Header("Target Properties")]
    private Transform currentTarget;
    private Vector3 targetPosition;

    [Header("Attack Properties")]
    private const float ACTIVATING_MOVE_SPEED = 1f;
    private const float AWAKENED_MOVE_SPEED = 5f;
    private const float STOPPING_DISTANCE = 1.5f;
    private float attackRange;
    private float attackCooldown;

    [Header("Light Properties")]
    // This variable will ensure that the monster stops moving if it is in light
    public NetworkVariable<bool> inLight = new NetworkVariable<bool>(false);

    private void Start()
    {
        manager = GFClockManager.Instance;
        threatLevelNetworkState.Value = manager.GetMQThreatLevel();
        agent.stopDistance = STOPPING_DISTANCE;
    }
    private void Update()
    {
        // if there is a change in threatLevel, then update the variable
        if (threatLevelNetworkState.Value != manager.GetMQThreatLevel())
        {
            threatLevelNetworkState.Value = manager.GetMQThreatLevel();
        }

        // Select a target if there is no target already
        if (currentTarget != null)
        {
            targetPosition = currentTarget.GetComponent<PlayerController>().GetPlayerGroundedPosition();
        } else 
        {
            if (GameManager.Instance.alivePlayers.Count != 0)
            {
                // Sic the monster on a random player
                currentTarget = GameManager.Instance.alivePlayers[Random.Range(0, GameManager.Instance.alivePlayers.Count)];
                SetCurrentTargetClientRpc(currentTarget.GetComponent<NetworkObject>());
            }

            targetPosition = currentTarget.GetComponent<PlayerController>().GetPlayerGroundedPosition();
        }

        // Basics of Movement
        switch(threatLevelNetworkState.Value)
        {
            case MQThreatLevel.PASSIVE:
                // Debug.Log("Mannequin Threat Level is now: " + threatLevelNetworkState.Value);
                // No moving for you
                agent.canMove = false;
                break;
            case MQThreatLevel.ACTIVATING:
                // Debug.Log("Mannequin Threat Level is now: " + threatLevelNetworkState.Value);
                // Chase currentTarget at ACTIVATING_SPEED
                MoveToPlayer(ACTIVATING_MOVE_SPEED);
                break;
            case MQThreatLevel.AWAKENED:
                // Debug.Log("Mannequin Threat Level is now: " + threatLevelNetworkState.Value);
                // Chase currentTarget at AWAKENED_SPEED
                MoveToPlayer(AWAKENED_MOVE_SPEED);
                break;

            // This really shouldn't ever happen, but here it is just to be safe
            default:
                Debug.Log("ERROR: Somehow the Mannequin threat level is broken");
                agent.canMove = false;
                break;
        }
    }

    [ClientRpc]
    private void SetCurrentTargetClientRpc(NetworkObjectReference playerObjectRef)
    {
        playerObjectRef.TryGet(out NetworkObject playerObject);
        currentTarget = playerObject.transform;
    }
    private void MoveToPlayer(float speed)
    {
        agent.canMove = true;
        agent.maxSpeed = speed;
        agent.destination = targetPosition;
        agent.SearchPath();
    }
    public void EnteredLight() 
    {
        //Debug.Log("Mannequin is in the light");
        //agent.canMove = false;
    }
	public void ExitLight() 
    {
        // Debug.Log("Mannequin has left the light");
        if (threatLevelNetworkState.Value != MQThreatLevel.PASSIVE)
        {
            agent.canMove = true;
        }
    }

}
