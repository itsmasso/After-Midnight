
using UnityEngine;
using UnityEngine.InputSystem;
using FishNet.Object;
using FishNet.Connection;
using Unity.Cinemachine;
using UnityEngine.Rendering.Universal;

public class PlayerCamera : NetworkBehaviour
{
	
	private PlayerController playerController;
	[Header("Camera Properties")]
	private Transform mainCameraPosition;
	private CinemachineCamera cmCam;
	[SerializeField] private Camera itemCamera;
	[SerializeField] private Transform camFollowPivot;
	private Vector3 camFollowPivotOriginalPos;
	[SerializeField] private Transform itemPivot;
	
	
	[Header("Head Bob")]
	private CinemachineBasicMultiChannelPerlin camNoiseChannel;
	[SerializeField] private bool enableHeadBob = true;
	[SerializeField] private float idleBobAmplitude = 0.2f, idleBobFrequency = 0.4f;
	[SerializeField, Range(0, 2f)] private float bobAmount = 0.02f; //amplitude
	[SerializeField, Range(0, 30)] private float frequency = 15f;
	[SerializeField, Range(10f, 100f)] private float smoothtime = 30.0f;
	[SerializeField, Range(5f, 20f)] private float verticalBobMultiplier = 10f;
	[SerializeField, Range(0.1f, 5f)] private float horizontalBobMultiplier = 0.7f;
	[SerializeField] private float headBobVelocityChangeScale;
	

	public override void OnStartClient()
	{
		base.OnStartClient();
		if(base.IsOwner)
		{
			cmCam = FindFirstObjectByType<CinemachineCamera>();
			camNoiseChannel = cmCam.GetComponentInChildren<CinemachineBasicMultiChannelPerlin>();
			
			if(cmCam != null)
			{
				cmCam.Follow = camFollowPivot;
			}
			
		}
		else
		{
			
			camFollowPivot.gameObject.SetActive(false);
			this.enabled = false;
		}
		
	}
	
	void Start()
	{
		Debug.Log("PlayerCamera.cs is being used");
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		playerController = gameObject.GetComponent<PlayerController>();
		camFollowPivotOriginalPos = camFollowPivot.localPosition;
		mainCameraPosition = Camera.main.transform;
		Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(itemCamera);

	}

	private void StartHeadBob()
	{
		Vector3 pos = Vector3.zero;
		float headBobSpeedFactor = Mathf.Clamp(Mathf.Pow(playerController.moveSpeed * headBobVelocityChangeScale, 0.75f), 0.1f, 2f); //Taking root of speed to make speed factor diminishing
		
		//Debug.Log(headBobSpeedFactor);
		pos.y += Mathf.Lerp(pos.y, Mathf.Sin(Time.time * (frequency * headBobSpeedFactor)) * bobAmount * verticalBobMultiplier * headBobSpeedFactor, smoothtime * Time.deltaTime);
		pos.x += Mathf.Lerp(pos.x, Mathf.Cos(Time.time * (frequency /2f * headBobSpeedFactor)) * bobAmount * horizontalBobMultiplier * headBobSpeedFactor, smoothtime * Time.deltaTime);
		
		camFollowPivot.localPosition += pos;
	}
	
	private void StopHeadBob()
	{
		if(camFollowPivot.localPosition == camFollowPivotOriginalPos) return;
		camFollowPivot.localPosition = Vector3.Lerp(camFollowPivot.localPosition, camFollowPivotOriginalPos, 1 * Time.deltaTime);
	}
	
	private void StopHeadSway()
	{
		if(camNoiseChannel.AmplitudeGain == 0 || camNoiseChannel.FrequencyGain == 0) return;
		camNoiseChannel.AmplitudeGain = Mathf.Lerp(camNoiseChannel.AmplitudeGain, 0, 1 * Time.deltaTime);
		camNoiseChannel.FrequencyGain = Mathf.Lerp(camNoiseChannel.FrequencyGain, 0, 1 * Time.deltaTime);
	}
	
	
	private void HeadBobbing()
	{
		StopHeadBob();
		if(camNoiseChannel != null)
		{	
			if(playerController.inputDir == Vector2.zero || !playerController.isGrounded)
			{
				
				camNoiseChannel.AmplitudeGain = idleBobAmplitude;
				camNoiseChannel.FrequencyGain = idleBobFrequency;

			}
			else
			{
				StartHeadBob();
				StopHeadSway();
			}
		}

	}
	
	void LateUpdate()
	{
		itemCamera.transform.rotation = mainCameraPosition.rotation;
		itemPivot.localRotation = mainCameraPosition.rotation;
		HeadBobbing();

	}

}
