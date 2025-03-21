using UnityEngine;
using Unity.Netcode;
using Pathfinding;
using System.Linq;
using System.Collections;
using System.Diagnostics;

public enum LurkerState
{
    Roaming,
    Stalking,
    Prechase,
    Chasing
}
public class LurkerMonsterScript : NetworkBehaviour, IReactToPlayerGaze, IAffectedByLight
{
	[Header("Scriptable Object")]
	public MonsterScriptableObject lurkerScriptableObj;
	
	[Header("Pathfinder")]
	public FollowerEntity agent;
	
	[Header("Monster States")]
	[SerializeField] public NetworkVariable<LurkerState> networkState = new NetworkVariable<LurkerState>(LurkerState.Roaming);
	public LurkerBaseState currentState;
	public LurkerRoamState roamState = new LurkerRoamState();
	public LurkerStalkState stalkState = new LurkerStalkState();
	public LurkerPrechaseState preChaseState = new LurkerPrechaseState();
	public LurkerChaseState chaseState = new LurkerChaseState();
	
	[Header("Target Properties")]
	public Transform currentTarget;
	public Vector3 targetPosition;
	
	[Header("Roam Properties")]
	public float defaultStoppingDistance;
	public float roamSpeed;
	
	[Header("Stalking Properties")]
	public float chanceToStalkPlayer;
	public float targetStalkRange;
	public float maxStalkTime;
	public float stalkCooldown;
	public bool canStalk;
	public Coroutine stalkCooldownCoroutine;
	
	[Header("Prechase Properties")]
	public float pauseBeforeChasingDuration;
	
	[Header("Chase Properties")]
	[SerializeField] private float aggressionDistance;
	public float chasingStoppingDistance;
	public float stopChasingDistance; //monster stops chasing after a certain distance
	public float minimumChaseTime; 
	public LayerMask groundLayer;
	public LayerMask playerLayer;
	public LayerMask obstacleLayer;
	[Header("Attack Properties")]
	public float attackRange;
	public float attackCooldown;
	
	[Header("Light Properties")]
	public NetworkVariable<bool> inLight = new NetworkVariable<bool>(false);
	[SerializeField] private float inDarkSpeed;
	[SerializeField] private float inLightSpeed;

	[Header("Animation Properties")]
	public Animator anim;
	public LurkerAnimationManager animationManager;

	private void Start() {
		if(IsServer)
		{
			roamSpeed = lurkerScriptableObj.baseSpeed;
			canStalk = true;
			SwitchState(LurkerState.Roaming);
		}
	}


    public void SwitchState(LurkerState newState)
	{
		if(!IsServer) return;
		networkState.Value = newState;
	    switch(newState)
	    {
	        case LurkerState.Roaming:
	        	currentState = roamState;
	        	break;
	        case LurkerState.Stalking:
	        	currentState = stalkState;
	        	break;
	        case LurkerState.Prechase:
	        	currentState = preChaseState;
	        	break;
	        case LurkerState.Chasing:
	        	currentState = chaseState;
	        	break;   
	    }

	    currentState.EnterState(this);
	    
	}
	//add a check to see if mosnter spawns in stuck arena/unstuck function
	
	private void Update() {
		if(!IsServer) return;
		currentState.UpdateState(this);
		
		if(currentTarget != null)
			targetPosition = currentTarget.GetComponent<PlayerController>().GetPlayerGroundedPosition();
	
		if ((agent.velocity.magnitude < 0.1f || !agent.canMove || agent.reachedEndOfPath || !agent.hasPath) && networkState.Value != LurkerState.Prechase)
		{
			// Freeze animation if not moving
			animationManager.PlayIdleAnimation();
		}
		
	}
	
    public void ReactToPlayerGaze(NetworkObjectReference playerObjectRef)
	{
		ChaseTargetServerRpc(playerObjectRef);
	}
	
	[ServerRpc(RequireOwnership = false)]
	private void ChaseTargetServerRpc(NetworkObjectReference playerObjectRef)
	{
		playerObjectRef.TryGet(out NetworkObject playerObject);
		float distance = Vector2.Distance(playerObject.transform.position, transform.position);
		if(networkState.Value != LurkerState.Chasing && networkState.Value != LurkerState.Prechase && distance <= aggressionDistance)
		{
			SetCurrentTargetClientRpc(playerObjectRef);
			SwitchState(LurkerState.Prechase);
		}
	}
	
	public bool SetRandomPlayerAsTarget()
	{
	    if(GameManager.Instance.alivePlayers.Count != 0)
	    {
	        currentTarget = GameManager.Instance.alivePlayers[Random.Range(0, GameManager.Instance.alivePlayers.Count)];
	    	SetCurrentTargetClientRpc(currentTarget.GetComponent<NetworkObject>());
	    	return true;
	    }else
	    {
	        return false;
	    }
	}
	
	[ClientRpc]
	private void SetCurrentTargetClientRpc(NetworkObjectReference playerObjectRef)
	{
		playerObjectRef.TryGet(out NetworkObject playerObject);
	    currentTarget = playerObject.transform;
	}
	
	
	
	public float GetSpeed()
	{
	    return inLight.Value ? inLightSpeed : inDarkSpeed;
	}
	
	
	private IEnumerator StalkCooldown()
	{
		canStalk = false;
		yield return new WaitForSeconds(stalkCooldown);
		canStalk = true;
	}
	
	public void StartStalkCooldown()
	{
	     if(stalkCooldownCoroutine != null)
				StopCoroutine(stalkCooldownCoroutine);
		stalkCooldownCoroutine = StartCoroutine(StalkCooldown());
	}
	
	public void EnteredLight()
	{
		if(IsServer)
			inLight.Value = true;
	}

	public void ExitLight()
	{
		if(IsServer)
			inLight.Value = false;
	}

}
