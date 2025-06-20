using UnityEngine;

public class CameraController : MonoBehaviour
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
    
    private void Start()
    {
        playerTransform = FindObjectOfType<PlayerController>().transform;
        if (playerTransform == null)
        {
            Debug.LogError("Player not found in scene!");
            enabled = false;
            return;
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
    
    public void StartSpellCasting()
    {
        isCasting = true;
        targetDistance = spellCastingDistance;
    }
    
    public void EndSpellCasting()
    {
        isCasting = false;
        targetDistance = distanceFromPlayer;
    }
    
    public bool IsCasting()
    {
        return isCasting;
    }
} 