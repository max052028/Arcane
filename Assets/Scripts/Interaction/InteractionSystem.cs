using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

/// <summary>
/// 交互系統管理器
/// </summary>
public class InteractionSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private KeyCode interactionKey = KeyCode.F;
    [SerializeField] private float detectionRadius = 3f;
    [SerializeField] private LayerMask interactableLayer = -1;
    [SerializeField] private int maxDetectionCount = 10;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showDebugLog = false;
    
    // Events
    public UnityEvent<List<IInteractable>> OnInteractableListChanged;
    public UnityEvent<IInteractable> OnFocusedInteractableChanged;
    public UnityEvent<IInteractable> OnInteractionPerformed;
    
    // Private fields
    private List<IInteractable> nearbyInteractables = new List<IInteractable>();
    private IInteractable focusedInteractable;
    private int focusedIndex = 0;
    private float lastScrollTime = 0f;
    private float scrollCooldown = 0.1f;
    
    // Components
    private Transform playerTransform;
    private Collider[] detectionBuffer;
    
    private void Start()
    {
        playerTransform = transform;
        detectionBuffer = new Collider[maxDetectionCount];
        
        // Initialize focused interactable
        UpdateFocusedInteractable();
    }
    
    private void Update()
    {
        DetectInteractables();
        HandleInput();
    }
    
    /// <summary>
    /// 檢測附近的可交互對象
    /// </summary>
    private void DetectInteractables()
    {
        var previousInteractables = new List<IInteractable>(nearbyInteractables);
        nearbyInteractables.Clear();
        
        // Use OverlapSphereNonAlloc for better performance
        int hitCount = Physics.OverlapSphereNonAlloc(
            playerTransform.position, 
            detectionRadius, 
            detectionBuffer, 
            interactableLayer
        );
        
        // Collect interactables
        for (int i = 0; i < hitCount; i++)
        {
            var interactable = detectionBuffer[i].GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteract)
            {
                float distance = Vector3.Distance(playerTransform.position, interactable.GetTransform().position);
                if (distance <= interactable.InteractionDistance)
                {
                    nearbyInteractables.Add(interactable);
                }
            }
        }
        
        // Sort by priority (descending) then by distance (ascending)
        nearbyInteractables = nearbyInteractables
            .OrderByDescending(x => x.InteractionPriority)
            .ThenBy(x => Vector3.Distance(playerTransform.position, x.GetTransform().position))
            .ToList();
        
        // Check for changes
        bool hasChanged = CheckForInteractableChanges(previousInteractables, nearbyInteractables);
        
        if (hasChanged)
        {
            HandleInteractableListChanged(previousInteractables, nearbyInteractables);
            OnInteractableListChanged?.Invoke(nearbyInteractables);
            
            if (showDebugLog)
            {
                Debug.Log($"Interactables updated: {nearbyInteractables.Count} found");
                foreach (var interactable in nearbyInteractables)
                {
                    Debug.Log($"  - {interactable.InteractionName} (Priority: {interactable.InteractionPriority})");
                }
            }
        }
    }
    
    /// <summary>
    /// 檢查交互對象列表是否有變化
    /// </summary>
    private bool CheckForInteractableChanges(List<IInteractable> previous, List<IInteractable> current)
    {
        if (previous.Count != current.Count)
            return true;
            
        for (int i = 0; i < previous.Count; i++)
        {
            if (previous[i] != current[i])
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 處理交互對象列表變化
    /// </summary>
    private void HandleInteractableListChanged(List<IInteractable> previous, List<IInteractable> current)
    {
        // Notify objects that are no longer in range
        foreach (var interactable in previous)
        {
            if (!current.Contains(interactable))
            {
                interactable.OnExitInteractionRange(gameObject);
            }
        }
        
        // Notify objects that are newly in range
        foreach (var interactable in current)
        {
            if (!previous.Contains(interactable))
            {
                interactable.OnEnterInteractionRange(gameObject);
            }
        }
        
        // Update focused interactable
        UpdateFocusedInteractable();
    }
    
    /// <summary>
    /// 處理輸入
    /// </summary>
    private void HandleInput()
    {
        // Handle interaction key
        if (Input.GetKeyDown(interactionKey))
        {
            if (focusedInteractable != null && focusedInteractable.CanInteract)
            {
                PerformInteraction();
            }
        }
        
        // Handle scroll wheel for switching focus
        if (nearbyInteractables.Count > 1 && Time.time - lastScrollTime > scrollCooldown)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.1f)
            {
                CycleFocus(-1); // Scroll up = previous
                lastScrollTime = Time.time;
            }
            else if (scroll < -0.1f)
            {
                CycleFocus(1); // Scroll down = next
                lastScrollTime = Time.time;
            }
        }
    }
    
    /// <summary>
    /// 循環切換聚焦對象
    /// </summary>
    private void CycleFocus(int direction)
    {
        if (nearbyInteractables.Count == 0)
            return;
            
        focusedIndex = (focusedIndex + direction) % nearbyInteractables.Count;
        if (focusedIndex < 0)
            focusedIndex = nearbyInteractables.Count - 1;
            
        UpdateFocusedInteractable();
        
        if (showDebugLog)
        {
            Debug.Log($"Focus cycled to: {focusedInteractable?.InteractionName ?? "None"}");
        }
    }
    
    /// <summary>
    /// 更新聚焦的交互對象
    /// </summary>
    private void UpdateFocusedInteractable()
    {
        IInteractable newFocused = null;
        
        if (nearbyInteractables.Count > 0)
        {
            // Clamp focused index
            focusedIndex = Mathf.Clamp(focusedIndex, 0, nearbyInteractables.Count - 1);
            newFocused = nearbyInteractables[focusedIndex];
        }
        else
        {
            focusedIndex = 0;
        }
        
        if (focusedInteractable != newFocused)
        {
            focusedInteractable = newFocused;
            OnFocusedInteractableChanged?.Invoke(focusedInteractable);
            
            if (showDebugLog)
            {
                Debug.Log($"Focused interactable changed to: {focusedInteractable?.InteractionName ?? "None"}");
            }
        }
    }
    
    /// <summary>
    /// 執行交互
    /// </summary>
    private void PerformInteraction()
    {
        if (focusedInteractable == null || !focusedInteractable.CanInteract)
            return;
            
        if (showDebugLog)
        {
            Debug.Log($"Interacting with: {focusedInteractable.InteractionName}");
        }
        
        focusedInteractable.Interact(gameObject);
        OnInteractionPerformed?.Invoke(focusedInteractable);
    }
    
    #region Public API
    
    /// <summary>
    /// 獲取當前聚焦的交互對象
    /// </summary>
    public IInteractable GetFocusedInteractable()
    {
        return focusedInteractable;
    }
    
    /// <summary>
    /// 獲取附近的所有交互對象
    /// </summary>
    public List<IInteractable> GetNearbyInteractables()
    {
        return new List<IInteractable>(nearbyInteractables);
    }
    
    /// <summary>
    /// 手動設置聚焦對象
    /// </summary>
    public void SetFocusedInteractable(IInteractable interactable)
    {
        if (nearbyInteractables.Contains(interactable))
        {
            focusedIndex = nearbyInteractables.IndexOf(interactable);
            UpdateFocusedInteractable();
        }
    }
    
    /// <summary>
    /// 強制刷新交互檢測
    /// </summary>
    public void RefreshInteractables()
    {
        DetectInteractables();
    }
    
    #endregion
    
    #region Debug
    
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos)
            return;
            
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Draw individual interaction ranges
        if (nearbyInteractables != null)
        {
            foreach (var interactable in nearbyInteractables)
            {
                if (interactable != null)
                {
                    Gizmos.color = interactable == focusedInteractable ? Color.green : Color.cyan;
                    Gizmos.DrawWireSphere(interactable.GetTransform().position, interactable.InteractionDistance);
                    
                    // Draw line to focused interactable
                    if (interactable == focusedInteractable)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(transform.position, interactable.GetTransform().position);
                    }
                }
            }
        }
    }
    
    #endregion
}
