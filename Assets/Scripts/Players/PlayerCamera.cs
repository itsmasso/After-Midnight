
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Cinemachine;
using UnityEngine.Rendering.Universal;

public class PlayerCamera : NetworkBehaviour
{
	
	private PlayerController playerController;
	[SerializeField] private MeshRenderer playerMesh;
	[Header("Camera Properties")]
	private Transform mainCameraPosition;
	[SerializeField] private CinemachineCamera cmCam;
	[SerializeField] private CinemachinePanTilt cmCamPanTilt;
	[SerializeField] private Camera itemCamera;
	[SerializeField] private Transform camFollowPivot;
	
	[SerializeField] private Transform itemPivot;
	
	[Header("Head Sway")]
	private CinemachineBasicMultiChannelPerlin camNoiseChannel;
	[SerializeField] private float idleBobAmplitude = 0.2f, idleBobFrequency = 0.4f;
	
	[Header("Head Bob")]
	[SerializeField] private float walkBobSpeed;
	[SerializeField] private float walkBobAmount; //how much the camera moves
	[SerializeField] private float sprintBobSpeed;
	[SerializeField] private float sprintBobAmount; //how much the camera moves
	[SerializeField] private float crouchBobSpeed;
	[SerializeField] private float crouchBobAmount; //how much the camera moves
	[SerializeField] private Vector3 originalCamPos;
	private float bobbingTimer;
	private float movingTimer;	
	
	[Header("Player View")]
	[SerializeField] private LayerMask groundLayer;
	[SerializeField] private LayerMask enemyLayer;
	[SerializeField] private LayerMask obstacleLayer;
	[SerializeField] private float peripheralAngle; //max angle that determines how wide the field of view extends around the player. If angle is 90 degrees, it means the view is limited to 45 to the left and right
	
	void Start()
	{
		if(!IsOwner)
		{
			playerMesh.enabled = true;
			this.enabled = false;
		}else
		{
			cmCam = FindFirstObjectByType<CinemachineCamera>();
			cmCamPanTilt = cmCam.gameObject.GetComponent<CinemachinePanTilt>();
			cmCam.Follow = camFollowPivot;
			camNoiseChannel = cmCam.GetComponentInChildren<CinemachineBasicMultiChannelPerlin>();
			playerMesh.enabled = false;
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
			playerController = gameObject.GetComponent<PlayerController>();
			originalCamPos = camFollowPivot.localPosition;
			mainCameraPosition = Camera.main.transform;
			Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(itemCamera);
		}
		

	}


	private void StartHeadBob()
	{
		bobbingTimer += Time.deltaTime * (playerController.currentState == PlayerState.Crouching ? crouchBobSpeed : playerController.currentState == PlayerState.Running ? sprintBobSpeed : walkBobSpeed);
	
		float bobAmount = playerController.currentState == PlayerState.Crouching ? crouchBobAmount : playerController.currentState == PlayerState.Running ? sprintBobAmount : walkBobAmount;
		camFollowPivot.localPosition = new Vector3(
			camFollowPivot.localPosition.x + Mathf.Cos(bobbingTimer/2f) * bobAmount * 0.01f,
			camFollowPivot.localPosition.y + Mathf.Sin(bobbingTimer) * bobAmount,
			camFollowPivot.localPosition.z
		);
		
	}
	
	private void StopHeadBob()
	{
		if(camFollowPivot.localPosition == originalCamPos) return;
		camFollowPivot.localPosition = Vector3.Lerp(camFollowPivot.localPosition, originalCamPos, 1 * Time.deltaTime);
	}

	private void StopHeadSway()
	{
		if(camNoiseChannel.AmplitudeGain == 0 || camNoiseChannel.FrequencyGain == 0) return;
		camNoiseChannel.AmplitudeGain = Mathf.Lerp(camNoiseChannel.AmplitudeGain, 0, 1 * Time.deltaTime);
		camNoiseChannel.FrequencyGain = Mathf.Lerp(camNoiseChannel.FrequencyGain, 0, 1 * Time.deltaTime);
	}
	
	
	private void HeadBobbing()
	{
		
		if(camNoiseChannel != null)
		{	
			if(playerController.inputDir == Vector2.zero || !playerController.isGrounded)
			{
				movingTimer = 0;
				bobbingTimer = 0;
				StopHeadBob();
				camNoiseChannel.AmplitudeGain = idleBobAmplitude;
				camNoiseChannel.FrequencyGain = idleBobFrequency;

			}
			else
			{
				movingTimer += Time.deltaTime;
				//don't start bobbing until player has moved for a little bit. Prevents weird stutters when quickily tapping move key
				if(movingTimer >= 0.25f)
				{
					StartHeadBob();
					StopHeadSway();
				}
			}
		}

	}
	
	private void CheckForEnemyVisibility()
	{
		// Check if enemy in camera frustrum
		float renderDistance = Camera.main.farClipPlane;
		Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
		Collider[] enemyColliders = Physics.OverlapSphere(Camera.main.transform.position, renderDistance, enemyLayer);
		foreach(Collider enemyCollider in enemyColliders)
		{
			if(GeometryUtility.TestPlanesAABB(planes, enemyCollider.bounds))
			{
				// Check line of sight
				//Debug.Log("found enemies");
				Vector3 directionToEnemy = (enemyCollider.transform.position - Camera.main.transform.position).normalized;
				float enemyDist = Vector3.Distance(transform.position, enemyCollider.transform.position);
				Debug.DrawRay(Camera.main.transform.position, directionToEnemy * (enemyDist + 1), Color.red);
				// Check field of view
				float angle = Vector3.Angle(Camera.main.transform.forward, directionToEnemy);
				
				if (angle < peripheralAngle / 2) 
				{
					CheckForObstaclesBetweenEnemy(directionToEnemy, enemyDist);
				}
			}
		}
	}
	
	private void CheckForObstaclesBetweenEnemy(Vector3 directionToEnemy, float enemyDistance)
	{
		int obstacleLayers = obstacleLayer.value | groundLayer.value;
		if(Physics.Raycast(Camera.main.transform.position, directionToEnemy, out RaycastHit hit, enemyDistance + 1))
		{
			if(((1 << hit.collider.gameObject.layer) & enemyLayer) != 0 && ((1 << hit.collider.gameObject.layer) & obstacleLayers) == 0)
			{
				IReactToPlayerGaze reactableMonster = hit.collider.GetComponent<IReactToPlayerGaze>();
				if(reactableMonster != null)
				{
					//Debug.Log("Enemy Seen ");
					reactableMonster.ReactToPlayerGaze(GetComponent<NetworkObject>());

				}
			}		
		}	
	}
	
	void LateUpdate()
	{	
		if(cmCam != null)
		{
			Quaternion targetRotation = Quaternion.Euler(0, cmCamPanTilt.PanAxis.Value, 0);
			transform.rotation =  targetRotation;
			
			itemCamera.transform.rotation = mainCameraPosition.rotation;
			HeadBobbing();
		}

	}

	void Update()
	{
		CheckForEnemyVisibility();
	}

}
