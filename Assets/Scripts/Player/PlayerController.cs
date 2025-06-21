using UnityEngine;
using System.Collections;

public class PlayerController : Singleton<PlayerController>
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float groundCheckDistance = 0.4f;
    [SerializeField] private float groundCheckRadius = 0.4f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private float slideSpeed = 5f;
    [SerializeField] private float gravityMultiplier = 1.0f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float fallGravityMultiplier = 1.5f;
    [SerializeField] private float airResistance = 9.8f;

    [Header("Combat Settings")]
    [SerializeField] private float basicAttackRecoveryTime = 0.5f;

    [Header("Input Buffer Settings")]
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float castBufferTime = 0.5f;

    private CharacterController cc;
    private CameraController cameraController;
    private StaminaSystem staminaSystem;
    private InputBuffer inputBuffer;

    private Vector3 moveDirection;
    private Vector3 slideDirection;
    private Vector3 airMoveVelocity = Vector3.zero;
    private Vector3 vector;

    private PositionStatus _posStatus;
    public PositionStatus posStatus
    {
        get => _posStatus;
        set
        {
            if (_posStatus != value)
            {
                _posStatus = value;
                Debug.Log($"Position status changed to: {_posStatus}");
            }
        }
    }

    private float verticalVelocity;
    private float recoveryEndTime;
    private float coyoteTimeCounter = 0f;
    private float jumpBufferCounter;
    private float wallSlowTimer = 0f;

    private bool isRecovering = false;
    private bool isSliding = false;
    private bool wasGrounded;
    private bool isMovable = true;
    private bool canWallJump = false;
    private bool isGliding = false;
    private bool jumpKeyHeld = false;
    private bool hasGlideJustStarted = false;
    private bool wallJumpSlowActive = false;

    #region Jump Variables
    private enum JumpState
    {
        None,
        Charging,
        WallReady,
        WallSlow,
        WallJump,
        Glide,
        SlideJump
    }
    private JumpState jumpState = JumpState.None;
    private float jumpChargeTime = 0f;

    [Header("Jump Settings")]
    [SerializeField] private float maxJumpChargeTime = 1.0f;
    [SerializeField] private float minJumpForce = 8f;
    [SerializeField] private float maxJumpForce = 16f;
    [SerializeField] private float minJumpStamina = 20f;
    [SerializeField] private float maxJumpStamina = 40f;
    [SerializeField] private float wallJumpForce = 10f;
    [SerializeField] private float wallJumpMaxAngle = 60f;
    [SerializeField] private float wallCheckRadius = 1.0f;
    [SerializeField] private float wallJumpSlowTime = 0.5f;
    [SerializeField] private float slideJumpForce = 10f;
    #endregion


    private void Start()
    {
        cc = GetComponent<CharacterController>();
        cameraController = FindObjectOfType<CameraController>();
        inputBuffer = GetComponent<InputBuffer>();
        staminaSystem = GetComponent<StaminaSystem>();

        if (inputBuffer == null)
        {
            inputBuffer = gameObject.AddComponent<InputBuffer>();
        }

        verticalVelocity = -1f;
    }

    private void Update()
    {
        UpdateStatus();
        if (isMovable) HandleMovement();
        if (posStatus == PositionStatus.Sliding) HandleSliding();
        HandleJump();
    }

    /*
    private void CheckGround()
    {
        // 直接使用 CharacterController.isGrounded
        isGrounded = cc.isGrounded;
        Debug.Log($"CharacterController.isGrounded: {isGrounded}");

        // 更新土狼時間
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (isGrounded)
        {
            // 檢查是否在陡坡上
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance + 0.1f, groundLayer))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle > maxSlopeAngle)
                {
                    isSliding = true;
                    slideDirection = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                }
                else
                {
                    isSliding = false;
                }
            }
        }
    }
    */

    private void HandleMovement()
    {
        // Don't handle movement if casting
        if (cameraController.IsCasting())
            return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 movement = new Vector3(horizontal, 0f, vertical).normalized;

        if (movement.magnitude >= 0.1f)
        {
            // Get camera's forward direction, but ignore Y component
            Vector3 cameraForward = cameraController.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            // Calculate movement direction relative to camera
            Vector3 moveDir = cameraForward * vertical + cameraController.transform.right * horizontal;
            moveDir.Normalize();

            // Check for sprint
            float currentSpeed = Input.GetKey(KeyCode.LeftShift) && staminaSystem.CanUseStamina(10f) ? sprintSpeed : moveSpeed;

            if (currentSpeed == sprintSpeed)
            {
                staminaSystem.UseSprintStamina();
            }

            // Move the player
            cc.Move(moveDir * currentSpeed * Time.deltaTime);

            // Rotate player to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleJump()
    {
        // 取得跳躍鍵狀態
        bool jumpPressed = Input.GetButtonDown("Jump");
        bool jumpHeld = Input.GetButton("Jump");
        bool jumpReleased = Input.GetButtonUp("Jump");

        // 若落地，重置滑翔與減速狀態
        if (posStatus == PositionStatus.Grounded)
        {
            isGliding = false;
            hasGlideJustStarted = false;
            wallJumpSlowActive = false;
            Time.timeScale = 1f;
        }

        switch (posStatus)
        {
            case PositionStatus.Midair:
                if (jumpPressed)
                {
                    if (CheckWallNearby(out vector))
                    {
                        jumpState = JumpState.WallSlow;
                        wallJumpSlowActive = true;
                        Time.timeScale = 0.2f; // 進入減速
                    }
                }
                if (jumpState == JumpState.WallSlow && jumpHeld)
                {
                    if (!CheckWallNearby(out vector))
                    {
                        // 失去合格面，結束減速
                        jumpState = JumpState.None;
                        wallJumpSlowActive = false;
                        Time.timeScale = 1f;
                    }
                }
                if (jumpState == JumpState.WallSlow && jumpReleased)
                {
                    if (CheckWallNearby(out vector))
                    {
                        // 蹬牆跳
                        Vector3 camForward = cameraController.transform.forward;
                        Vector3 jumpDir = CalcWallJumpDir(vector, camForward, wallJumpMaxAngle);
                        verticalVelocity = wallJumpForce;
                        moveDirection = jumpDir * wallJumpForce;
                        cc.Move(moveDirection * Time.deltaTime);
                    }
                    // 結束減速
                    jumpState = JumpState.None;
                    wallJumpSlowActive = false;
                    Time.timeScale = 1f;
                }
                if (jumpState != JumpState.WallSlow && jumpPressed && !isGliding)
                {
                    jumpState = JumpState.Glide;
                    isGliding = true;
                    hasGlideJustStarted = true;
                    if (verticalVelocity > 0f)
                        verticalVelocity = 0f;
                }
                // 滑翔狀態
                if (jumpState == JumpState.Glide && isGliding)
                {
                    // 滑翔邏輯（可調整重力或水平移動）
                    verticalVelocity = Mathf.Max(verticalVelocity, -2f); // 減緩下墜
                    if (!hasGlideJustStarted && jumpPressed)
                    {
                        jumpState = JumpState.None;
                        isGliding = false;
                    }
                    hasGlideJustStarted = false;
                }
                break;
            case PositionStatus.Grounded:
            case PositionStatus.CoyoteTime:
                if (jumpPressed && staminaSystem.CanUseStamina(minJumpStamina))
                {
                    jumpState = JumpState.Charging;
                    jumpChargeTime = 0f;
                    jumpKeyHeld = true;
                }
                if (jumpState == JumpState.Charging && jumpKeyHeld)
                {
                    if (jumpHeld)
                    {
                        jumpChargeTime += Time.deltaTime;
                        if (jumpChargeTime > maxJumpChargeTime) jumpChargeTime = maxJumpChargeTime;
                    }
                    if (jumpReleased)
                    {
                        float t = Mathf.Clamp01(jumpChargeTime / maxJumpChargeTime);
                        float force = Mathf.Lerp(minJumpForce, maxJumpForce, t);
                        float staminaCost = Mathf.Lerp(minJumpStamina, maxJumpStamina, t);
                        if (staminaSystem.CanUseStamina(staminaCost))
                        {
                            // 跳躍方向：地面法線
                            Vector3 jumpDir = Vector3.up;
                            RaycastHit hit;
                            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance + 0.1f, groundLayer))
                            {
                                jumpDir = hit.normal;
                            }
                            airMoveVelocity = jumpDir * force;
                            verticalVelocity = airMoveVelocity.y;
                            airMoveVelocity.y = 0f;
                            staminaSystem.UseStamina(staminaCost);
                        }
                        jumpState = JumpState.None;
                        jumpKeyHeld = false;
                    }
                }
                break;
            case PositionStatus.Sliding:
                if (jumpReleased)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 3))
                    {
                        Vector3 jumpDir = hit.normal;
                        airMoveVelocity = jumpDir * slideJumpForce;
                        verticalVelocity = airMoveVelocity.y;
                        airMoveVelocity.y = 0f;
                    }
                    jumpState = JumpState.None;
                }
                break;
        }

        // 應用重力
        if (verticalVelocity > 0)
        {
            verticalVelocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
        }
        else
        {
            verticalVelocity += Physics.gravity.y * fallGravityMultiplier * Time.deltaTime;
        }

        if (posStatus == PositionStatus.Grounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
            airMoveVelocity = Vector3.zero;
        }

        // 空中移動：維持跳躍初速的水平分量
        Vector3 totalMove = airMoveVelocity * Time.deltaTime + new Vector3(0, verticalVelocity, 0) * Time.deltaTime;
        airMoveVelocity = Vector3.Lerp(airMoveVelocity, Vector3.zero, airResistance * Time.deltaTime);
        cc.Move(totalMove);
    }

    // 找出最近的朝下面
    private bool CheckWallNearby(out Vector3 vector)
    {
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        float checkDistance = wallCheckRadius * 2f;
        Collider[] colliders = Physics.OverlapSphere(origin, wallCheckRadius, groundLayer);
        if (colliders.Length == 0)
        {
            vector = Vector3.zero;
            return false; // 沒有找到任何碰撞體
        }
        foreach (Collider collider in colliders)
        {
            Vector3 currVector = collider.ClosestPoint(origin) - origin;
            if (Vector3.Angle(currVector, Vector3.down) < 90f)
            {
                Debug.Log($"Found wall collider: {collider.name}, Direction: {currVector}");
                vector = currVector;
                return true; // 找到一個朝下的碰撞體
            }
        }
        vector = Vector3.zero;
        return false; // 沒有找到朝下的碰撞體

        /*
        float minDist = float.MaxValue;
        Vector3 bestNormal = Vector3.zero;
        bool found = false;
        foreach (var hit in hits)
        {
            Debug.Log($"Hit: {hit.collider.name}, Normal: {hit.normal}, Distance: {hit.distance}");
            float downAngle = Vector3.Angle(hit.normal, Vector3.down);
            Debug.Log($"Down Angle: {downAngle}");
            if (downAngle < 90f) // 朝下
            {
                float dist = hit.distance;
                if (dist < minDist)
                {
                    minDist = dist;
                    bestNormal = hit.normal;
                    found = true;
                }
            }
        }
        if (found)
        {
            normal = bestNormal;
            return true;
        }
        normal = Vector3.zero;
        return false;
        */
    }

    // 蹬牆跳方向計算
    private Vector3 CalcWallJumpDir(Vector3 vector, Vector3 camForward, float maxAngle)
    {
        float angle = Vector3.Angle(camForward, vector);
        if (angle < maxAngle)
            return camForward.normalized;
        else
            return Vector3.Slerp(vector, camForward, maxAngle / angle).normalized;
    }

    private void HandleSliding()
    {
        // 在滑動時禁用跳躍
        inputBuffer.ClearBuffer();
        jumpBufferCounter = 0f; // 清除跳躍緩衝

        // 計算滑動速度
        float slideVelocity = slideSpeed * Time.deltaTime;
        cc.Move(slideDirection * slideVelocity);
    }

    public void StartRecovery(float recoveryTime)
    {
        isRecovering = true;
        recoveryEndTime = Time.time + recoveryTime;
    }

    public void StartBasicAttackRecovery()
    {
        StartRecovery(basicAttackRecoveryTime);
    }

    public bool IsRecovering()
    {
        return isRecovering;
    }

    private void OnDrawGizmosSelected()
    {
        // 繪製地面檢測範圍
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * groundCheckRadius, groundCheckRadius);

        // 繪製地面檢測射線
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + Vector3.down * (groundCheckDistance + 0.1f));
    }

    private void UpdateStatus()
    {
        if (cc.isGrounded) // Now on the ground
        {
            if (verticalVelocity > 0) posStatus = PositionStatus.Midair; // If jumping, stay in midair
            else posStatus = PositionStatus.Grounded;

            // 檢查是否在陡坡上
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 10))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle > maxSlopeAngle)
                {
                    posStatus = PositionStatus.Sliding;
                    slideDirection = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                }
            }
        }
        else if (posStatus == PositionStatus.CoyoteTime) // In coyote time
        {
            if (coyoteTimeCounter > 0.1f)
            {
                posStatus = PositionStatus.Midair;
                coyoteTimeCounter = 0f; // Reset coyote time counter
            }
            else
            {
                coyoteTimeCounter += Time.deltaTime;
            }
        }
        else if (posStatus == PositionStatus.Grounded) // Last frame was grounded but now in midair
        {
            posStatus = PositionStatus.CoyoteTime;
        }
        else
        {
            posStatus = PositionStatus.Midair; // Default to midair if not grounded or in coyote time
        }
    }
}