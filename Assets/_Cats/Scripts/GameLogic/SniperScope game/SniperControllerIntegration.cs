using UnityEngine;

public class SniperControllerIntegration : MonoBehaviour
{
    [Header("Components")]
    public MountedSniperController sniperController;
    public SniperScopeSystem scopeSystem;
    public Camera playerCamera;
    
    [Header("Zoom Settings")]
    public float normalFOV = 60f;
    public float scopedFOV = 15f;
    public float zoomSpeed = 3f;
    
    [Header("Input")]
    public KeyCode scopeKey = KeyCode.Mouse1;
    
    private bool isScoped = false;
    
    void Start()
    {
        // Get components if not assigned
        if (sniperController == null)
            sniperController = GetComponent<MountedSniperController>();
        
        if (scopeSystem == null)
            scopeSystem = GetComponent<SniperScopeSystem>();
        
        if (playerCamera == null)
            playerCamera = Camera.main;
        
        // Set initial FOV
        if (playerCamera)
            playerCamera.fieldOfView = normalFOV;
    }
    
    void Update()
    {
        HandleScopeInput();
    }
    
    void HandleScopeInput()
    {
        if (Input.GetKeyDown(scopeKey))
        {
            if (isScoped)
                ExitScope();
            else
                EnterScope();
        }
    }
    
    void EnterScope()
    {
        isScoped = true;
        
        // Activate scope UI
        if (scopeSystem)
            scopeSystem.EnterScope();
        
        // Zoom camera
        StartCoroutine(SmoothZoom(scopedFOV));
    }
    
    void ExitScope()
    {
        isScoped = false;
        
        // Deactivate scope UI
        if (scopeSystem)
            scopeSystem.ExitScope();
        
        // Zoom out camera
        StartCoroutine(SmoothZoom(normalFOV));
    }
    
    System.Collections.IEnumerator SmoothZoom(float targetFOV)
    {
        if (playerCamera == null) yield break;
        
        float startFOV = playerCamera.fieldOfView;
        float elapsed = 0f;
        float duration = 1f / zoomSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            playerCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
            yield return null;
        }
        
        playerCamera.fieldOfView = targetFOV;
    }
    
    // Public properties
    public bool IsScoped => isScoped;
}