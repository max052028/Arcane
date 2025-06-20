using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMainUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider manaBar;
    [SerializeField] private Slider staminaBar;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject skillPanel;

    // Start is called before the first frame update
    void Start()
    {
        InitializeUI();
    }

    // Initialize UI elements
    private void InitializeUI()
    {
        healthBar.gameObject.SetActive(true);
        manaBar.gameObject.SetActive(true);
        staminaBar.gameObject.SetActive(true);
        // inventoryPanel.SetActive(false);
        // skillPanel.SetActive(false);
    }

    // Toggle inventory panel visibility
    public void ToggleInventory()
    {
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
    }

    // Toggle skill panel visibility
    public void ToggleSkillPanel()
    {
        skillPanel.SetActive(!skillPanel.activeSelf);
    }

    // Update health bar
    public void UpdateHealthBar(float healthPercentage)
    {
        if (healthBar != null)
        {
            healthBar.value = healthPercentage;
        }
    }

    // Update mana bar
    public void UpdateManaBar(float manaPercentage)
    {
        if (manaBar != null)
        {
            manaBar.value = manaPercentage;
        }
    }

    // Update stamina bar
    public void UpdateStaminaBar(float staminaPercentage)
    {
        if (staminaBar != null)
        {
            staminaBar.value = staminaPercentage;
        }
    }
}
