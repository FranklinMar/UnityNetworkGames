using UnityEngine;
using UnityEngine.SceneManagement;

public class Fox : Photon.MonoBehaviour
{
    // 2DCollider for standing
    [SerializeField] private Collider2D Stand;
    // 2DCollider for crouching
    [SerializeField] private Collider2D Crouch;
    // Coordinates object of player's feet
    [SerializeField] private Transform feetPos;
    // PhotonView object for multiplayer
    [SerializeField] private PhotonView photonView;
    // Type of ground object for configuration of physics
    [SerializeField] private LayerMask groundType;
    // Speed velocity value for player
    [SerializeField] private float speed = 5f;
    // Number of lives value for player
    [SerializeField] private int lives = 3;
    // Jump force value for player
    [SerializeField] private float jump = 7.5f;

    // RigidBody2D object for physics (velocity and movement)
    private Rigidbody2D rigidBody;
    // Object for displaying Player sprite
    private SpriteRenderer sprite;
    // Animator object for displaying animations depending on state
    private Animator animator;
    // X axis movement force from input
    private float moveInput;
    // Y axis velocity float for animator object, to detect and change the state of animation
    private float yVelocity
    {
        get { return animator.GetFloat("yVelocity"); }
        set { bool isGrounded = CheckGround();
            // If doesn't touch ground
            if (!CheckGround())
            {
                animator.SetFloat("yVelocity", value);
            } 
            else
            {
                animator.SetFloat("yVelocity", 0);
            }
            animator.SetBool("Jump", !isGrounded);
        }
    }
    // X axis velocity float for animator object, to detect and change the state of animation
    private float xVelocity
    {
        get { return animator.GetFloat("xVelocity"); }
        set { animator.SetFloat("xVelocity", System.Math.Abs(value)); }
    }

    // Radius float radius for detecting colliders (In example, for ground)
    [SerializeField] private float checkRadius = 0.3f;
    // Max jump time for player (while going upwards)
    [SerializeField] private float jumpTime = 0.20f;
    // Speed increase value for running
    [SerializeField] private float runSpeedIncreasing = 1.5f;
    // Speed decrease value for crouching
    [SerializeField] private float runCrouchDecreasing = 0.5f;
    // Private in-air time counter
    private float jumpTimeCounter;
    // Player jumping indicator
    private bool isJumping;
    // Player running indicator
    private bool isRunning;
    // Player crouching indicator
    private bool isCrouching;

    private bool crouching
    {
        get
        {
            return isCrouching;
        }
        set
        {
            // Swap colliders and set animator state variable
            isCrouching = value;
            animator.SetBool("Crouch", isCrouching);
            Stand.enabled = !value;
            Crouch.enabled = value;
        }
    }
    // Is called before the first frame update
    void Start()
    {
        // If player object belongs to this user in network
        if (photonView.isMine)
        {
            rigidBody = GetComponent<Rigidbody2D>();
            sprite = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            // Assign this user's player object to camera
            CameraController camera = Camera.main.GetComponent<CameraController>();
            camera.player = transform;
        }
    }

    // Is called when user changes focus back to game
    void Awake()
    {
        // If player object belongs to this user in network
        if (photonView.isMine) { 
            rigidBody = GetComponent<Rigidbody2D>();
            sprite = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }
    }

    // Update that is called once per several frames
    void FixedUpdate()
    {
        
    }

    // Update that is called once per frame
    void Update()
    {
        // If player object belongs to this user in network
        if (photonView.isMine)
        {
            yVelocity = rigidBody.velocity.y;
            xVelocity = rigidBody.velocity.x;
            // Get X axis input movement
            moveInput = Input.GetAxisRaw("Horizontal");

            // If "Running" is pressed
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                isRunning = true;
            }
            // Else when released
            else if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                isRunning = false;
            }
            // If "Crouch" is pressed and touching the ground
            if (Input.GetButton("Crouch") && CheckGround())
            {
                crouching = true;
            }
            // Else if isn't pressing "Crouch" and not touching anything above while crouching, or when jumping
            else if ((!Input.GetButton("Crouch") && !IsUnder(Crouch)) || !CheckGround())
            {
                crouching = false;
            }

            // If button for X axis input was pressed
            if (Input.GetButton("Horizontal"))
            {
                Run();
            }

            // If touches ground and "Jump" is pressed
            if (CheckGround() && Input.GetButtonDown("Jump"))
            {
                isJumping = true;
                // Set counter to initial value
                jumpTimeCounter = jumpTime;
                Jump();
            }

            // If "Jump" is pressed and in the air
            if (Input.GetButton("Jump") && isJumping)
            {
                // If counter bigger than zero
                if (jumpTimeCounter >= 0.0f)
                {
                    // Decrease counter by smallest amount of time
                    jumpTimeCounter -= Time.deltaTime;
                    Jump();
                }
                else
                {
                    isJumping = false;
                }
            }

            // If "Jump" button is released
            if (Input.GetButtonUp("Jump"))
            {
                isJumping = false;
            }

            // If "ESC" button is pressed
            if (Input.GetButton("Cancel"))
            {
                // Leave connection to the room and return to lobby
                PhotonNetwork.LeaveRoom();
                // Load Main Menu UI
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
            }
        }
    }

    // X axis movement function
    private void Run()
    {
        // Usual speed
        float currentSpeed = moveInput * speed;
        // If running but isn't crouching
        if (isRunning && !isCrouching)
            currentSpeed *= runSpeedIncreasing;
        // If crouching and standing on ground
        if (isCrouching && CheckGround())
            currentSpeed *= runCrouchDecreasing;
        rigidBody.velocity = new Vector2(currentSpeed, rigidBody.velocity.y);
        sprite.flipX = moveInput < 0.0f;
    }

    // Add jump velocity, but not higher than "jump" property
    private void Jump()
    {
        rigidBody.velocity = new Vector2(rigidBody.velocity.x, rigidBody.velocity.y <= jump ? jump : rigidBody.velocity.y);
        yVelocity = rigidBody.velocity.y;
    }

    // Check if player object collides with floor colider
    private bool CheckGround()
    {
        // If number of colliders is bigger than 1
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, checkRadius);
        bool isGrounded = colliders.Length > 1;
        return isGrounded;
    }

    // Check if current collider is underneath some object
    private bool IsUnder(Collider2D current)
    {
        Collider2D[] otherColliders = Physics2D.OverlapCircleAll(current.transform.position, checkRadius);
        // Check for any colliders that are on top
        foreach (var collider in otherColliders)
        {
            if (collider.transform.position.y > current.transform.position.y)
            {
                return true;
            }
        }
        return false;
    }
}
