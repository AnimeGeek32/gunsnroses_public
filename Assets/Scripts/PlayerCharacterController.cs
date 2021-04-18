using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_PS4
using Rewired.Platforms.PS4;
#endif
#if !UNITY_PS4
using UnityEngine.Analytics;
#endif
using Prime31;
using Rewired;
using GunsNRoses;

public enum PlayerCharacterState
{
    State_Start,
    State_Idle,
    State_Walk,
    State_Jump,
    State_Fall,
    State_Dash,
    State_Crouch,
    State_MeleeAttack,
    State_SpecialAttack,
    State_Kick,
    State_Block,
    State_CrouchBlock,
    State_Hurt,
    State_Down,
    State_Win
}



public class PlayerCharacterController : MonoBehaviour
{
    // movement config
    [Header("Movement Config")]
    public float gravity = -25f;
    public float walkSpeed = 4f;
    //public float runSpeed = 8f;
    public float groundDamping = 20f; // how fast do we change direction? higher means faster
    public float recoveryFromHurtTime = 1f;
    public float knockBackForce = 2f;
    public float dropDownForce = 3.5f;
    public float dropDownInSecs = 0.3f;
    public float startDelayTime = 1f;
    public float analogDeadZone = 0.4f;

    [Header("Jump Properties")]
    public float inAirDamping = 5f;
    public float jumpHeight = 3f;
    public int numberOfJumps = 2;
    public bool JumpIsProportionalToThePressTime = true;
    public float JumpMinimumAirTime = 0.1f;
    public float JumpAnalogDeadZone = 0.9f;
    public int NumberOfJumpsLeft { get; protected set; }

    [Header("Dash")]
    /// the duration of dash (in seconds)
    public float DashDistance = 3f;
    /// the force of the dash
    public float DashForce = 40f;
    /// the duration of the cooldown between 2 dashes (in seconds)
    public float DashCooldown = 1f;

    [Header("Melee Attack")]
    public bool enableMeleeAttack = false;
    // Melee attack trigger
    public BoxCollider2D meleeTrigger;
    public List<GameObject> meleeComboHurtParticles;
    public float meleeMovementForceInFrame = 2.0f;
    public int meleeAttackPower = 10;

    [Header("Special Attack")]
    public bool enableSpecialAttack = false;
    public Transform specialAttackSpawnPoint;
    public GameObject specialAttackPrefab;

    [Header("Kick")]
    public float kickDurationInSecs = 0.5f;

    [Header("Block")]
    public GameObject blockParticlesPrefab;
    public float blockKnockBackForce = 2.0f;
    public int blockDamage = 1;

    [Header("Hurt")]
    public GameObject hurtParticlesPrefab;

    // For player controller ID (range: 0 or 1);
    [Header("Controller Properties")]
    public int playerId = 0;

    // Animations names
    [Header("Animation Properties")]
    public string animationStartName = "Start";
    public string animationIdleName = "Idle";
    public string animationWalkName = "Walk";
    public string animationJumpName = "Jump";
    public string animationFallName = "Fall";
    public string animationDashName = "Dash";
    public string animationHurtName = "Hurt";
    public string animationCrouchName = "Crouch";
    public List<string> animationMeleeNames;
    public List<float> animationMeleeTimes;
    public List<string> animationSpecialAttackNames;
    public List<float> animationSpecialAttackTimes;
    public string animationBlockName = "Block";
    public string animationDownName = "Down";
    public string animationKickName = "Kick";
    public string animationCrouchBlockName = "CrouchBlock";

    [Header("FMOD Events")]
    [FMODUnity.EventRef]
    public string dashEvent;
    [FMODUnity.EventRef]
    public string gunEvent;
    [FMODUnity.EventRef]
    public string swordEvent;
    [FMODUnity.EventRef]
    public string dieEvent;
    [FMODUnity.EventRef]
    public string comboEvent;
    [FMODUnity.EventRef]
    public string meleeDamageEvent;
    [FMODUnity.EventRef]
    public string projectileDamageEvent;
	[FMODUnity.EventRef]
	public string kickDamageEvent;
    [FMODUnity.EventRef]
    public string kickEvent;
	[FMODUnity.EventRef]
	public string blockEvent;
    [FMODUnity.EventRef]
    public string blockProjectileEvent;

    [HideInInspector]
    private float normalizedHorizontalSpeed = 0;
    private bool _gravityActive = true;
    private PlayerCharacterState _currentState = PlayerCharacterState.State_Idle;
    private PlayerCharacterState _previousState = PlayerCharacterState.State_Idle;

