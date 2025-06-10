using _cats.Scripts.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _cats.Scripts.MathGame
{
    public class MathGameUI : CATSMonoBehaviour
    {
        [Header("Main Menu")]
        public GameObject mainMenuPanel;
        public Button playButton;
        public Button settingsButton;
        public Button quitButton;
        
        [Header("Game UI")]
        public GameObject gamePanel;
        public GameObject pausePanel;
        public Button pauseButton;
        public Button resumeButton;
        public Button mainMenuButton;
        
        [Header("Settings Panel")]
        public GameObject settingsPanel;
        public Slider masterVolumeSlider;
        public Slider sfxVolumeSlider;
        public Slider musicVolumeSlider;
        public Button backButton;
        
        [Header("Game Over Panel")]
        public GameObject gameOverPanel;
        public Text finalScoreText;
        public Text finalLevelText;
        public Button playAgainButton;
        public Button menuButton;
        
        [Header("Pause/Resume Effects")]
        public CanvasGroup gameCanvasGroup;
        public float pausedAlpha = 0.5f;
        
        private bool isPaused = false;
        private MathGameManager gameManager;

        public override void Start()
        {
            base.Start();
            
            gameManager = FindObjectOfType<MathGameManager>();
            SetupUI();
            ShowMainMenu();
        }

        void SetupUI()
        {
            if (playButton != null)
                playButton.onClick.AddListener(StartGame);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(ShowSettings);
            
            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);
            
            if (pauseButton != null)
                pauseButton.onClick.AddListener(PauseGame);
            
            if (resumeButton != null)
                resumeButton.onClick.AddListener(ResumeGame);
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            
            if (backButton != null)
                backButton.onClick.AddListener(CloseSettings);
            
            SetupVolumeSliders();
            
            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(PlayAgain);
            
            if (menuButton != null)
                menuButton.onClick.AddListener(ReturnToMainMenu);
        }

        void SetupVolumeSliders()
        {
            var audioManager = _manager.AudioManager;
            
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = audioManager.GetMasterVolume();
                masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            }
            
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = audioManager.GetSFXVolume();
                sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
            }
            
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = audioManager.GetMusicVolume();
                musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            }
        }

        public void ShowMainMenu()
        {
            SetPanelActive(mainMenuPanel, true);
            SetPanelActive(gamePanel, false);
            SetPanelActive(settingsPanel, false);
            SetPanelActive(gameOverPanel, false);
            SetPanelActive(pausePanel, false);
            
            _manager.AudioManager.PlayMusic("MenuMusic", 1f);
        }

        public void StartGame()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(gamePanel, true);
            
            _manager.AudioManager.PlayMusic("GameMusic", 1f);
            
            if (gameManager != null)
                gameManager.GenerateNewEquation();
            
            AnimateGameUIEntrance();
        }

        void AnimateGameUIEntrance()
        {
            if (gamePanel != null)
            {
                gamePanel.transform.localScale = Vector3.zero;
                gamePanel.transform.DOScale(Vector3.one, 0.5f)
                    .SetEase(Ease.OutBack);
            }
        }

        public void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;
            
            SetPanelActive(pausePanel, true);
            
            if (gameCanvasGroup != null)
                gameCanvasGroup.DOFade(pausedAlpha, 0.3f).SetUpdate(true);
            
            InvokeEvent(CATSEventNames.OnPause, true);
        }

        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;
            
            SetPanelActive(pausePanel, false);
            
            
            if (gameCanvasGroup != null)
                gameCanvasGroup.DOFade(1f, 0.3f);
            
            InvokeEvent(CATSEventNames.OnPause, false);
        }

        public void ShowSettings()
        {
            SetPanelActive(settingsPanel, true);
            SetPanelActive(mainMenuPanel, false);
            
            if (settingsPanel != null)
            {
                settingsPanel.transform.localScale = Vector3.zero;
                settingsPanel.transform.DOScale(Vector3.one, 0.4f)
                    .SetEase(Ease.OutBack);
            }
        }

        public void CloseSettings()
        {
            SetPanelActive(settingsPanel, false);
            SetPanelActive(mainMenuPanel, true);
        }

        public void ShowGameOver(int finalScore, int finalLevel)
        {
            SetPanelActive(gameOverPanel, true);
            
            if (finalScoreText != null)
                finalScoreText.text = $"Final Score: {finalScore}";
            
            if (finalLevelText != null)
                finalLevelText.text = $"Level Reached: {finalLevel}";
            
            if (gameOverPanel != null)
            {
                gameOverPanel.transform.localScale = Vector3.zero;
                gameOverPanel.transform.DOScale(Vector3.one, 0.5f)
                    .SetEase(Ease.OutBounce);
            }
        }

        public void PlayAgain()
        {
            SetPanelActive(gameOverPanel, false);
            StartGame();
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            isPaused = false;
            
            ShowMainMenu();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
        }

        public void SetMasterVolume(float volume)
        {
            CATSManager.Instance.AudioManager.SetMasterVolume(volume);
        }

        public void SetSFXVolume(float volume)
        {
            CATSManager.Instance.AudioManager.SetSFXVolume(volume);
        }

        public void SetMusicVolume(float volume)
        {
            CATSManager.Instance.AudioManager.SetMusicVolume(volume);
        }

        void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }

        public override void OnUpdate(float deltaTime)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    CloseSettings();
                }
                else if (gamePanel != null && gamePanel.activeSelf)
                {
                    if (isPaused)
                        ResumeGame();
                    else
                        PauseGame();
                }
            }
        }

        public void OnButtonHover(Button button)
        {
            button.transform.DOScale(1.1f, 0.2f).SetEase(Ease.OutBack);
            _manager.AudioManager.PlaySFX("ButtonHover", 0.3f);
        }

        public void OnButtonExit(Button button)
        {
            button.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        }

        public void OnButtonClick(Button button)
        {
            button.transform.DOPunchScale(Vector3.one * -0.1f, 0.2f, 10, 1f);
            _manager.AudioManager.PlaySFX("ButtonClick", 0.5f);
        }

        public void ShowNotification(string message, float duration = 2f)
        {
            GameObject notification = new GameObject("Notification");
            notification.transform.SetParent(transform, false);
            
            Text notificationText = notification.AddComponent<Text>();
            notificationText.text = message;
            notificationText.fontSize = 24;
            notificationText.color = Color.yellow;
            notificationText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform rect = notification.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.8f);
            rect.anchorMax = new Vector2(0.5f, 0.8f);
            rect.sizeDelta = new Vector2(400, 50);
            
            notification.transform.localScale = Vector3.zero;
            notification.transform.DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => 
                {
                    notification.transform.DOScale(Vector3.zero, 0.3f)
                        .SetDelay(duration)
                        .OnComplete(() => Destroy(notification));
                });
        }
    }
}