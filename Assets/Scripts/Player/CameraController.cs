using UnityEngine;

public class CameraController : Singleton<CameraController>
{
    [Header("Camera Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalRotationLimit = 80f;
    [SerializeField] private float distanceFromPlayer = 4f;
    [SerializeField] private float minDistanceFromPlayer = 1f;
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private float collisionOffset = 0.3f;  // 碰撞時的額外偏移
    
    [Header("Camera Offset")]
    [SerializeField] private Vector3 leftOffset = new Vector3(-1f, 1f, 0f);
    [SerializeField] private Vector3 rightOffset = new Vector3(1f, 1f, 0f);
    [SerializeField] private float offsetSmoothTime = 0.2f;
    
    [Header("Spell Casting")]
    [SerializeField] private float spellCastingDistance = 2f;
    [SerializeField] private float spellCastingTransitionTime = 0.3f;
    
    [Header("First Person Settings")]
    [SerializeField] private Transform fppTransform; // FPP 空物件的 Transform
    [SerializeField] private float fppTransitionSpeed = 5f;
    
    private Transform playerTransform;
    private float verticalRotation = 0f;
    private float horizontalRotation = 0f;
    private bool isRightOffset = false;
    private Vector3 currentOffset;
    private Vector3 targetOffset;
    private Vector3 offsetVelocity;
    private float currentDistance;
    private float targetDistance;
    private float distanceVelocity;
    private bool isCasting = false;
    private bool isDirectionalCasting = false;
    private Transform originalParent;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    
    private void Start()
    {
        playerTransform = FindObjectOfType<PlayerController>().transform;
        if (playerTransform == null)
        {
            Debug.LogError("Player not found in scene!");
            enabled = false;
            return;
        }
        
        // Check if FPP transform is assigned, if not create one
        if (fppTransform == null)
        {
            Debug.LogWarning("FPP Transform not assigned. Creating default FPP position...");
            CreateDefaultFPPTransform();
        }
        
        // Initialize offset and distance
        currentOffset = leftOffset;
        targetOffset = leftOffset;
        currentDistance = distanceFromPlayer;
        targetDistance = distanceFromPlayer;
        
        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void Update()
    {
        // Don't update camera logic during directional casting
        if (isDirectionalCasting)
        {
            // Only handle mouse look for player rotation
            HandleDirectionalCastingInput();
            return;
        }
        
        // Handle camera rotation
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Update rotation angles
        horizontalRotation += mouseX;
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalRotationLimit, verticalRotationLimit);
        
        // Calculate camera position
        Vector3 direction = new Vector3(0, 0, -1);
        Quaternion rotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0);
        
        // Handle offset switching
        if (Input.GetKeyDown(KeyCode.CapsLock))
        {
            isRightOffset = !isRightOffset;
            targetOffset = isRightOffset ? rightOffset : leftOffset;
        }
        
        // Smoothly move to target offset
        currentOffset = Vector3.SmoothDamp(currentOffset, targetOffset, ref offsetVelocity, offsetSmoothTime);
        
        // Smoothly move to target distance
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, spellCastingTransitionTime);
        
        // Check for wall collision
        Vector3 targetPosition = playerTransform.position + rotation * (direction * currentDistance + currentOffset);
        Vector3 directionToCamera = targetPosition - playerTransform.position;
        float distanceToTarget = directionToCamera.magnitude;
        
        // Cast ray to check for obstacles
        RaycastHit hit;
        if (Physics.Raycast(playerTransform.position, directionToCamera.normalized, out hit, distanceToTarget, collisionLayers))
        {
            // 增加額外的偏移距離，避免貼邊
            targetPosition = hit.point - directionToCamera.normalized * collisionOffset;
        }
        
        // Update camera position and rotation
        transform.position = targetPosition;
        transform.rotation = rotation;
    }
    
    private void HandleDirectionalCastingInput()
    {
        // Get mouse input for camera rotation only
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Only handle camera vertical rotation (first-person style)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalRotationLimit, verticalRotationLimit);
        
        transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
        
        // Send horizontal mouse input to PlayerController for player rotation
        PlayerController playerController = PlayerController.instance;
        if (playerController != null)
        {
            playerController.HandleDirectionalCastingRotation(mouseX);
        }
    }
    
    public void StartSpellCasting()
    {
        isCasting = true;
        targetDistance = spellCastingDistance;
    }
    
    public void StartDirectionalCasting(Transform fppTransform)
    {
        isCasting = true;
        isDirectionalCasting = true;
        
        // Store original position and parent
        originalParent = transform.parent;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        
        // Move camera to FPP position
        transform.SetParent(fppTransform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        
        Debug.Log("Started directional casting - camera moved to FPP position");
    }
    
    public void StartDirectionalCasting()
    {
        if (fppTransform == null)
        {
            Debug.LogError("FPP Transform is null! Cannot start directional casting.");
            return;
        }
        
        StartDirectionalCasting(fppTransform);
    }
    
    public void EndSpellCasting()
    {
        if (isDirectionalCasting)
        {
            // Restore original camera position and parent
            transform.SetParent(originalParent);
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
            
            isDirectionalCasting = false;
            Debug.Log("Ended directional casting - camera restored to original position");
        }
        
        isCasting = false;
        targetDistance = distanceFromPlayer;
    }
    
    public bool IsCasting()
    {
        return isCasting;
    }
    
    public bool IsDirectionalCasting()
    {
        return isDirectionalCasting;
    }
    
    private void CreateDefaultFPPTransform()
    {
        // Create a default FPP position as a child of the player
        GameObject fppObject = new GameObject("FPP_Auto");
        fppObject.transform.SetParent(playerTransform);
        
        // Position it at eye level (adjust this value as needed)
        fppObject.transform.localPosition = new Vector3(0, 1.6f, 0);
        fppObject.transform.localRotation = Quaternion.identity;
        
        fppTransform = fppObject.transform;
        Debug.Log("Created default FPP transform at player eye level position.");
    }
} 