    private float _cooldownTimeStamp = 0;
    private Vector3 _initialPosition;
    private float _dashDirection;
    private float _distanceTraveled = 0;
    private bool _shouldKeepDashing = true;
    private float _computedDashForce;
    private bool _isJumpButtonPressed = false;

    private float _calculatedKnockBackForce = 0;
    private float _computedMeleeMovementForce = 0f;

    private int _numOfMeleesQueued = 0;
    private int _numOfSpecialAttacksQueued = 0;

    private bool _isControllerVibrating = false;

    private CharacterController2D _controller;
    private Animator _animator;
    private RaycastHit2D _lastControllerColliderHit;
    private Vector3 _velocity;
    private Player _player;
    private CharacterSound _characterSound;



    void Awake()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController2D>();
        _characterSound = GetComponent<CharacterSound>();

        // listen to some events for illustration purposes
        _controller.onControllerCollidedEvent += onControllerCollider;
        _controller.onTriggerEnterEvent += onTriggerEnterEvent;
        _controller.onTriggerExitEvent += onTriggerExitEvent;

    }

    // Use this for initialization
    void Start()
    {
        _player = ReInput.players.GetPlayer(playerId);

        ResetNumberOfJumps();

        if (meleeTrigger != null)
            meleeTrigger.enabled = true;

        // Do start pose first
        InitiateStartPose();
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
        if (col.tag == "Attack")
        {
            AttackController attackController = col.GetComponent<AttackController>();
            ProjectileController projectileController = col.GetComponent<ProjectileController>();

            if (attackController.owner != this.transform)
            {
                //Debug.Log("attack's scale x: " + attackController.transform.localScale.x);
                float tempKnockBackForce = 0;
                
                //set attack damage to default
                int attackDamage = attackController.attackPower;

                if (projectileController != null)
                {
                    
                    tempKnockBackForce = (attackController.transform.localScale.x > 0) ? knockBackForce : -knockBackForce;

                    // If blocking
                    if (_currentState == PlayerCharacterState.State_Block || _currentState == PlayerCharacterState.State_CrouchBlock)
                    {

                        if ((IsFacingRight && (tempKnockBackForce < 0))
                            || (!IsFacingRight && (tempKnockBackForce > 0)))
                        {
                            Instantiate(blockParticlesPrefab, col.transform.position, Quaternion.identity);
                            attackDamage = blockDamage;
                            FMODUnity.RuntimeManager.PlayOneShot(blockProjectileEvent, transform.position);
                            Debug.Log("blocking from projectiles with damage:" + attackDamage);
                        }
                        else
                        {
                            FMODUnity.RuntimeManager.PlayOneShot(projectileDamageEvent, transform.position);
                        }

                    }
                    else
                    {
                        //NOTE: REPLACE IS HURT PARTICLE
                        GameObject hurtParticle = attackController.collisionParticle;
                        Instantiate(hurtParticle, col.transform.position, Quaternion.identity);
                        if (attackController.shakeCameraOnHit) {
                            Camera.main.GetComponent<MultiplayerCameraController>().StartCameraShake();
                        }

						FMODUnity.RuntimeManager.PlayOneShot(projectileDamageEvent, transform.position);
                    }

                    Destroy(col.gameObject);
                }
                else
                {
                    tempKnockBackForce = (attackController.transform.parent.localScale.x > 0) ? knockBackForce : -knockBackForce;
                    Vector3 tempBlockPosition = transform.position;
                    
                    // If blocking
                    if (_currentState == PlayerCharacterState.State_Block || _currentState == PlayerCharacterState.State_CrouchBlock)
                    {
						
                        if ((IsFacingRight && (tempKnockBackForce < 0))
                            || (!IsFacingRight && (tempKnockBackForce > 0)))
                        {
                            FMODUnity.RuntimeManager.PlayOneShot(blockEvent, transform.position);
                            tempBlockPosition.y += col.gameObject.GetComponent<BoxCollider2D>().offset.y;
                            Instantiate(blockParticlesPrefab, tempBlockPosition, Quaternion.identity);
                            attackDamage = blockDamage;
                             Debug.Log("blocking from melee with damage:" + attackDamage);
                        }
                        else
                        {
                            FMODUnity.RuntimeManager.PlayOneShot(meleeDamageEvent, transform.position);
                        }
                    }
                    else
                    {
                        Debug.Log("hit from melee");                        
                        //NOTE: REPLACE IS HURT PARTICLE
                        tempBlockPosition.y += col.gameObject.GetComponent<BoxCollider2D>().offset.y;
                        GameObject hurtParticle = attackController.collisionParticle;
                        Instantiate(hurtParticle, tempBlockPosition, Quaternion.identity);
                        if (attackController.shakeCameraOnHit)
                        {
                            Camera.main.GetComponent<MultiplayerCameraController>().StartCameraShake();
                        }

                        if (attackController.attackType == AttackType.TYPE_KICK)
                        {
                            FMODUnity.RuntimeManager.PlayOneShot(kickDamageEvent, transform.position);
                        }
                        else if (attackController.attackType == AttackType.TYPE_NORMALMELEE)
                        {
                            FMODUnity.RuntimeManager.PlayOneShot(meleeDamageEvent, transform.position);
                        }
                    }
                }
                Debug.Log("Call StartHurt with damage: " + attackDamage);
                StartHurt(attackDamage, tempKnockBackForce);
            }
        }
    }


    void onTriggerExitEvent(Collider2D col)
    {
        Debug.Log("onTriggerExitEvent: " + col.gameObject.name);
    }

    #endregion

    // Update is called once per frame
    void Update()
    {
        Vector2 tempPlayerMovement = Vector2.zero;
        tempPlayerMovement.x = _player.GetAxis("Move Horizontal");
        tempPlayerMovement.y = _player.GetAxis("Move Vertical");

        if (_player.GetButtonDown("Exit"))
        {
            GameManager.instance.LeaveGame();
        }

        if (GameManager.instance.GetPauseStatus)
        {
            if (_isControllerVibrating) {
                StopVibrate();
            }

            if (_player.GetButtonDown("Pause"))
            {
                GameManager.instance.TogglePause();
            }
        }
        else
        {
            if (_controller.isGrounded)
            {
                ResetNumberOfJumps();
                // Just landed
                if (!_controller.collisionState.wasGroundedLastFrame && _currentState != PlayerCharacterState.State_Start)
                {
                    _characterSound.OnLandSound();
                }
                _velocity.y = 0;
            }

            if (_currentState == PlayerCharacterState.State_Dash)
            {
                _gravityActive = false;
                _velocity.y = 0;
            }
            else if (_currentState == PlayerCharacterState.State_Hurt)
            {
                _velocity.x = _calculatedKnockBackForce;
            }
            else if (_currentState == PlayerCharacterState.State_MeleeAttack)
            {
                _velocity.x = _computedMeleeMovementForce;
                _computedMeleeMovementForce = 0f;

                // Melee attack
                if (enableMeleeAttack && _player.GetButtonDown("Melee Attack") && _controller.isGrounded)
                {
                    StartMeleeAttack();
                }
                else if (enableSpecialAttack && _player.GetButtonDown("Special Attack"))
				{
                    _currentState = PlayerCharacterState.State_SpecialAttack;
					StartSpecialAttack();
				}
				else if (_player.GetButtonDown("Dash"))
				{
					StartDash();
				}
				else if (_player.GetButtonDown("Block") && _controller.isGrounded)
				{
					_velocity.x = 0;
					StartBlock();
				}
				else if (tempPlayerMovement.y >= JumpAnalogDeadZone)
				{
					JumpStart();
				}
            }
            else if (_currentState == PlayerCharacterState.State_SpecialAttack)
            {
                _velocity.x = 0;

                // Special attack
                if (enableSpecialAttack && _player.GetButtonDown("Special Attack"))
                {
                    StartSpecialAttack();
                }
				else if (enableMeleeAttack && _player.GetButtonDown("Melee Attack") && _controller.isGrounded)
				{
					_currentState = PlayerCharacterState.State_MeleeAttack;
					StartMeleeAttack();
				}
				else if (_player.GetButtonDown("Dash"))
				{
					StartDash();
				}
				else if (_player.GetButtonDown("Block") && _controller.isGrounded)
				{
					_velocity.x = 0;
					StartBlock();
				}
				else if (tempPlayerMovement.y >= JumpAnalogDeadZone)
				{
					JumpStart();
				}
            }
            else if (_currentState == PlayerCharacterState.State_Block)
            {
                _velocity.x = _calculatedKnockBackForce;
                _calculatedKnockBackForce = 0.0f;

                if (_player.GetButtonUp("Block"))
                {
                    StopBlock();
                }
                else if (_controller.isGrounded && (tempPlayerMovement.y < -analogDeadZone))
                {
                    _velocity.y *= dropDownForce;
                    _controller.startTimedIgnoreOneWay(dropDownInSecs);
                    if (_controller.collisionState.below)
                    {
                        _velocity.x = 0f;
                        StartCrouchBlock();
                    }
                }
            }
            else if ((_currentState == PlayerCharacterState.State_Start) || (_currentState == PlayerCharacterState.State_Down) || (_currentState == PlayerCharacterState.State_Win))
            {
                _velocity.x = 0;

				if (_isControllerVibrating)
				{
					StopVibrate();
				}
            }
            else if (_currentState == PlayerCharacterState.State_Crouch)
            {
                if (tempPlayerMovement.y > -analogDeadZone && tempPlayerMovement.y < analogDeadZone)
                {
                    StopCrouch();
                }

                if (_velocity.y < 0)
                {
                    _animator.Play(Animator.StringToHash(animationFallName));
                    _currentState = PlayerCharacterState.State_Fall;
                }

                if (_player.GetButtonDown("Melee Attack"))
                {
                    StartKick();
                }
                else if (_player.GetButtonDown("Block") && _controller.isGrounded)
                {
                    StartCrouchBlock();
                }
            }
            else if (_currentState == PlayerCharacterState.State_CrouchBlock)
            {
                _velocity.x = _calculatedKnockBackForce;
                _calculatedKnockBackForce = 0.0f;

                if (_player.GetButtonUp("Block") && (tempPlayerMovement.y > -analogDeadZone && tempPlayerMovement.y < analogDeadZone))
                {
                    _currentState = PlayerCharacterState.State_Idle;
                }
                else if (_player.GetButtonUp("Block"))
                {
                    StartCrouch();
                }
                else if (tempPlayerMovement.y > -analogDeadZone && tempPlayerMovement.y < analogDeadZone)
                {
                    StartBlock();
                }
            }
            else if (_currentState == PlayerCharacterState.State_Kick)
            {
                _velocity.x = 0;


                if (tempPlayerMovement.y > -analogDeadZone && tempPlayerMovement.y < analogDeadZone)
                {
                    _previousState = PlayerCharacterState.State_Idle;
                }
            }
            else
            {
                if (tempPlayerMovement.x >= analogDeadZone)
                {
                    normalizedHorizontalSpeed = 1;
                    if (transform.localScale.x < 0f)
                        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);

                    if (_controller.isGrounded)
                    {
                        _animator.Play(Animator.StringToHash(animationWalkName));
                        _previousState = _currentState;
                        _currentState = PlayerCharacterState.State_Walk;
                    }
                }
                else if (tempPlayerMovement.x <= -analogDeadZone)
                {
                    normalizedHorizontalSpeed = -1;
                    if (transform.localScale.x > 0f)
                        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);

                    if (_controller.isGrounded)
                    {
                        _animator.Play(Animator.StringToHash(animationWalkName));
                        _previousState = _currentState;
                        _currentState = PlayerCharacterState.State_Walk;
                    }
                }
                else
                {
                    normalizedHorizontalSpeed = 0;

                    if (_controller.isGrounded)
                    {
                        _animator.Play(Animator.StringToHash(animationIdleName));
                        _previousState = _currentState;
                        _currentState = PlayerCharacterState.State_Idle;
                    }
                }

                // jump
                if (tempPlayerMovement.y >= JumpAnalogDeadZone)
                {
                    JumpStart();
                }
                else if (tempPlayerMovement.y < JumpAnalogDeadZone && tempPlayerMovement.y >= analogDeadZone)
                {
                    _isJumpButtonPressed = false;
                }
                else if (tempPlayerMovement.y < analogDeadZone && tempPlayerMovement.y > -analogDeadZone)
                {
                    JumpStop();
                }

                // dash
                if ((_player.GetButtonDown("Dash")) || (_player.GetButtonDoublePressDown("Move Horizontal")) || (_player.GetNegativeButtonDoublePressDown("Move Horizontal")))
                {
                    StartDash();
                }

                // Melee attack
                if (enableMeleeAttack && _player.GetButtonDown("Melee Attack"))
                {
                    Debug.Log("Melee attack while normal.");
                    _previousState = _currentState;
                    _currentState = PlayerCharacterState.State_MeleeAttack;
                    StartMeleeAttack();
                }
                else if (enableSpecialAttack && _player.GetButtonDown("Special Attack"))
                {
                    _previousState = _currentState;
                    _currentState = PlayerCharacterState.State_SpecialAttack;
                    StartSpecialAttack();
                }

                // Block
                if (_player.GetButtonDown("Block") && _controller.isGrounded)
                {
                    _velocity.x = 0;
                    StartBlock();
                }
                else if (_player.GetButtonUp("Block") && _controller.isGrounded)
                {
                    StopBlock();
                }

                // apply horizontal speed smoothing it. dont really do this with Lerp. Use SmoothDamp or something that provides more control
                var smoothedMovementFactor = _controller.isGrounded ? groundDamping : inAirDamping; // how fast do we change direction?
                _velocity.x = Mathf.Lerp(_velocity.x, normalizedHorizontalSpeed * walkSpeed, Time.deltaTime * smoothedMovementFactor);

                // if holding down bump up our movement amount and turn off one way platform detection for a frame.
                // this lets us jump down through one way platforms
                if (_controller.isGrounded && tempPlayerMovement.y < -analogDeadZone)
                {
                    _velocity.y *= dropDownForce;
                    //_controller.ignoreOneWayPlatformsThisFrame = true;
                    _controller.startTimedIgnoreOneWay(dropDownInSecs);
                    if (_controller.collisionState.below)
                    {
                        _velocity.x = 0f;
                        StartCrouch();
                    }
                }

                // Check if player's life is down to zero
                if ((GameManager.instance.player1LifeBar.playerLife <= 0.0f) && (playerId == 0))
                {
                    _previousState = _currentState;
                    _currentState = PlayerCharacterState.State_Down;
                    _animator.Play(Animator.StringToHash(animationDownName));
                }
                else if ((GameManager.instance.player2LifeBar.playerLife <= 0.0f) && (playerId == 1))
                {
                    _previousState = _currentState;
                    _currentState = PlayerCharacterState.State_Down;
                    _animator.Play(Animator.StringToHash(animationDownName));
                }
            }

            if (_gravityActive)
            {
                // apply gravity before moving
                _velocity.y += gravity * Time.deltaTime;
            }

            _controller.move(_velocity * Time.deltaTime);

            // grab our current _velocity to use as a base for all calculations
            _velocity = _controller.velocity;

            if ((_velocity.y < 0)
                && (_currentState != PlayerCharacterState.State_Down)
                && (_currentState != PlayerCharacterState.State_Start)
                && (_currentState != PlayerCharacterState.State_MeleeAttack)
                && (_currentState != PlayerCharacterState.State_SpecialAttack)
                && (_currentState != PlayerCharacterState.State_Hurt)
                && (_currentState != PlayerCharacterState.State_Dash)
                && (_currentState != PlayerCharacterState.State_Win)
                && (_currentState != PlayerCharacterState.State_Block)
                && (_currentState != PlayerCharacterState.State_Crouch)
                && (_currentState != PlayerCharacterState.State_Kick)
                && (_currentState != PlayerCharacterState.State_CrouchBlock))
            {
                _previousState = _currentState;
                _currentState = PlayerCharacterState.State_Fall;
            }

            if (_player.GetButtonDown("Pause"))
            {
                // Only pause on normal conditions
                if ((_currentState != PlayerCharacterState.State_Win)
                && (_currentState != PlayerCharacterState.State_Down)
                && (_currentState != PlayerCharacterState.State_Start))
                {
                    GameManager.instance.TogglePause();
                }
            }
        }
    }

    public void InitiateStartPose()
    {
        StartCoroutine(StartPose());
    }

    private IEnumerator StartPose()
    {
        _currentState = PlayerCharacterState.State_Start;
        _previousState = PlayerCharacterState.State_Start;

        _animator.Play(Animator.StringToHash(animationStartName));

        yield return new WaitForSeconds(startDelayTime);
        _currentState = PlayerCharacterState.State_Idle;
    }

    public void StartWinPose()
    {
        _previousState = PlayerCharacterState.State_Win;
        _currentState = PlayerCharacterState.State_Win;
        _animator.Play(Animator.StringToHash(animationStartName));
    }

    public void ResetNumberOfJumps()
    {
        NumberOfJumpsLeft = numberOfJumps;
    }

    public void JumpStart()
    {
        if (NumberOfJumpsLeft > 0 && !_isJumpButtonPressed)
        {
            // Decrease the number of jumps left
            NumberOfJumpsLeft = NumberOfJumpsLeft - 1;
            _velocity.y = Mathf.Sqrt(2f * jumpHeight * -gravity);
            _animator.Play(Animator.StringToHash(animationJumpName));
            _previousState = _currentState;
            _currentState = PlayerCharacterState.State_Jump;

            // Play jump sound
            _characterSound.OnJumpSound();

            _isJumpButtonPressed = true;

#if !UNITY_PS4
            //TODO: refactor analytics
            Analytics.CustomEvent("move", new Dictionary<string, object>
                 {{ "type", "jump"}}
                 );
#endif            
        }
    }

    public void JumpStop()
    {
        _isJumpButtonPressed = false;
        if (JumpIsProportionalToThePressTime && _velocity.y > 0)
        {
            //Debug.Log("Stopping jump. " + Time.time);
            _velocity.y = 0;
        }
    }

    public bool IsFacingRight
    {
        get
        {
            return (transform.localScale.x >= 0);
        }
    }

    public void StartHurt(int amount, float knockBackAmount)
    {
        if (_currentState == PlayerCharacterState.State_Hurt
            || _currentState == PlayerCharacterState.State_Down
            || _currentState == PlayerCharacterState.State_Start
            || _currentState == PlayerCharacterState.State_Win)
        {
            return;
        }

        if (_currentState == PlayerCharacterState.State_Block || _currentState == PlayerCharacterState.State_CrouchBlock)
        {
            if ((IsFacingRight && (knockBackAmount < 0))
                || (!IsFacingRight && (knockBackAmount > 0)))
            {
                _calculatedKnockBackForce = blockKnockBackForce * (IsFacingRight ? -1f : 1f);
                GunsNRoses.GameManager.instance.RemoveLife(transform.name, amount);                
                return;
            }
        }

        Debug.Log(transform.name + " gets hit with " + amount + "!");
        _calculatedKnockBackForce = knockBackAmount;
        Debug.Log("_calculatedKnockBack: " + _calculatedKnockBackForce);
        GunsNRoses.GameManager.instance.RemoveLife(transform.name, amount);

        _previousState = _currentState;
        _currentState = PlayerCharacterState.State_Hurt;
        StartCoroutine(Hurt());
    }

    protected IEnumerator Hurt()
    {
        StartVibrate(0.5f, 1.0f, recoveryFromHurtTime);
        _animator.Play(Animator.StringToHash(animationHurtName));

        yield return new WaitForSeconds(recoveryFromHurtTime);

        _calculatedKnockBackForce = 0f;

        //_currentState = _previousState;
        if ((_currentState == PlayerCharacterState.State_Win)
            || (_previousState == PlayerCharacterState.State_Win))
        {
            _currentState = PlayerCharacterState.State_Win;
        }
        else if ((GameManager.instance.player1LifeBar.playerLife <= 0.0f) && (playerId == 0)
          || (GameManager.instance.player2LifeBar.playerLife <= 0.0f) && (playerId == 1))
        {
            _currentState = PlayerCharacterState.State_Down;
            _animator.Play(Animator.StringToHash(animationDownName));
        }
        else
        {
            _currentState = PlayerCharacterState.State_Idle;
        }
    }

    public void StartDash()
    {
        if (_currentState == PlayerCharacterState.State_Dash)
        {
            return;
        }

        // if the character is allowed to dash
        if (_cooldownTimeStamp <= Time.time)
        {
            // we set its dashing state to true
            //_previousState = _currentState;
            _currentState = PlayerCharacterState.State_Dash;

            _cooldownTimeStamp = Time.time + DashCooldown;
            // we launch the boost corountine with the right parameters
            StartCoroutine(Dash());

 #if !UNITY_PS4
            //TODO: refactor analytics
            Analytics.CustomEvent("move", new Dictionary<string, object>
                 {{ "type", "dash"}}
                 );
#endif
        }
    }

    protected IEnumerator Dash()
    {
        _initialPosition = this.transform.position;
        _distanceTraveled = 0;
        _shouldKeepDashing = true;
        _dashDirection = IsFacingRight ? 1f : -1f;
        _computedDashForce = DashForce * _dashDirection;

        // Play dash animation
        _animator.Play(Animator.StringToHash(animationDashName));

        // Play dash sound
        FMODUnity.RuntimeManager.PlayOneShot(dashEvent, transform.position);

        while (_distanceTraveled < DashDistance && _shouldKeepDashing)
        {
            _distanceTraveled = Vector3.Distance(_initialPosition, this.transform.position);

            if ((_controller.collisionState.left || _controller.collisionState.right) || (System.Math.Abs(_controller.velocity.x) < Mathf.Epsilon && !_controller.isGrounded))
            {
                //Debug.Log("Collided while dashing.");
                _shouldKeepDashing = false;
            }

            _gravityActive = false;

            _velocity.y = 0;
            _velocity.x = _computedDashForce;

            yield return null;
        }

        _currentState = PlayerCharacterState.State_Idle;
        _gravityActive = true;
    }

    public void ResetMeleeAttackNums()
    {
        _numOfMeleesQueued = 0;
    }

    public void StartMeleeAttack()
    {
        if (_numOfMeleesQueued < animationMeleeNames.Count)
        {
            _numOfMeleesQueued++;
            //Debug.Log("_numOfMeleesQueued: " + _numOfMeleesQueued);

            if (_numOfMeleesQueued == 1)
            {
                //Debug.Log("Start meleeing.");
                //_previousState = _currentState;
                //_currentState = PlayerCharacterState.State_MeleeAttack;
                StartCoroutine(MeleeSequence());
 #if !UNITY_PS4
            //TODO: refactor analytics
            Analytics.CustomEvent("move", new Dictionary<string, object>
                 {{ "type", "melee"}}
                 );
#endif
            }
            
        }
    }

    protected IEnumerator MeleeSequence()
    {
        int currentSequenceNumber = 0;

        if (_currentState == PlayerCharacterState.State_MeleeAttack)
        {

            while ((currentSequenceNumber < _numOfMeleesQueued) && (_currentState == PlayerCharacterState.State_MeleeAttack))
            {
                // Set the attack type to normal melee
                AttackController meleeController = meleeTrigger.GetComponent<AttackController>();
                meleeController.attackType = AttackType.TYPE_NORMALMELEE;
                meleeController.attackPower = meleeAttackPower;
                if (meleeComboHurtParticles.Count >= (currentSequenceNumber + 1)) {
                    meleeController.collisionParticle = meleeComboHurtParticles[currentSequenceNumber];
                }

                // Shake camera on last combo on hit
                if (currentSequenceNumber == (animationMeleeNames.Count - 1)) {
                    meleeController.shakeCameraOnHit = true;
                } else {
                    meleeController.shakeCameraOnHit = false;
                }

                //Debug.Log("currentSequenceNumber: " + currentSequenceNumber);
                string currentAnimationName = animationMeleeNames[currentSequenceNumber];
                _animator.Play(Animator.StringToHash(currentAnimationName));
                float targetTime = Time.time + animationMeleeTimes[currentSequenceNumber];

                // Move forward a bit
                _computedMeleeMovementForce = meleeMovementForceInFrame * (IsFacingRight ? 1f : -1f);

                while ((Time.time < targetTime) && (_currentState == PlayerCharacterState.State_MeleeAttack)) {
                    yield return null;
                }

                currentSequenceNumber++;
            }

            if (_currentState == PlayerCharacterState.State_MeleeAttack)
                _currentState = _previousState;
            ResetMeleeAttackNums();
        }
    }

    public void ResetSpecialAttackNums()
    {
        _numOfSpecialAttacksQueued = 0;
    }

    public void StartSpecialAttack()
    {
        if (_numOfSpecialAttacksQueued < animationSpecialAttackNames.Count)
        {
            _numOfSpecialAttacksQueued++;

            if (_numOfSpecialAttacksQueued == 1)
            {
                StartCoroutine(SpecialAttackSequence());

 #if !UNITY_PS4
            //TODO: refactor analytics
            Analytics.CustomEvent("move", new Dictionary<string, object>
                 {{ "type", "special"}}
                 );
#endif
            }
        }
    }

    protected IEnumerator SpecialAttackSequence()
    {
        int currentSequenceNumber = 0;

        if (_currentState == PlayerCharacterState.State_SpecialAttack)
        {
            while ((currentSequenceNumber < _numOfSpecialAttacksQueued) && (_currentState == PlayerCharacterState.State_SpecialAttack))
            {
                string currentAnimationName = animationSpecialAttackNames[currentSequenceNumber];
                _animator.Play(Animator.StringToHash(currentAnimationName));
                //yield return new WaitForSeconds(animationSpecialAttackTimes[currentSequenceNumber]);
                float targetTime = Time.time + animationSpecialAttackTimes[currentSequenceNumber];

				while ((Time.time < targetTime) && (_currentState == PlayerCharacterState.State_SpecialAttack))
				{
					yield return null;
				}

                currentSequenceNumber++;
            }

            if (_currentState == PlayerCharacterState.State_SpecialAttack)
			    _currentState = _previousState;
			ResetSpecialAttackNums();
        }
    }

    public void SpawnSpecialAttack()
    {
        FMODUnity.RuntimeManager.PlayOneShot(gunEvent, transform.position);
        StartVibrate(0.0f, 1.0f, 0.2f);
        GameObject specialAttackInstance = Instantiate(specialAttackPrefab, specialAttackSpawnPoint.position, Quaternion.identity) as GameObject;
        float specialAttackDirection = IsFacingRight ? 1f : -1f;
        Vector3 specialAttackScale = specialAttackInstance.transform.localScale;
        specialAttackScale.x = specialAttackDirection;
        specialAttackInstance.transform.localScale = specialAttackScale;
        specialAttackInstance.GetComponent<AttackController>().owner = this.transform;
    }

    public void StartCrouch()
    {
        //Debug.Log("Starting to crouch.");
        //_previousState = _currentState;
        _currentState = PlayerCharacterState.State_Crouch;
        _animator.Play(Animator.StringToHash(animationCrouchName));
#if !UNITY_PS4
            //TODO: refactor analytics
            Analytics.CustomEvent("move", new Dictionary<string, object>
                 {{ "type", "crounch"}}
                 );
#endif        
    }

    public void StopCrouch()
    {
        _currentState = PlayerCharacterState.State_Idle;
    }

    public void StartKick()
    {
        if (_currentState == PlayerCharacterState.State_Crouch)
		{
            Debug.Log("From crouch to kick.");
            _previousState = _currentState;
            _currentState = PlayerCharacterState.State_Kick; FMODUnity.RuntimeManager.PlayOneShot(kickEvent, transform.position);
            StartCoroutine(Kick());

#if !UNITY_PS4
            //TODO: refactor analytics
            Analytics.CustomEvent("move", new Dictionary<string, object>
                 {{ "type", "melee_kick"}}
                 );
#endif
        }
    }

    protected IEnumerator Kick()
    {
        meleeTrigger.GetComponent<AttackController>().attackType = AttackType.TYPE_KICK;
        meleeTrigger.GetComponent<AttackController>().collisionParticle = hurtParticlesPrefab;
        meleeTrigger.GetComponent<AttackController>().attackPower = meleeAttackPower;
        _animator.Play(Animator.StringToHash(animationKickName));
        yield return new WaitForSeconds(kickDurationInSecs);
        _currentState = _previousState;
        if (_currentState == PlayerCharacterState.State_Crouch)
        {
            _animator.Play(Animator.StringToHash(animationCrouchName));
        }
    }

    public void StartBlock()
    {
        //_previousState = _currentState;
        _currentState = PlayerCharacterState.State_Block;
        _animator.Play(Animator.StringToHash(animationBlockName));

#if !UNITY_PS4
            //TODO: refactor analytics
            Analytics.CustomEvent("move", new Dictionary<string, object>
                 {{ "type", "block"}}
                 );
#endif        
    }

    public void StopBlock()
    {
        //_currentState = _previousState;
        _currentState = PlayerCharacterState.State_Idle;
    }

    public void StartCrouchBlock()
    {
        _currentState = PlayerCharacterState.State_CrouchBlock;
        _animator.Play(Animator.StringToHash(animationCrouchBlockName));
#if !UNITY_PS4
            //TODO: refactor analytics
            Analytics.CustomEvent("move", new Dictionary<string, object>
                 {{ "type", "crouch_block"}}
                 );
#endif        
    }

    public void StartVibrate(float leftMotor, float rightMotor, float duration)
    {
        bool vibrationSupportAvailable = false;
        foreach (Joystick j in _player.controllers.Joysticks)
        {
            if (j.supportsVibration)
            {
                if (j.vibrationMotorCount >= 2)
                {
                    vibrationSupportAvailable = true;
                }
            }
        }
        if (vibrationSupportAvailable && !_isControllerVibrating)
        {
            //Debug.Log("Vibration supported.");
            _isControllerVibrating = true;
            StartCoroutine(Vibrate(leftMotor, rightMotor, duration));
        }
    }

    public void StopVibrate()
    {
#if UNITY_PS4 && !UNITY_EDITOR
        foreach (Joystick j in _player.controllers.Joysticks) {
            var ext = j.GetExtension<PS4GamepadExtension>();
            ext.StopVibration();
        }
#else
		foreach (Joystick j in _player.controllers.Joysticks)
		{
			j.StopVibration();
		}
#endif
        _isControllerVibrating = false;
    }

    IEnumerator Vibrate(float leftMotor, float rightMotor, float duration) {
#if UNITY_PS4 && !UNITY_EDITOR
        foreach (Joystick j in _player.controllers.Joysticks) {
            var ext = j.GetExtension<PS4GamepadExtension>();
            ext.SetVibration(leftMotor, rightMotor);
        }
#else
        foreach (Joystick j in _player.controllers.Joysticks) {
            j.SetVibration(leftMotor, rightMotor);
        }

#endif
        yield return new WaitForSeconds(duration);
#if UNITY_PS4 && !UNITY_EDITOR
        foreach (Joystick j in _player.controllers.Joysticks) {
            var ext = j.GetExtension<PS4GamepadExtension>();
            ext.StopVibration();
        }
#else
        foreach (Joystick j in _player.controllers.Joysticks)
		{
            j.StopVibration();
		}
#endif
        _isControllerVibrating = false;
    }
}
