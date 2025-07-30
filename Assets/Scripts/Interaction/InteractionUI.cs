using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 交互 UI 管理器
/// </summary>
public class InteractionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject interactionPanel;
    [SerializeField] private Transform interactionListParent;
    [SerializeField] private GameObject interactionItemPrefab;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Settings")]
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color focusedColor = Color.yellow;
    [SerializeField] private string interactionKeyName = "F";
    
    [Header("Layout")]
    [SerializeField] private float itemSpacing = 5f;
    [SerializeField] private int maxVisibleItems = 5;
    
    private List<InteractionUIItem> uiItems = new List<InteractionUIItem>();
    private InteractionSystem interactionSystem;
    private bool isVisible = false;
    private float targetAlpha = 0f;
    
    [System.Serializable]
    private class InteractionUIItem
    {
        public GameObject gameObject;
        public TextMeshProUGUI nameText;
        public Image backgroundImage;
        public IInteractable interactable;
        
        public InteractionUIItem(GameObject go, TextMeshProUGUI text, Image bg, IInteractable interactable)
        {
            this.gameObject = go;
            this.nameText = text;
            this.backgroundImage = bg;
            this.interactable = interactable;
        }
    }
    
    private void Start()
    {
        // Find interaction system
        interactionSystem = FindObjectOfType<InteractionSystem>();
        if (interactionSystem == null)
        {
            Debug.LogError("InteractionUI: No InteractionSystem found in scene!");
            enabled = false;
            return;
        }
        
        // Subscribe to events
        interactionSystem.OnInteractableListChanged.AddListener(OnInteractableListChanged);
        interactionSystem.OnFocusedInteractableChanged.AddListener(OnFocusedInteractableChanged);
        
        // Initialize UI
        InitializeUI();
        SetVisible(false);
    }
    
    private void Update()
    {
        // Smooth fade in/out
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (interactionSystem != null)
        {
            interactionSystem.OnInteractableListChanged.RemoveListener(OnInteractableListChanged);
            interactionSystem.OnFocusedInteractableChanged.RemoveListener(OnFocusedInteractableChanged);
        }
    }
    
    /// <summary>
    /// 初始化 UI
    /// </summary>
    private void InitializeUI()
    {
        if (interactionPanel != null)
            interactionPanel.SetActive(false);
            
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
            
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
        canvasGroup.alpha = 0f;
    }
    
    /// <summary>
    /// 當交互對象列表改變時調用
    /// </summary>
    private void OnInteractableListChanged(List<IInteractable> interactables)
    {
        bool shouldShow = interactables.Count > 0;
        SetVisible(shouldShow);
        
        if (shouldShow)
        {
            UpdateInteractionList(interactables);
        }
    }
    
    /// <summary>
    /// 當聚焦的交互對象改變時調用
    /// </summary>
    private void OnFocusedInteractableChanged(IInteractable focusedInteractable)
    {
        UpdateFocusHighlight(focusedInteractable);
        UpdatePromptText(focusedInteractable);
    }
    
    /// <summary>
    /// 設置 UI 可見性
    /// </summary>
    private void SetVisible(bool visible)
    {
        if (isVisible == visible)
            return;
            
        isVisible = visible;
        targetAlpha = visible ? 1f : 0f;
        
        if (interactionPanel != null)
            interactionPanel.SetActive(visible);
    }
    
    /// <summary>
    /// 更新交互對象列表
    /// </summary>
    private void UpdateInteractionList(List<IInteractable> interactables)
    {
        // Clear existing items
        ClearUIItems();
        
        // Create new items
        int itemsToShow = Mathf.Min(interactables.Count, maxVisibleItems);
        for (int i = 0; i < itemsToShow; i++)
        {
            CreateUIItem(interactables[i], i);
        }
        
        // Update layout
        UpdateLayout();
    }
    
    /// <summary>
    /// 清除 UI 項目
    /// </summary>
    private void ClearUIItems()
    {
        foreach (var item in uiItems)
        {
            if (item.gameObject != null)
                DestroyImmediate(item.gameObject);
        }
        uiItems.Clear();
    }
    
    /// <summary>
    /// 創建 UI 項目
    /// </summary>
    private void CreateUIItem(IInteractable interactable, int index)
    {
        if (interactionItemPrefab == null || interactionListParent == null)
            return;
            
        GameObject itemGO = Instantiate(interactionItemPrefab, interactionListParent);
        TextMeshProUGUI nameText = itemGO.GetComponentInChildren<TextMeshProUGUI>();
        Image backgroundImage = itemGO.GetComponent<Image>();
        
        if (nameText != null)
        {
            nameText.text = interactable.InteractionName;
            nameText.color = normalColor;
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(normalColor.r, normalColor.g, normalColor.b, 0.2f);
        }
        
        var uiItem = new InteractionUIItem(itemGO, nameText, backgroundImage, interactable);
        uiItems.Add(uiItem);
    }
    
    /// <summary>
    /// 更新聚焦高亮
    /// </summary>
    private void UpdateFocusHighlight(IInteractable focusedInteractable)
    {
        foreach (var item in uiItems)
        {
            bool isFocused = item.interactable == focusedInteractable;
            Color targetColor = isFocused ? focusedColor : normalColor;
            
            if (item.nameText != null)
                item.nameText.color = targetColor;
                
            if (item.backgroundImage != null)
            {
                Color bgColor = new Color(targetColor.r, targetColor.g, targetColor.b, isFocused ? 0.5f : 0.2f);
                item.backgroundImage.color = bgColor;
            }
        }
    }
    
    /// <summary>
    /// 更新提示文本
    /// </summary>
    private void UpdatePromptText(IInteractable focusedInteractable)
    {
        if (promptText == null)
            return;
            
        if (focusedInteractable != null)
        {
            string prompt = focusedInteractable.InteractionPrompt;
            // Replace placeholder with actual key name
            prompt = prompt.Replace("F", interactionKeyName);
            promptText.text = prompt;
            promptText.gameObject.SetActive(true);
        }
        else
        {
            promptText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 更新布局
    /// </summary>
    private void UpdateLayout()
    {
        if (interactionListParent == null)
            return;
            
        // Simple vertical layout
        for (int i = 0; i < uiItems.Count; i++)
        {
            if (uiItems[i].gameObject != null)
            {
                RectTransform rectTransform = uiItems[i].gameObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    float yPos = -i * (rectTransform.rect.height + itemSpacing);
                    rectTransform.anchoredPosition = new Vector2(0, yPos);
                }
            }
        }
    }
    
    #region Public API
    
    /// <summary>
    /// 設置交互鍵名稱
    /// </summary>
    public void SetInteractionKeyName(string keyName)
    {
        interactionKeyName = keyName;
        
        // Update current prompt if needed
        if (interactionSystem != null)
        {
            UpdatePromptText(interactionSystem.GetFocusedInteractable());
        }
    }
    
    /// <summary>
    /// 設置最大可見項目數
    /// </summary>
    public void SetMaxVisibleItems(int maxItems)
    {
        maxVisibleItems = Mathf.Max(1, maxItems);
        
        // Refresh list if needed
        if (interactionSystem != null)
        {
            var interactables = interactionSystem.GetNearbyInteractables();
            if (interactables.Count > 0)
            {
                UpdateInteractionList(interactables);
            }
        }
    }
    
    #endregion
}
