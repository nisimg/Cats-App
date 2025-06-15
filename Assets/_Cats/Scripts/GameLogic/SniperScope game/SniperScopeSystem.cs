using UnityEngine;
using UnityEngine.UI;

public class SniperScopeSystem : MonoBehaviour
{
    [Header("Scope UI References")]
    public GameObject scopeOverlay;          
    public Image scopeCircle;               
    public Image scopeBorder;               
    public Image crosshair;                 // Crosshair/reticle image
    public CanvasGroup scopeCanvasGroup;    // For fade in/out effects
    
    [Header("Scope Settings")]
    public float scopeFadeSpeed = 5f;
    public Color scopeBorderColor = Color.black;
    public float scopeCircleSize = 300f;
    
    [Header("Crosshair Settings")]
    public Sprite[] crosshairSprites;       // Different reticle options
    public Color crosshairColor = Color.red;
    public float crosshairSize = 50f;
    
    [Header("Scope Effects")]
    public bool enableBreathing = true;
    public float breathingIntensity = 0.5f;
    public float breathingSpeed = 1.5f;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip scopeInSound;
    public AudioClip scopeOutSound;
    
    // Private variables
    private bool isScopeActive = false;
    private Camera playerCamera;
    private float originalFOV;
    private Vector3 originalCrosshairPos;
    
    void Start()
    {
        SetupScopeUI();
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        
        originalFOV = playerCamera.fieldOfView;
        
        if (scopeOverlay)
            scopeOverlay.SetActive(false);
    }
    
    void SetupScopeUI()
    {
        if (scopeOverlay == null)
        {
            Debug.LogError("SniperScopeSystem: scopeOverlay is not assigned! Please assign the scope UI elements in the inspector.");
            return;
        }
        
        if (crosshair && crosshairSprites.Length > 0)
        {
            crosshair.sprite = crosshairSprites[0];
            crosshair.color = crosshairColor;
            originalCrosshairPos = crosshair.transform.localPosition;
        }
        
        if (scopeCircle)
        {
            scopeCircle.rectTransform.sizeDelta = Vector2.one * scopeCircleSize;
        }
        
        if (scopeBorder)
        {
            scopeBorder.color = scopeBorderColor;
        }
        
        if (scopeCanvasGroup == null && scopeOverlay != null)
        {
            scopeCanvasGroup = scopeOverlay.GetComponent<CanvasGroup>();
            if (scopeCanvasGroup == null)
            {
                Debug.LogWarning("SniperScopeSystem: No CanvasGroup found on scopeOverlay. Adding one for fade effects.");
                scopeCanvasGroup = scopeOverlay.AddComponent<CanvasGroup>();
            }
        }
    }
    

    
    void Update()
    {
        if (isScopeActive && enableBreathing)
        {
            HandleBreathingEffect();
        }
    }
    
    void HandleBreathingEffect()
    {
        if (crosshair == null) return;
        
        float breathX = Mathf.Sin(Time.time * breathingSpeed) * breathingIntensity;
        float breathY = Mathf.Cos(Time.time * breathingSpeed * 1.3f) * breathingIntensity * 0.7f;
        
        Vector3 breathOffset = new Vector3(breathX, breathY, 0);
        crosshair.transform.localPosition = originalCrosshairPos + breathOffset;
    }
    
    public void EnterScope()
    {
        if (isScopeActive) return;
        
        isScopeActive = true;
        
        if (scopeOverlay)
            scopeOverlay.SetActive(true);
        
        if (audioSource && scopeInSound)
            audioSource.PlayOneShot(scopeInSound);
        
        if (scopeCanvasGroup)
            StartCoroutine(FadeScope(1f));
        
        Cursor.visible = false;
    }
    
    public void ExitScope()
    {
        if (!isScopeActive) return;
        
        isScopeActive = false;
        
        if (audioSource && scopeOutSound)
            audioSource.PlayOneShot(scopeOutSound);
        
        if (scopeCanvasGroup)
        {
            StartCoroutine(FadeScope(0f));
        }
        else
        {
            if (scopeOverlay)
                scopeOverlay.SetActive(false);
        }
        
        if (crosshair)
            crosshair.transform.localPosition = originalCrosshairPos;
    }
    
    System.Collections.IEnumerator FadeScope(float targetAlpha)
    {
        float startAlpha = scopeCanvasGroup.alpha;
        float elapsed = 0f;
        float duration = 1f / scopeFadeSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            scopeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }
        
        scopeCanvasGroup.alpha = targetAlpha;
        
        if (targetAlpha <= 0f && scopeOverlay)
            scopeOverlay.SetActive(false);
    }
    
    public void ChangeCrosshair(int index)
    {
        if (crosshair && crosshairSprites.Length > index && index >= 0)
        {
            crosshair.sprite = crosshairSprites[index];
        }
    }
    
    public void SetCrosshairColor(Color newColor)
    {
        crosshairColor = newColor;
        if (crosshair)
            crosshair.color = crosshairColor;
    }
    
    public bool IsScopeActive => isScopeActive;
}
