using UnityEngine;
using Unity.Netcode;
using Pathfinding;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public enum LurkerState
{
	Roaming,
	Stalking,
	Prechase,
	Chasing,
	Attacking
}
public class LurkerMonsterScript : NetworkBehaviour, IReactToPlayerGaze, IAffectedByLight, IAffectedByBear
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
	public LurkerAttackState attackState = new LurkerAttackState();
	[Header("Target Properties")]
	public Transform currentTarget;
	public Vector3 targetPosition;
	public Transform headTransform;


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
	public LayerMask doorLayer;

	[Header("Attack Properties")]
	public float attackRange;
	public float attackCooldown;
	public bool canAttack;
	private Coroutine attackCooldownCoroutine;
	public int damage;


	[Header("Light Properties")]
	public NetworkVariable<bool> inLight = new NetworkVariable<bool>(false);
	[SerializeField] private float inDarkSpeed;
	[SerializeField] private float inLightSpeed;
	private bool isInLightThisFrame = false;
	private bool wasInLightLastFrame = false;

	[Header("Animation Properties")]
	public Animator anim;
	public LurkerAnimationManager animationManager;
	[Header("Map Properties")]
	public HouseMapGenerator houseMapGenerator;
	[Header("SFX")]
	public Sound3DSO lurkerBreathingSFX;
	public Sound3DSO lurkerPreChaseSFX;
	public Sound3DSO lurkerChaseSFX;
	public Sound3DSO[] regularFootSteps;
	public Sound3DSO[] heavyFootSteps;
	[SerializeField] private AudioSource footStepAudioSource;
	public float walkingFootStepInterval = 1;
	private float footstepTimer;
	private void Start()
	{
		if (IsServer)
		{
			roamSpeed = lurkerScriptableObj.baseSpeed;
			canStalk = true;
			canAttack = true;
			SetDifficulty();
			SwitchState(LurkerState.Roaming);
			
		}
	}

	void OnEnable()
	{
		if (IsServer)
		{
			roamSpeed = lurkerScriptableObj.baseSpeed;
			canStalk = true;
			canAttack = true;

			SwitchState(LurkerState.Roaming);
		}
	}
	private void SetDifficulty()
	{
		int currentLevel = GameManager.Instance.GetCurrentDreamLayer();
		switch (currentLevel)
		{
			case 0:
				chanceToStalkPlayer = 0f;
				break;
			case 1:
				chanceToStalkPlayer = 5f;
				damage = 25;
				maxStalkTime = 20f;
				stalkCooldown = 30f;
				minimumChaseTime = 5f;
				break;
			case 2:
				chanceToStalkPlayer = 15f;
				damage = 50;
				maxStalkTime = 40f;
				stalkCooldown = 20f;
				minimumChaseTime = 9f;
				break;
			case 3:
				chanceToStalkPlayer = 25f;
				damage = 100;
				maxStalkTime = 50f;
				stalkCooldown = 10f;
				minimumChaseTime = 15f;
				break;
		}
	}


	public void SwitchState(LurkerState newState)
	{
		if (!IsServer) return;
		networkState.Value = newState;
		switch (newState)
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
			case LurkerState.Attacking:
				currentState = attackState;
				break;
		}

		currentState.EnterState(this);

	}
	//add a check to see if mosnter spawns in stuck arena/unstuck function

	private void Update()
	{
		if (!IsServer) return;

		// if (inLight.Value)
		// {
		// 	UnityEngine.Debug.Log("Lurker is in Light");
		// }
		// else
		// {
		// 	UnityEngine.Debug.Log("Lurker is NOT in Light");
		// }
		currentState.UpdateState(this);

		if (currentTarget != null)
		{
			targetPosition = currentTarget.GetComponent<PlayerController>().GetPlayerGroundedPosition();

		}

		if ((agent.velocity.magnitude < 0.1f || !agent.canMove || agent.reachedEndOfPath || !agent.hasPath) && networkState.Value != LurkerState.Prechase)
		{
			// Freeze animation if not moving
			animationManager.PlayIdleAnimation();
		}
		if (isInLightThisFrame && !wasInLightLastFrame)
		{
			EnteredLight();
		}

		if (!isInLightThisFrame && wasInLightLastFrame)
		{
			ExitLight();
		}

		wasInLightLastFrame = isInLightThisFrame;
		isInLightThisFrame = false;

	}
	public void ReactToPlayerGaze(NetworkObjectReference playerObjectRef)
	{
		RequestServerToChasePlayerRpc(playerObjectRef);
	}

	[Rpc(SendTo.Server)]
	private void RequestServerToChasePlayerRpc(NetworkObjectReference playerObjectRef)
	{
		if (canAttack)
		{
			playerObjectRef.TryGet(out NetworkObject playerObject);
			float distance = Vector2.Distance(playerObject.transform.position, transform.position);
			if (networkState.Value != LurkerState.Chasing && networkState.Value != LurkerState.Prechase && networkState.Value != LurkerState.Attacking && distance <= aggressionDistance)
			{
				//SetCurrentTargetRpc(playerObjectRef);
				currentTarget = playerObject.transform;
				SwitchState(LurkerState.Prechase);
			}
		}
	}

	public void SetRandomPlayerAsTarget()
	{
		if (!IsServer) return;
		NetworkObject playerNetworkObject = PlayerNetworkManager.Instance.GetRandomPlayer();
		if (playerNetworkObject != null && playerNetworkObject.IsSpawned)
		{
			currentTarget = playerNetworkObject.transform;
		}
		else
		{
			currentTarget = null;
			Debug.Log("Could not get player net obj");
		}
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
		if (stalkCooldownCoroutine != null)
			StopCoroutine(stalkCooldownCoroutine);
		stalkCooldownCoroutine = StartCoroutine(StalkCooldown());
	}

	private IEnumerator AttackCooldown()
	{
		canAttack = false;
		yield return new WaitForSeconds(attackCooldown);
		canAttack = true;
	}

	public void StartAttackCooldown()
	{
		if (attackCooldownCoroutine != null)
			StopCoroutine(attackCooldownCoroutine);
		attackCooldownCoroutine = StartCoroutine(AttackCooldown());
	}

	[Rpc(SendTo.Everyone)]
	public void AllObserveAnimationLockRpc(NetworkObjectReference networkObjectReference, float animationTime)
	{
		if (networkObjectReference.TryGet(out NetworkObject networkObject))
		{
			if (!networkObject.IsOwner) return;
			ILurkerJumpScare[] animationLocks = networkObject.GetComponents<ILurkerJumpScare>();
			foreach (var animationLock in animationLocks)
			{
				animationLock.ApplyAnimationLock(animationTime);
			}

		}

	}
	[Rpc(SendTo.Everyone)]
	public void AllObservePlayerRotateRpc(NetworkObjectReference networkObjectReference, float duration)
	{
		networkObjectReference.TryGet(out NetworkObject networkObject);
		if (!networkObject.IsOwner) return;
		StartCoroutine(SmoothRotateCoroutine(networkObjectReference, duration));
	}

	private IEnumerator SmoothRotateCoroutine(NetworkObjectReference networkObjectReference, float duration)
	{
		if (networkObjectReference.TryGet(out NetworkObject networkObject))
		{
			Transform playerTransform = networkObject.transform;
			if (playerTransform == null) yield break;

			// Body rotation setup
			Vector3 bodyDirection = transform.position - playerTransform.position;
			bodyDirection.y = 0;
			Quaternion startBodyRot = playerTransform.rotation;
			Quaternion endBodyRot = Quaternion.LookRotation(bodyDirection);

			// Camera rotation setup
			CinemachinePanTilt panTilt = networkObject.GetComponent<PlayerCamera>()?.GetPlayerCamRotation();
			Vector3 headDirection = headTransform.position - playerTransform.position;
			Quaternion endHeadRot = Quaternion.LookRotation(headDirection);
			float startPan = panTilt?.PanAxis.Value ?? 0f;
			float startTilt = panTilt?.TiltAxis.Value ?? 0f;
			float targetPan = endHeadRot.eulerAngles.y;
			float targetTilt = endHeadRot.eulerAngles.x;

			float elapsed = 0f;

			while (elapsed < duration)
			{
				float t = elapsed / duration;
				// Body rotation
				playerTransform.rotation = Quaternion.Slerp(startBodyRot, endBodyRot, t);

				// Camera rotation
				if (panTilt != null)
				{
					panTilt.PanAxis.Value = Mathf.LerpAngle(startPan, targetPan, t);
					panTilt.TiltAxis.Value = Mathf.LerpAngle(startTilt, targetTilt, t);
				}

				elapsed += Time.deltaTime;
				yield return null;
			}

			// Final set to ensure precision
			playerTransform.rotation = endBodyRot;
			if (panTilt != null)
			{
				panTilt.PanAxis.Value = targetPan;
				panTilt.TiltAxis.Value = targetTilt;
			}
		}

	}
	public void SetInLight(bool isInLight)
	{
		isInLightThisFrame = true;
	}

	public void EnteredLight()
	{
		if (IsServer)
			inLight.Value = true;
	}

	public void ExitLight()
	{
		if (IsServer)
			inLight.Value = false;
	}

	void OnDisable()
	{
		SwitchState(LurkerState.Roaming);
		currentTarget = null;
	}

	public void HandleNormalFootStepSFX()
	{
		footstepTimer -= Time.deltaTime;

		if (footstepTimer <= 0f)
		{
			PlayNormalFootstepRpc();

			float speed = GetSpeed();
			float interval = walkingFootStepInterval / Mathf.Max(speed, 0.1f); // prevent division by 0

			footstepTimer = Mathf.Max(interval, 0.2f);
		}
	}

	public void HandleHeavyFootStepSFX()
	{
		footstepTimer -= Time.deltaTime;

		if (footstepTimer <= 0f)
		{
			PlayHeavyFootstepRpc();

			float speed = GetSpeed();
			float interval = walkingFootStepInterval * 0.5f / Mathf.Max(speed, 0.1f); // prevent division by 0

			footstepTimer = Mathf.Max(interval, 0.2f);
		}
	}

	[Rpc(SendTo.Everyone)]
	public void PlayNormalFootstepRpc()
	{
		Sound3DSO footStep = regularFootSteps[Random.Range(0, regularFootSteps.Length)];
		footStepAudioSource.pitch = Random.Range(0.9f, 1.1f);
		footStepAudioSource.volume = Random.Range(Mathf.Clamp01(footStep.volume - 0.4f), footStep.volume);
		footStepAudioSource.minDistance = footStep.minDistance;
		footStepAudioSource.maxDistance = footStep.maxDistance;
		footStepAudioSource.outputAudioMixerGroup = footStep.audioMixerGroup;
		footStepAudioSource.PlayOneShot(footStep.clip, footStep.volume);

	}
	[Rpc(SendTo.Everyone)]
	public void PlayHeavyFootstepRpc()
	{
		Sound3DSO footStep = heavyFootSteps[Random.Range(0, heavyFootSteps.Length)];
		footStepAudioSource.pitch = Random.Range(0.9f, 1.1f);
		footStepAudioSource.volume = Random.Range(Mathf.Clamp01(footStep.volume - 0.4f), footStep.volume);
		footStepAudioSource.minDistance = footStep.minDistance;
		footStepAudioSource.maxDistance = footStep.maxDistance;
		footStepAudioSource.outputAudioMixerGroup = footStep.audioMixerGroup;
		footStepAudioSource.PlayOneShot(footStep.clip, footStep.volume);

	}

	public void ActivateBearItemEffect()
	{
		if (networkState.Value != LurkerState.Attacking)
		{
			StartStalkCooldown();
			StartAttackCooldown();
			AudioManager.Instance.Stop3DSoundServerRpc(AudioManager.Instance.Get3DSoundFromList(lurkerChaseSFX), 0.5f);
			SwitchState(LurkerState.Roaming);
		}
	}


}
