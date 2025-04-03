using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using Unity.Netcode.Components;

public enum PlayerState
{
	Walking,
	Running,
	Crouching,
	Hiding
}
public class PlayerController : NetworkBehaviour
{
 	public PlayerState currentState {get; private set;}

	[Header("Initialize")]
	[SerializeField] private CharacterController characterController;
	[SerializeField] private PlayerScriptable playerScriptableObj;
	
	[Header("Camera")]
	[SerializeField] private Transform camFollowPivot;
	private float targetCamHeight;
	private float currentCamHeight;
	private float cameraSmoothDampVelocity = 0f;
	

	[Header("Movement")]
	private float baseMoveSpeed;
	public float moveSpeed;
	public Vector2 inputDir; 
	private Vector3 smoothedDirection;
	private Vector3 playerVelocity;
	private Vector3 smoothDampVelocity = Vector3.zero;
	[SerializeField] private float moveSmoothTime;
	[SerializeField] private float gravity = -15f;

	[Header("Sprinting")]
	[SerializeField] private float addedSprintSpeed;
	private bool enabledSprinting;

	[Header("Crouching")]
	[SerializeField] private float standHeight;
	[SerializeField] private float crouchHeight;
	[SerializeField] private float crouchSpeedMultiplier;
	[SerializeField] private float crouchSmoothTime;
	private bool enabledCrouching;
	[Header("Jump")]
	[SerializeField] private float jumpHeight;
	[SerializeField] private float airResistanceMultiplier;
	[Header("Ground Check")]
	public Transform groundCheckTransform;
	[SerializeField] private LayerMask groundCheckLayer;
	public bool isGrounded;
	public float floorPosition;

	public override void OnNetworkSpawn()
	{

	}
	
	void Start()
	{
		if(!IsOwner)
		{
			gameObject.GetComponent<CharacterController>().enabled = false;
			gameObject.GetComponent<PlayerInput>().enabled = false;
			
		}else
		{
			
			baseMoveSpeed = playerScriptableObj.baseMovementSpeed;
			moveSpeed = baseMoveSpeed; //setting movespeed to default base speed
			targetCamHeight = standHeight;
			
			//setting booleans
			enabledCrouching = false;
			enabledSprinting = false;


			//initializing player character controller
			characterController = GetComponent<CharacterController>();
		}
		
		
	}
	

	public void OnMove(InputAction.CallbackContext ctx)
	{
		inputDir = ctx.ReadValue<Vector2>(); //getting the player's input values. example: input A returns (-1, 0)
	}
	
	public void OnCrouch(InputAction.CallbackContext ctx)
	{
		//later probably add a way to switch between toggle to crouch and hold to crouch (for now its toggle to crouch)
		if(ctx.performed && !enabledCrouching && isGrounded)
		{
			targetCamHeight = crouchHeight;
			enabledCrouching = true;
		}else if(ctx.performed && enabledCrouching && isGrounded)
		{
			targetCamHeight = standHeight;
			enabledCrouching = false;   
		}
	}

	public void OnSprint(InputAction.CallbackContext ctx)
	{
		//later probably add a limit to the spring and/or stamina bar
		if(ctx.performed && !enabledSprinting && isGrounded)
			enabledSprinting = true;
		
		if(ctx.canceled && enabledSprinting)
			enabledSprinting = false;
		
	}

	public void OnJump(InputAction.CallbackContext ctx)
	{
		if(isGrounded && ctx.performed)
		{
			playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
		}
	}
	
	private void CrouchFunctionality()
	{
		currentCamHeight = Mathf.SmoothDamp(currentCamHeight, targetCamHeight, ref cameraSmoothDampVelocity, crouchSmoothTime * Time.deltaTime);
		Vector3 newCamPosition = camFollowPivot.localPosition;
		newCamPosition.y = currentCamHeight;
		camFollowPivot.localPosition = newCamPosition;
	}

	public Vector3 GetPlayerGroundedPosition()
	{
		return new Vector3(transform.position.x, floorPosition, transform.position.z);
	}

	public Vector3 GetPlayerCameraPosition()
	{
		return camFollowPivot.position;
	}
	
	void Update()
	{
		if(IsOwner)
		{

			//checking to see if sphere collider is touching the ground to determine if player is grounded or not
			isGrounded = Physics.CheckSphere(groundCheckTransform.position, 0.25f, groundCheckLayer); 
			
			Vector3 moveDir = Camera.main.transform.right * inputDir.x + Camera.main.transform.forward * inputDir.y; 
			
			moveDir.y = 0;
			Vector3 targetDirection = moveDir.normalized; //normalizing movement direction to prevent diagonal direction from moving faster	
			smoothedDirection = Vector3.SmoothDamp(smoothedDirection, targetDirection, ref smoothDampVelocity, moveSmoothTime);
			// Apply gravity
			if(!isGrounded)
				playerVelocity.y += gravity * Time.deltaTime;
				
			characterController.Move(playerVelocity * Time.deltaTime);
		
			if(isGrounded)
				characterController.Move(smoothedDirection * moveSpeed * Time.deltaTime);	
			else
				characterController.Move(smoothedDirection * moveSpeed * airResistanceMultiplier * Time.deltaTime);	
	
			CrouchFunctionality();
			
			switch(currentState)
			{
				case PlayerState.Walking:
					moveSpeed = baseMoveSpeed;
					if(enabledSprinting)
						currentState = PlayerState.Running;
					else if(enabledCrouching)
					{
						currentState = PlayerState.Crouching;
					}
					break;
				case PlayerState.Running:
					moveSpeed = baseMoveSpeed + addedSprintSpeed;
					if(enabledCrouching)
					{
						enabledSprinting = false;
						targetCamHeight = crouchHeight;
						currentState = PlayerState.Crouching;
					}
					else if(!enabledSprinting)             
						currentState = PlayerState.Walking;
					break;
				case PlayerState.Crouching:
						
					moveSpeed = baseMoveSpeed * crouchSpeedMultiplier;
					if(enabledSprinting)
					{
						enabledCrouching = false;
						targetCamHeight = standHeight;
						currentState = PlayerState.Running;
					}
					else if(!enabledCrouching)
						currentState = PlayerState.Walking;
					break;
				case PlayerState.Hiding:
					break;
				default:
					break;
			}
			
			
		}
		
	}

    void FixedUpdate()
    {
    
        if(Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundCheckLayer))
        {
            floorPosition = hit.collider.transform.position.y;
        }
    }

}
