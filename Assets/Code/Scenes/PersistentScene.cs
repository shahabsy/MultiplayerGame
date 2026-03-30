using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.Diagnostics;
using System.Collections;

public class PersistentScene : MonoBehaviour
{
    [Header("Camera")]
    public Camera MainCamera;

    [Header("UI Elements")]
    public GameObject HeaderLayer;
    public GameObject FooterLayer;
    public Button SettingsBtn;
    public Button DiscordBtn;

    //[Header("Multiplayer")]
    //[SerializeField] private MultiplayerConnectionManager _multiplayerConnectionManager;

    [Header("Loading Screen")]
    public TextMeshProUGUI LoadingText;
    public GameObject LoadingTextContainer;

    private float m_LastLoadingTextStateChange;
    private int m_LoadingTextState;
    private const float LOADING_TEXT_STATE_DURATION = .5f;
    private const float LOADING_TEXT_MAX_PERIODS = 3;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        // Basic Game Settings
        Application.targetFrameRate = 60;
        Application.runInBackground = true;

        // Initialize required managers
        if (GameInstanceManager.Instance != null)
        {
            GameInstanceManager.Instance.InitCamera(MainCamera);
            GameInstanceManager.Instance.InitLoadingScreen(LoadingTextContainer);
        }
        StartCoroutine(LoadMainMenu());

        //Utils.LoadScene("MissionControlMenu");
    }

    private IEnumerator LoadMainMenu()
    {
        // Show loading screen
        if (GameInstanceManager.Instance != null)
        {
            GameInstanceManager.Instance.ShowLoadingScreen();
        }
        else if (LoadingTextContainer != null)
        {
            LoadingTextContainer.SetActive(true);
        }

        SceneManager.LoadSceneAsync("MissionControlMenu");
        if (GameInstanceManager.Instance != null)
        {
            GameInstanceManager.Instance.HideLoadingScreen();
        }
        else if (LoadingTextContainer != null)
        {
            LoadingTextContainer.SetActive(false);
        }

        yield return null; // Wait a frame to ensure the scene starts loading
    }
}
