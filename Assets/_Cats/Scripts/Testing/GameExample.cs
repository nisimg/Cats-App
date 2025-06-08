using _cats.Scripts.Core;
using UnityEngine;

public class GameExample : MonoBehaviour
{
    void Start()
    {
        // Initialize CATS Manager
        new CATSManager((success) =>
        {
            if (success)
            {
                SetupAudio();
                SetupPopups();
            }
        });
    }

    void SetupAudio()
    {
        var audioManager = CATSManager.Instance.AudioManager;
        
        // Load audio clips
        audioManager.LoadAudioClip("button_click", "Audio/SFX/button_click");
        audioManager.LoadAudioClip("popup_show", "Audio/SFX/popup_show");
        audioManager.LoadAudioClip("popup_close", "Audio/SFX/popup_close");
        audioManager.LoadAudioClip("background_music", "Audio/Music/background");
        
        // Play background music
        audioManager.PlayMusic("background_music", 2f); // 2 second fade in
    }

    void SetupPopups()
    {
        var popupManager = CATSManager.Instance.PopupManager;
        
        // Create and register a popup
        var popupPrefab = Resources.Load<ExamplePopup>("Popups/ExamplePopup");
        var popupInstance = Instantiate(popupPrefab);
        popupManager.RegisterPopup("example_popup", popupInstance);
    }

    void OnButtonClick()
    {
        // Play button sound
        CATSManager.Instance.AudioManager.PlaySFX("button_click");
        
        // Show popup
        var popupData = new ExamplePopup.PopupData
        {
            Title = "Welcome!",
            Message = "This is an example popup using CATS framework."
        };
        
        CATSManager.Instance.PopupManager.ShowPopup("example_popup", popupData, () =>
        {
            Debug.Log("Popup closed!");
        });
    }

    void OnSettingsButtonClick()
    {
        // Example of volume control
        var audioManager = CATSManager.Instance.AudioManager;
        
        // Mute toggle
        audioManager.ToggleMute();
        
        // Or set specific volumes
        audioManager.SetMasterVolume(0.8f);
        audioManager.SetSFXVolume(0.7f);
        audioManager.SetMusicVolume(0.5f);
    }
}