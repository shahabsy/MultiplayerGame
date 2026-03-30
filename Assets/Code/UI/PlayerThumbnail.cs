using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies;
using System;

public class PlayerThumbnail : MonoBehaviour
{
    public Image PlayerIcon;

    public Image PlayerIconInactive;
    public Image PlayerIconCheckmark;
    public Button IconButton;
    public TextMeshProUGUI playerLabel;

    public Player DisplayedPlayer { get; set; }
    private int m_Index;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (IconButton != null)
        {
            IconButton.onClick.AddListener(OnIconClicked);
        }
    }

    public void InitDefaultData(int index)
    {
        m_Index = index;
        playerLabel.text = $"Player {index + 1}";

        if (PlayerIcon != null)
        {
            PlayerIcon.gameObject.SetActive(false);
        }
        if (PlayerIconInactive != null)
        {
            PlayerIconInactive.gameObject.SetActive(true);
        }
        SetCheckmark(false);
        playerLabel.gameObject.SetActive(true);
        DisplayedPlayer = null;
    }

    public void InitPlayerThumbnail(Player player)
    {
        if (PlayerIcon != null) PlayerIcon.gameObject.SetActive(true);
        if (PlayerIconInactive != null) PlayerIconInactive.gameObject.SetActive(false);

        DisplayedPlayer = player;
        UpdatePlayerThumbnailData();
        playerLabel.gameObject.SetActive(false);
    }

    private void UpdatePlayerThumbnailData()
    {
        if (playerLabel != null)        {
            playerLabel.text = DisplayedPlayer?.Name ?? "Unknown";
        }

        SetCheckmark(DisplayedPlayer != null && DisplayedPlayer.IsReadyToStartGame);
    }

    private void SetCheckmark(bool ready)
    {
        if (PlayerIconCheckmark != null)
        {
            PlayerIconCheckmark.gameObject.SetActive(ready);
        }
    }

    private void OnIconClicked()
    {
        if (DisplayedPlayer != null)
        {
            
        }
    }
}
