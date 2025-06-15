using UnityEditor.MemoryProfiler;
using UnityEngine;

public class MountedSniperController : MonoBehaviour
{
    [Header("Mouse Sensitivity")]
    public float mouseSensitivity = 2f;
    public float smoothing = 2f;
    
    [Header("Rotation Limits")]
    public float maxVerticalAngle = 30f;   // How far up you can look
    public float minVerticalAngle = -20f;  // How far down you can look
    public float maxHorizontalAngle = 45f; // How far left/right you can turn
    
    [Header("Scope Settings")]
    public KeyCode scopeKey = KeyCode.Mouse1;
    public float normalFOV = 60f;
    public float scopedFOV = 15f;
    public float scopeSpeed = 3f;
    
    // Private variables
    private Vector2 mouseLook;
    private Vector2 smoothV;
    private Camera playerCamera;
    private bool isScoped = false;
    private float startingHorizontalRotation;
    [SerializeField] private GameObject gunObject;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
            
        Cursor.lockState = CursorLockMode.Locked;
        
        startingHorizontalRotation = transform.eulerAngles.y;
    }
    
    void Update()
    {
        HandleMouseLook();
        HandleScoping();
    }
    
    void HandleMouseLook()
    {
        // Get mouse input
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        mouseDelta = Vector2.Scale(mouseDelta, new Vector2(mouseSensitivity, mouseSensitivity));
        
        // Smooth the mouse movement
        smoothV.x = Mathf.Lerp(smoothV.x, mouseDelta.x, 1f / smoothing);
        smoothV.y = Mathf.Lerp(smoothV.y, mouseDelta.y, 1f / smoothing);
        mouseLook += smoothV;
        
        // Apply rotation limits
        mouseLook.y = Mathf.Clamp(mouseLook.y, minVerticalAngle, maxVerticalAngle);
        
        // Calculate horizontal rotation relative to starting position
        float targetHorizontalRotation = startingHorizontalRotation + mouseLook.x;
        float clampedHorizontalRotation = Mathf.Clamp(targetHorizontalRotation, 
            startingHorizontalRotation - maxHorizontalAngle, 
            startingHorizontalRotation + maxHorizontalAngle);
        
        mouseLook.x = clampedHorizontalRotation - startingHorizontalRotation;
        
        transform.localRotation = Quaternion.AngleAxis(mouseLook.x, Vector3.up);
        playerCamera.transform.localRotation = Quaternion.AngleAxis(-mouseLook.y, Vector3.right);
    }
    
    void HandleScoping()
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
        StartCoroutine(SmoothZoom(scopedFOV));
    }
    
    void ExitScope()
    {
        isScoped = false;
        StartCoroutine(SmoothZoom(normalFOV));
    }
    
    System.Collections.IEnumerator SmoothZoom(float targetFOV)
    {
        float startFOV = playerCamera.fieldOfView;
        float elapsed = 0f;
        
        while (elapsed < 1f / scopeSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed * scopeSpeed;
            playerCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
            yield return null;
        }
        
        playerCamera.fieldOfView = targetFOV;
        gunObject.SetActive(!isScoped);
    }
    
    public bool IsScoped => isScoped;
}


public interface IDamageable
{
    void TakeDamage(float damage);
}