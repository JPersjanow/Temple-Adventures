using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float m_JumpForce = 400f;                          // Amount of force added when the player jumps.
    [Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;          // Amount of maxSpeed applied to crouching movement. 1 = 100%
    [SerializeField] private float m_SlideSpeed = 10f;
    [Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;  // How much to smooth out the movement
    [SerializeField] private bool m_AirControl = false;                         // Whether or not a player can steer while jumping;
    [SerializeField] private LayerMask m_WhatIsGround;                          // A mask determining what is ground to the character
    [SerializeField] private Transform m_GroundCheck;                           // A position marking where to check if the player is grounded.
    [SerializeField] private Transform m_CeilingCheck;                          // A position marking where to check for ceilings
    [SerializeField] private Transform m_WallRightCheck;
    [SerializeField] private Collider2D m_CrouchDisableCollider;                // A collider that will be disabled when crouching

    const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded
    private bool m_Grounded;            // Whether or not the player is grounded.
    private bool m_OnWall;
    const float k_CeilingRadius = .2f; // Radius of the overlap circle to determine if the player can stand up
    private Rigidbody2D m_Rigidbody2D;
    private bool m_FacingRight = true;  // For determining which way the player is currently facing.
    private float m_FacingDirection;
    private Vector3 m_Velocity = Vector3.zero;

    [Header("Events")]
    [Space]

    public UnityEvent OnLandEvent;

    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }

    public BoolEvent OnCrouchEvent;
    private bool m_wasCrouching = false;

    public BoolEvent OnSlideEvent;
    private bool m_wasSliding = false;
    private bool m_wasWallJumping = false;

    private bool m_canDoubleJump = true;

    public float fallMultiplier = 2.5f;
    public float fallDecreaser = 0.2f;

    public float m_WallJumpSpeed = 100f;

    public Vector2 initialPosition;

    private void Awake()
    {

        m_Rigidbody2D = GetComponent<Rigidbody2D>();

        if (OnLandEvent == null)
            OnLandEvent = new UnityEvent();

        if (OnCrouchEvent == null)
            OnCrouchEvent = new BoolEvent();

        if (OnSlideEvent == null)
            OnSlideEvent = new BoolEvent();
    }

    private void Start()
    {
        initialPosition = m_Rigidbody2D.position;
    }

    private void FixedUpdate()
    {
        //Debug.Log(m_Rigidbody2D.velocity);
        bool wasGrounded = m_Grounded;
        m_Grounded = false;
        m_OnWall = false;

        // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
        // This can be done using layers instead but Sample Assets will not overwrite your project settings.
        Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
        Collider2D[] collidersWallsRight = Physics2D.OverlapCircleAll(m_WallRightCheck.position, k_GroundedRadius, m_WhatIsGround);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
            {
                m_Grounded = true;
                if (!wasGrounded)
                    OnLandEvent.Invoke();
            }
        }

        for (int i = 0; i < collidersWallsRight.Length; i++)
        {
            if (collidersWallsRight[i].gameObject != gameObject)
            {
                m_OnWall = true;
                m_wasWallJumping = false;

                m_Rigidbody2D.velocity += new Vector2(0, -m_Rigidbody2D.velocity.y * fallDecreaser);
                
            }
        }
    }

    public void Update()
    {
        if (m_FacingRight)
        {
            m_FacingDirection = 1;
        }
        else
        {
            m_FacingDirection = -1;
        }
    }


    public void Move(float move, bool crouch, bool jump, bool slide)
    {
        if (m_Rigidbody2D.velocity.y < 0)
        {
            m_Rigidbody2D.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }

        // If crouching, check to see if the character can stand up
        if (!crouch)
        {
            // If the character has a ceiling preventing them from standing up, keep them crouching
            if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
            {
                crouch = true;
            }

        }

        //checks if grounded and allows double jump
        if (m_Grounded)
        {
            m_canDoubleJump = true;
        }

        //only control the player if grounded or airControl is turned on
        if (m_Grounded || m_AirControl)
        {

            // If crouching
            if (crouch)
            {
                if (!m_wasCrouching)
                {
                    m_wasCrouching = true;
                    OnCrouchEvent.Invoke(true);
                }

                // Reduce the speed by the crouchSpeed multiplier
                move *= m_CrouchSpeed;

                // Disable one of the colliders when crouching
                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.enabled = false;
            }
            else
            {
                // Enable the collider when not crouching
                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.enabled = true;

                if (m_wasCrouching)
                {
                    m_wasCrouching = false;
                    OnCrouchEvent.Invoke(false);
                }
            }

            if (slide && m_Grounded)
            {
                if (!m_wasSliding)
                {
                    m_wasSliding = true;
                    OnSlideEvent.Invoke(true);
                }

                move *= m_SlideSpeed;
            }
            else
            {
                if (m_wasSliding)
                {
                    m_wasSliding = false;
                    OnSlideEvent.Invoke(false);
                }
            }

            // Move the character by finding the target velocity
            Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
            // And then smoothing it out and applying it to the character
            m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);

            // If the input is moving the player right and the player is facing left...
            if (move > 0 && !m_FacingRight && !m_OnWall)
            {
                // ... flip the player.
                Flip();
            }
            // Otherwise if the input is moving the player left and the player is facing right...
            else if (move < 0 && m_FacingRight && !m_OnWall)
            {
                // ... flip the player.
                Flip();
            }
        }
        // If the player should jump...
        if (m_Grounded && jump)
        {
            // Add a vertical force to the player.
            m_Grounded = false;
            m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
        }
        else if (!m_Grounded && jump && m_canDoubleJump)
        {
            m_canDoubleJump = false;
            if (m_Rigidbody2D.velocity.y < 0)
            {
                m_Rigidbody2D.velocity -= new Vector2(0f, m_Rigidbody2D.velocity.y);
                m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce), ForceMode2D.Force);
            }
            else
            {
                m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce), ForceMode2D.Force);
            }
        }
        else if (m_OnWall && jump && !m_wasWallJumping)
        {
            m_wasWallJumping = true;
            m_Rigidbody2D.velocity = new Vector2(0, m_Rigidbody2D.velocity.y);
            Vector2 targerVelocity = new Vector2(-m_FacingDirection * 10f, 10f);
            m_Rigidbody2D.velocity += targerVelocity;
            Flip();
        }

        Debug.Log(m_Rigidbody2D.velocity);
            
        
        

    }


    private void Flip()
    {
        // Switch the way the player is labelled as facing.
        m_FacingRight = !m_FacingRight;

        // Multiply the player's x local scale by -1.
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    public void Reborn()
    {
        if (m_Rigidbody2D.position.y < -7f)
        {
            m_Rigidbody2D.position = initialPosition;
        }
    }
}
