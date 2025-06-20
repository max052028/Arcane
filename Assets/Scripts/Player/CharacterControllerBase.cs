using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class CharacterControllerBase : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpForce = 8f;
    public float gravityMultiplier = 2f;
    public float maxSlopeAngle = 45f;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundMask;

    protected Rigidbody rb;
    protected CapsuleCollider capsule;
    protected bool isGrounded;
    protected Vector3 groundNormal;
    protected bool isSprinting;
    protected bool jumpBuffered;
    protected float jumpCharge;
    protected float jumpChargeTime = 0.5f;
    protected float jumpChargeTimer;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        rb.freezeRotation = true;
    }

    protected virtual void Update()
    {
        GroundCheck();
    }

    protected virtual void FixedUpdate()
    {
        ApplyExtraGravity();
    }

    protected void Move(Vector3 moveDir, float speed)
    {
        Vector3 projectedMove = Vector3.ProjectOnPlane(moveDir, groundNormal).normalized;
        Vector3 velocity = projectedMove * speed;
        velocity.y = rb.velocity.y;
        rb.velocity = velocity;
    }

    protected void TryJump()
    {
        if (isGrounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(Vector3.up * (jumpForce + jumpCharge * jumpForce), ForceMode.VelocityChange);
        }
    }

    protected void ApplyExtraGravity()
    {
        if (!isGrounded)
            rb.AddForce(Physics.gravity * (gravityMultiplier - 1), ForceMode.Acceleration);
    }

    protected void GroundCheck()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float radius = capsule.radius * 0.95f;
        float castDist = capsule.bounds.extents.y + groundCheckDistance;
        if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, castDist, groundMask, QueryTriggerInteraction.Ignore))
        {
            isGrounded = Vector3.Angle(hit.normal, Vector3.up) <= maxSlopeAngle;
            groundNormal = hit.normal;
        }
        else
        {
            isGrounded = false;
            groundNormal = Vector3.up;
        }
    }
}
