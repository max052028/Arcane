using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 基礎可交互對象
/// </summary>
public class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private string interactionName = "Object";
    [SerializeField] private string interactionPrompt = "按 F 交互";
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private int interactionPriority = 0;
    [SerializeField] private bool canInteract = true;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject highlightEffect;
    [SerializeField] private bool autoToggleHighlight = true;
    
    [Header("Events")]
    public UnityEvent<GameObject> OnInteracted;
    public UnityEvent<GameObject> OnPlayerEnterRange;
    public UnityEvent<GameObject> OnPlayerExitRange;
    
    private bool isPlayerInRange = false;
    private bool isHighlighted = false;
    
    #region IInteractable Implementation
    
    public string InteractionName => interactionName;
    public string InteractionPrompt => interactionPrompt;
    public bool CanInteract => canInteract;
    public float InteractionDistance => interactionDistance;
    public int InteractionPriority => interactionPriority;
    
    public virtual void Interact(GameObject player)
    {
        if (!CanInteract)
            return;
            
        Debug.Log($"Interacting with {InteractionName}");
        OnInteracted?.Invoke(player);
    }
    
    public virtual void OnEnterInteractionRange(GameObject player)
    {
        isPlayerInRange = true;
        OnPlayerEnterRange?.Invoke(player);
        
        if (autoToggleHighlight)
        {
            SetHighlight(true);
        }
        
        Debug.Log($"Player entered range of {InteractionName}");
    }
    
    public virtual void OnExitInteractionRange(GameObject player)
    {
        isPlayerInRange = false;
        OnPlayerExitRange?.Invoke(player);
        
        if (autoToggleHighlight)
        {
            SetHighlight(false);
        }
        
        Debug.Log($"Player exited range of {InteractionName}");
    }
    
    public Transform GetTransform()
    {
        return transform;
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// 設置是否可以交互
    /// </summary>
    public void SetCanInteract(bool canInteract)
    {
        this.canInteract = canInteract;
    }
    
    /// <summary>
    /// 設置交互名稱
    /// </summary>
    public void SetInteractionName(string name)
    {
        this.interactionName = name;
    }
    
    /// <summary>
    /// 設置交互提示
    /// </summary>
    public void SetInteractionPrompt(string prompt)
    {
        this.interactionPrompt = prompt;
    }
    
    /// <summary>
    /// 設置高亮效果
    /// </summary>
    public void SetHighlight(bool highlighted)
    {
        if (isHighlighted == highlighted)
            return;
            
        isHighlighted = highlighted;
        
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(highlighted);
        }
        
        // Call virtual method for custom highlight logic
        OnHighlightChanged(highlighted);
    }
    
    /// <summary>
    /// 檢查玩家是否在範圍內
    /// </summary>
    public bool IsPlayerInRange()
    {
        return isPlayerInRange;
    }
    
    #endregion
    
    #region Virtual Methods for Override
    
    /// <summary>
    /// 當高亮狀態改變時調用，子類可重寫此方法實現自定義高亮效果
    /// </summary>
    protected virtual void OnHighlightChanged(bool highlighted)
    {
        // Override in derived classes for custom highlight effects
    }
    
    /// <summary>
    /// 驗證交互條件，子類可重寫此方法添加額外的交互條件
    /// </summary>
    protected virtual bool ValidateInteraction(GameObject player)
    {
        return CanInteract;
    }
    
    #endregion
    
    #region Debug
    
    private void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = CanInteract ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, InteractionDistance);
        
        // Draw name label
        Gizmos.color = Color.white;
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, InteractionName);
        #endif
    }
    
    #endregion
}
