using UnityEngine;

/// <summary>
/// 對話 NPC
/// </summary>
public class DialogueNPC : InteractableObject
{
    [Header("Dialogue Settings")]
    [SerializeField] private string npcName = "NPC";
    [SerializeField] private string[] dialogueLines;
    [SerializeField] private bool canRepeatDialogue = true;
    [SerializeField] private float dialogueCooldown = 1f;
    
    private bool hasSpokenBefore = false;
    private float lastDialogueTime = 0f;
    
    private void Start()
    {
        // Set interaction settings
        SetInteractionName(npcName);
        SetInteractionPrompt($"按 F 與 {npcName} 對話");
    }
    
    public override void Interact(GameObject player)
    {
        if (!CanInteract)
            return;
            
        // Check cooldown
        if (Time.time - lastDialogueTime < dialogueCooldown)
            return;
            
        // Check if can repeat dialogue
        if (hasSpokenBefore && !canRepeatDialogue)
        {
            ShowNoMoreDialogue();
            return;
        }
        
        StartDialogue(player);
        base.Interact(player);
    }
    
    private void StartDialogue(GameObject player)
    {
        lastDialogueTime = Time.time;
        hasSpokenBefore = true;
        
        Debug.Log($"Starting dialogue with {npcName}");
        
        // Simple dialogue display (in a real game, you'd use a proper dialogue system)
        if (dialogueLines != null && dialogueLines.Length > 0)
        {
            foreach (string line in dialogueLines)
            {
                Debug.Log($"{npcName}: {line}");
            }
        }
        
        // Here you would typically:
        // - Open dialogue UI
        // - Display dialogue text
        // - Handle player responses
        // - Manage dialogue flow
        
        ShowDialogueUI();
    }
    
    private void ShowDialogueUI()
    {
        // Find and show dialogue UI
        var dialogueUI = FindObjectOfType<DialogueUI>();
        if (dialogueUI != null)
        {
            dialogueUI.ShowDialogue(npcName, dialogueLines);
        }
        else
        {
            Debug.Log("No DialogueUI found - displaying in console");
        }
    }
    
    private void ShowNoMoreDialogue()
    {
        Debug.Log($"{npcName} has nothing more to say.");
        // Could show a brief UI message
    }
    
    protected override void OnHighlightChanged(bool highlighted)
    {
        // Custom highlight for NPCs
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            if (highlighted)
            {
                renderer.material.color = Color.yellow;
            }
            else
            {
                renderer.material.color = Color.white;
            }
        }
    }
}

/// <summary>
/// 簡單的對話 UI（示例）
/// </summary>
public class DialogueUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMPro.TextMeshProUGUI nameText;
    [SerializeField] private TMPro.TextMeshProUGUI dialogueText;
    [SerializeField] private UnityEngine.UI.Button continueButton;
    
    private string[] currentDialogue;
    private int currentLineIndex = 0;
    private bool isDialogueActive = false;
    
    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
            
        if (continueButton != null)
            continueButton.onClick.AddListener(NextLine);
    }
    
    public void ShowDialogue(string speakerName, string[] lines)
    {
        if (lines == null || lines.Length == 0)
            return;
            
        currentDialogue = lines;
        currentLineIndex = 0;
        isDialogueActive = true;
        
        if (nameText != null)
            nameText.text = speakerName;
            
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
            
        DisplayCurrentLine();
        
        // Pause game or disable player movement
        Time.timeScale = 0f;
    }
    
    private void DisplayCurrentLine()
    {
        if (currentDialogue != null && currentLineIndex < currentDialogue.Length)
        {
            if (dialogueText != null)
                dialogueText.text = currentDialogue[currentLineIndex];
        }
    }
    
    public void NextLine()
    {
        currentLineIndex++;
        
        if (currentLineIndex < currentDialogue.Length)
        {
            DisplayCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }
    
    private void EndDialogue()
    {
        isDialogueActive = false;
        
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
            
        // Resume game
        Time.timeScale = 1f;
    }
    
    private void Update()
    {
        // Allow space or enter to continue dialogue
        if (isDialogueActive && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            NextLine();
        }
    }
}
