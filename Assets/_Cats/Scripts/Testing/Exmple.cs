using _cats.Scripts.Core;

public class ExamplePopup : CATSPopup
{
    public UnityEngine.UI.Text titleText;
    public UnityEngine.UI.Text messageText;
    public UnityEngine.UI.Button closeButton;

    private void Awake()
    {
        closeButton.onClick.AddListener(() => Close());
    }

    protected override void OnShow(object data)
    {
        if (data is PopupData popupData)
        {
            titleText.text = popupData.Title;
            messageText.text = popupData.Message;
        }
        
        // Play popup show sound
        CATSManager.Instance.AudioManager.PlaySFX("popup_show");
    }

    protected override void OnClose()
    {
        // Play popup close sound
        CATSManager.Instance.AudioManager.PlaySFX("popup_close");
    }
    
    public class PopupData
    {
        public string Title;
        public string Message;
    }
}

// Example usage in a game script