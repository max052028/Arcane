using UnityEngine;

/// <summary>
/// 可交互對象接口
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// 交互對象的顯示名稱
    /// </summary>
    string InteractionName { get; }
    
    /// <summary>
    /// 交互提示文本（如 "按 F 拾取", "按 F 對話" 等）
    /// </summary>
    string InteractionPrompt { get; }
    
    /// <summary>
    /// 是否可以交互
    /// </summary>
    bool CanInteract { get; }
    
    /// <summary>
    /// 交互距離
    /// </summary>
    float InteractionDistance { get; }
    
    /// <summary>
    /// 交互優先級（數字越大優先級越高）
    /// </summary>
    int InteractionPriority { get; }
    
    /// <summary>
    /// 執行交互
    /// </summary>
    /// <param name="player">執行交互的玩家</param>
    void Interact(GameObject player);
    
    /// <summary>
    /// 當玩家進入交互範圍時調用
    /// </summary>
    /// <param name="player">進入範圍的玩家</param>
    void OnEnterInteractionRange(GameObject player);
    
    /// <summary>
    /// 當玩家離開交互範圍時調用
    /// </summary>
    /// <param name="player">離開範圍的玩家</param>
    void OnExitInteractionRange(GameObject player);
    
    /// <summary>
    /// 獲取交互對象的Transform
    /// </summary>
    Transform GetTransform();
}
