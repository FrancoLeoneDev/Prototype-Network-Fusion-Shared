using System.Collections;
using Fusion;
using UnityEngine;

/// <summary>
/// Player controller for the obstacle course: movement, jumping with coyote
/// time, moving-platform carry, hazard respawn and the victory hand-off.
/// </summary>
/// <remarks>
/// Shared Mode topology: every client holds state authority over its own player,
/// so input is polled locally instead of being routed through Fusion's
/// NetworkInput pipeline. That keeps the prototype simple at the cost of
/// prediction and cheat resistance — see the README for the trade-off.
/// </remarks>
[RequireComponent(typeof(Rigidbody))]
public class Player : NetworkBehaviour
{
    private const float AccelerationScale = 10f;
    private const float JumpAnimationDuration = 1f;
    private const int MaxOverlapResults = 4;

    private static readonly int JumpingAnimatorHash = Animator.StringToHash("Jumping");

    /// <summary>The player this client owns. Null until the local player spawns.</summary>
    public static Player LocalPlayer { get; private set; }

    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float jumpForce = 3f;

    [Header("Ground detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.5f;
    [SerializeField] private float coyoteTime = 0.1f;

    // Reused across ticks so the per-tick overlap query never allocates.
    private readonly Collider[] _overlapResults = new Collider[MaxOverlapResults];

    private Rigidbody _rigidbody;
    private Animator _animator;

    private int _obstacleLayer;
    private int _victoryLayer;
    private int _movingPlatformLayer;

    private float _moveX;
    private float _moveY;
    private bool _jumpPressed;
    private bool _isGrounded;
    private float _coyoteTimeCounter;
    private bool _hasFinished;
    private bool _isFrozen;

    private Transform _platform;
    private Vector3 _platformLastPosition;

    private void Awake()
    {
        // Resolved here rather than in Spawned() so collision callbacks that fire
        // before the network spawn completes still compare against real layers.
        _obstacleLayer = LayerMask.NameToLayer("Obstacle");
        _victoryLayer = LayerMask.NameToLayer("VictoryFlag");
        _movingPlatformLayer = LayerMask.NameToLayer("MovingPlatform");
    }

    public override void Spawned()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponentInChildren<Animator>();

        if (!HasStateAuthority) return;

        LocalPlayer = this;
        AttachCamera();
    }

    private void Update()
    {
        if (!HasStateAuthority) return;

        _moveX = Input.GetAxisRaw("Horizontal");
        _moveY = Input.GetAxisRaw("Vertical");

        TickCoyoteTime();

        if (Input.GetKeyDown(KeyCode.Space) && _coyoteTimeCounter > 0f)
        {
            _jumpPressed = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (PlayerSpawner.Instance == null || !PlayerSpawner.Instance.GameStart) return;
        if (!HasStateAuthority) return;

        CarryWithPlatform();

        _isGrounded = IsTouchingGround();

        Move();
        CheckHazardsAndGoal();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer != _movingPlatformLayer) return;

        _platform = collision.transform;
        _platformLastPosition = _platform.position;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer == _movingPlatformLayer)
        {
            _platform = null;
        }
    }

    /// <summary>Points the main camera's follow rig at this player's camera anchor.</summary>
    private void AttachCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        CameraBehavior follow = mainCamera.GetComponent<CameraBehavior>();
        CamPos anchor = GetComponentInChildren<CamPos>();

        if (follow != null && anchor != null)
        {
            follow.Target = anchor.transform;
        }
    }

    /// <summary>
    /// Keeps a short grace window open after leaving the ground during which a
    /// jump input is still accepted.
    /// </summary>
    private void TickCoyoteTime()
    {
        if (_isGrounded || IsTouchingGround())
        {
            _coyoteTimeCounter = coyoteTime;
        }
        else
        {
            _coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private bool IsTouchingGround()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    /// <summary>Applies the platform's per-tick delta so the player rides along with it.</summary>
    private void CarryWithPlatform()
    {
        if (_platform == null) return;

        Vector3 delta = _platform.position - _platformLastPosition;
        _rigidbody.MovePosition(_rigidbody.position + delta);
        _platformLastPosition = _platform.position;
    }

    private void Move()
    {
        if (_isFrozen)
        {
            StopHorizontalMovement();
            return;
        }

        if (_moveX != 0f || _moveY != 0f)
        {
            Vector3 direction = (Vector3.right * _moveX) + (Vector3.forward * _moveY);
            _rigidbody.velocity += direction * (speed * AccelerationScale * Runner.DeltaTime);

            ClampHorizontalSpeed();
        }
        else
        {
            StopHorizontalMovement();
        }

        if (_jumpPressed && (_isGrounded || _coyoteTimeCounter > 0f))
        {
            Jump();
        }
    }

    private void ClampHorizontalSpeed()
    {
        Vector3 velocity = _rigidbody.velocity;
        if (Mathf.Abs(velocity.x) <= speed && Mathf.Abs(velocity.z) <= speed) return;

        float verticalSpeed = velocity.y;
        velocity = Vector3.ClampMagnitude(velocity, speed);
        velocity.y = verticalSpeed;

        _rigidbody.velocity = velocity;
    }

    private void StopHorizontalMovement()
    {
        Vector3 velocity = _rigidbody.velocity;
        velocity.x = 0f;
        velocity.z = 0f;

        _rigidbody.velocity = velocity;
    }

    private void Jump()
    {
        Vector3 velocity = _rigidbody.velocity;
        velocity.y = jumpForce;
        _rigidbody.velocity = velocity;

        _jumpPressed = false;
        StartCoroutine(PlayJumpAnimation());
    }

    private IEnumerator PlayJumpAnimation()
    {
        _animator.SetBool(JumpingAnimatorHash, true);
        yield return new WaitForSeconds(JumpAnimationDuration);
        _animator.SetBool(JumpingAnimatorHash, false);
    }

    /// <summary>Overlap-tests the player against hazards and the goal flag.</summary>
    private void CheckHazardsAndGoal()
    {
        int hitCount = Runner.GetPhysicsScene().OverlapBox(
            transform.position,
            transform.localScale / 2f,
            _overlapResults);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _overlapResults[i];
            if (hit == null) continue;

            if (hit.gameObject.layer == _obstacleLayer)
            {
                ResetPosition();
            }
            // IsForward keeps the RPC from being re-sent during resimulation.
            else if (hit.gameObject.layer == _victoryLayer && !_hasFinished && Runner.IsForward)
            {
                _hasFinished = true;
                SetVictoryScreenRpc(this);
            }
        }
    }

    /// <summary>Teleports the player back to its spawn point after hitting a hazard.</summary>
    public void ResetPosition()
    {
        _rigidbody.position = PlayerSpawner.SpawnPosition;
    }

    /// <summary>Announces the winner so every client shows the matching end screen.</summary>
    [Rpc]
    private void SetVictoryScreenRpc(Player winner)
    {
        if (UiManager.Instance != null)
        {
            UiManager.Instance.SetVictoryScreen(winner);
        }

        if (winner != LocalPlayer)
        {
            _isFrozen = true;
        }
    }
}
