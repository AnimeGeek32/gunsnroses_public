using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Prime31;
//using InControl;
using Rewired;

/*
public class TestPlayerActions : PlayerActionSet
{
    public PlayerAction Left;
    public PlayerAction Right;
    public PlayerAction Down;
    public PlayerAction Jump;

    public PlayerOneAxisAction Move;

    public TestPlayerActions() {
        Left = CreatePlayerAction("Move Left");
        Right = CreatePlayerAction("Move Right");
        Down = CreatePlayerAction("Drop Down");
        Jump = CreatePlayerAction("Jump");

        Move = CreateOneAxisPlayerAction(Left, Right);
    }
}
*/

public class TestMultiplayerController : MonoBehaviour {
	// movement config
	public float gravity = -25f;
	public float runSpeed = 8f;
	public float groundDamping = 20f; // how fast do we change direction? higher means faster
	public float inAirDamping = 5f;
	public float jumpHeight = 3f;

    // For player controller ID (range: 0 or 1);
    public int playerId = 0;

	[HideInInspector]
	private float normalizedHorizontalSpeed = 0;

	private CharacterController2D _controller;
	private Animator _animator;
	private RaycastHit2D _lastControllerColliderHit;
	private Vector3 _velocity;
    //private TestPlayerActions _characterActions;
    private Player _player;

	void Awake()
	{
		_animator = GetComponent<Animator>();
		_controller = GetComponent<CharacterController2D>();

		// listen to some events for illustration purposes
		_controller.onControllerCollidedEvent += onControllerCollider;
		_controller.onTriggerEnterEvent += onTriggerEnterEvent;
		_controller.onTriggerExitEvent += onTriggerExitEvent;
	}

	// Use this for initialization
	void Start () {
        _player = ReInput.players.GetPlayer(playerId);
	}

	#region Event Listeners

	void onControllerCollider(RaycastHit2D hit)
	{
		// bail out on plain old ground hits cause they arent very interesting
		if (hit.normal.y == 1f)
			return;

		// logs any collider hits if uncommented. it gets noisy so it is commented out for the demo
		//Debug.Log( "flags: " + _controller.collisionState + ", hit.normal: " + hit.normal );
	}


	void onTriggerEnterEvent(Collider2D col)
	{
		Debug.Log("onTriggerEnterEvent: " + col.gameObject.name);
	}


	void onTriggerExitEvent(Collider2D col)
	{
		Debug.Log("onTriggerExitEvent: " + col.gameObject.name);
	}

	#endregion

	// the Update loop contains a very simple example of moving the character around and controlling the animation
	void Update()
	{
        Vector2 tempPlayerMovement = Vector2.zero;
        tempPlayerMovement.x = _player.GetAxis("Move Horizontal");
        tempPlayerMovement.y = _player.GetAxis("Move Vertical");

		if (_controller.isGrounded)
			_velocity.y = 0;
        
		if (tempPlayerMovement.x > 0)
		{
			normalizedHorizontalSpeed = 1;
			if (transform.localScale.x < 0f)
				transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);

			if (_controller.isGrounded)
				_animator.Play(Animator.StringToHash("Run"));
		}
		else if (tempPlayerMovement.x < 0)
		{
			normalizedHorizontalSpeed = -1;
			if (transform.localScale.x > 0f)
				transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);

			if (_controller.isGrounded)
				_animator.Play(Animator.StringToHash("Run"));
		}
		else
		{
			normalizedHorizontalSpeed = 0;

			if (_controller.isGrounded)
				_animator.Play(Animator.StringToHash("Idle"));
		}


		// we can only jump whilst grounded
		if (_controller.isGrounded && tempPlayerMovement.y > 0)
		{
			_velocity.y = Mathf.Sqrt(2f * jumpHeight * -gravity);
			_animator.Play(Animator.StringToHash("Jump"));
		}


		// apply horizontal speed smoothing it. dont really do this with Lerp. Use SmoothDamp or something that provides more control
		var smoothedMovementFactor = _controller.isGrounded ? groundDamping : inAirDamping; // how fast do we change direction?
		_velocity.x = Mathf.Lerp(_velocity.x, normalizedHorizontalSpeed * runSpeed, Time.deltaTime * smoothedMovementFactor);

		// apply gravity before moving
		_velocity.y += gravity * Time.deltaTime;

		// if holding down bump up our movement amount and turn off one way platform detection for a frame.
		// this lets us jump down through one way platforms
		if (_controller.isGrounded && tempPlayerMovement.y < 0)
		{
			_velocity.y *= 3f;
			_controller.ignoreOneWayPlatformsThisFrame = true;
		}

		_controller.move(_velocity * Time.deltaTime);

		// grab our current _velocity to use as a base for all calculations
		_velocity = _controller.velocity;
	}
}
