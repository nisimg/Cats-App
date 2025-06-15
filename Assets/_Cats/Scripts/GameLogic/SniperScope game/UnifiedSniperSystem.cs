using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UnifiedSniperSystem : MonoBehaviour
{
    [Header("Mouse Control")]
    public float mouseSensitivity = 2f;
    public float smoothing = 2f;
    
    [Header("Rotation Limits")]
    public float maxVerticalAngle = 30f;   
    public float minVerticalAngle = -20f;  
    public float maxHorizontalAngle = 45f; 
    
    [Header("Scope System")]
    public KeyCode scopeKey = KeyCode.Mouse1;
    public float normalFOV = 60f;
    public float scopedFOV = 15f;
    public float scopeSpeed = 3f;
    public GameObject scopeOverlay;
    public Image scopeCircle;
    public Image scopeBorder;
    public Image crosshair;
    public CanvasGroup scopeCanvasGroup;
    public Color crosshairColor = Color.red;
    public Sprite[] crosshairSprites;
    
    [Header("Scope Effects")]
    public bool enableBreathing = true;
    public float breathingIntensity = 0.5f;
    public float breathingSpeed = 1.5f;
    public float scopeFadeSpeed = 5f;
    
    [Header("Shooting")]
    public KeyCode fireKey = KeyCode.Mouse0;
    public KeyCode reloadKey = KeyCode.R;
    public float damage = 100f;
    public float range = 1000f;
    public float fireRate = 0.5f;
    public LayerMask hitLayers = -1;
    
    [Header("Accuracy & Recoil")]
    public float baseAccuracy = 0.8f;
    public float scopedAccuracy = 0.98f;
    public float maxSpread = 2f;
    public float recoilForce = 2f;
    public float scopedRecoilMultiplier = 0.3f;
    public float recoilRecoverySpeed = 8f;
    
    [Header("Ammunition")]
    public int magazineSize = 5;
    public int totalAmmo = 30;
    public float reloadTime = 3f;
    
    [Header("References")]
    public Transform firePoint;
    public Camera playerCamera;
    public GameObject gunObject;
    public ParticleSystem muzzleFlash;
    public LineRenderer bulletTrail;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    public AudioClip scopeInSound;
    public AudioClip scopeOutSound;
    
    [Header("Visual Effects")]
    public GameObject hitEffect;
    public GameObject bloodEffect;
    public float trailDuration = 0.1f;
    
    // Private variables - Mouse Look
    private Vector2 mouseLook;
    private Vector2 smoothV;
    private float startingHorizontalRotation;
    private Vector2 recoilOffset;
    
    // Private variables - Scope
    private bool isScoped = false;
    private Vector3 originalCrosshairPos;
    
    // Private variables - Shooting
    private int currentAmmo;
    private bool isReloading = false;
    private bool canFire = true;
    
    // Events
    public System.Action<int, int> OnAmmoChanged;
    public System.Action OnReloadStart;
    public System.Action OnReloadComplete;
    public System.Action<float> OnDamageDealt;
    
    void Start()
    {
        Initialize();
    }
    
    void Initialize()
    {
        // Setup camera
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        
        // Setup audio
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        // Setup fire point
        if (firePoint == null)
            firePoint = playerCamera.transform;
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        startingHorizontalRotation = transform.eulerAngles.y;
        
        // Initialize ammo
        currentAmmo = magazineSize;
        OnAmmoChanged?.Invoke(currentAmmo, totalAmmo);
        
        // Setup scope UI
        SetupScopeUI();
        
        // Setup bullet trail
        if (bulletTrail)
        {
            bulletTrail.enabled = false;
            bulletTrail.useWorldSpace = true;
        }
    }
    
    void SetupScopeUI()
    {
        if (scopeOverlay != null)
        {
            scopeOverlay.SetActive(false);
            
            if (scopeCanvasGroup == null)
            {
                scopeCanvasGroup = scopeOverlay.GetComponent<CanvasGroup>();
                if (scopeCanvasGroup == null)
                    scopeCanvasGroup = scopeOverlay.AddComponent<CanvasGroup>();
            }
        }
        
        if (crosshair)
        {
            if (crosshairSprites.Length > 0)
                crosshair.sprite = crosshairSprites[0];
            crosshair.color = crosshairColor;
            originalCrosshairPos = crosshair.transform.localPosition;
        }
    }
    
    void Update()
    {
        HandleInput();
        HandleMouseLook();
        HandleRecoilRecovery();
        
        if (isScoped && enableBreathing)
            HandleBreathingEffect();
    }
    
    void HandleInput()
    {
        // Scope toggle
        if (Input.GetKeyDown(scopeKey))
        {
            if (isScoped)
                ExitScope();
            else
                EnterScope();
        }
        
        // Fire
        if (Input.GetKeyDown(fireKey))
        {
            TryFire();
        }
        
        // Reload
        if (Input.GetKeyDown(reloadKey))
        {
            TryReload();
        }
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
        
        // Add recoil offset
        Vector2 finalLook = mouseLook + recoilOffset;
        
        // Apply rotation limits
        finalLook.y = Mathf.Clamp(finalLook.y, minVerticalAngle, maxVerticalAngle);
        
        // Calculate horizontal rotation with limits
        float targetHorizontalRotation = startingHorizontalRotation + finalLook.x;
        float clampedHorizontalRotation = Mathf.Clamp(targetHorizontalRotation, 
            startingHorizontalRotation - maxHorizontalAngle, 
            startingHorizontalRotation + maxHorizontalAngle);
        
        // Update actual mouse look to respect clamping
        float clampedMouseLookX = clampedHorizontalRotation - startingHorizontalRotation - recoilOffset.x;
        mouseLook.x = clampedMouseLookX;
        
        // Apply rotations
        transform.localRotation = Quaternion.AngleAxis(clampedHorizontalRotation - startingHorizontalRotation, Vector3.up);
        playerCamera.transform.localRotation = Quaternion.AngleAxis(-finalLook.y, Vector3.right);
    }
    
    #region Scope System
    
    void EnterScope()
    {
        isScoped = true;
        
        // Show scope UI
        if (scopeOverlay)
            scopeOverlay.SetActive(true);
        
        // Hide gun model
        if (gunObject)
            gunObject.SetActive(false);
        
        // Play sound
        if (audioSource && scopeInSound)
            audioSource.PlayOneShot(scopeInSound);
        
        // Fade in scope
        if (scopeCanvasGroup)
            StartCoroutine(FadeScope(1f));
        
        // Zoom camera
        StartCoroutine(SmoothZoom(scopedFOV));
        
        Cursor.visible = false;
    }
    
    void ExitScope()
    {
        isScoped = false;
        
        // Show gun model
        if (gunObject)
            gunObject.SetActive(true);
        
        // Play sound
        if (audioSource && scopeOutSound)
            audioSource.PlayOneShot(scopeOutSound);
        
        // Fade out scope
        if (scopeCanvasGroup)
            StartCoroutine(FadeScope(0f));
        else if (scopeOverlay)
            scopeOverlay.SetActive(false);
        
        // Reset crosshair
        if (crosshair)
            crosshair.transform.localPosition = originalCrosshairPos;
        
        // Zoom out camera
        StartCoroutine(SmoothZoom(normalFOV));
    }
    
    IEnumerator SmoothZoom(float targetFOV)
    {
        if (playerCamera == null) yield break;
        
        float startFOV = playerCamera.fieldOfView;
        float elapsed = 0f;
        float duration = 1f / scopeSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            playerCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
            yield return null;
        }
        
        playerCamera.fieldOfView = targetFOV;
    }
    
    IEnumerator FadeScope(float targetAlpha)
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
    
    void HandleBreathingEffect()
    {
        if (crosshair == null) return;
        
        float breathX = Mathf.Sin(Time.time * breathingSpeed) * breathingIntensity;
        float breathY = Mathf.Cos(Time.time * breathingSpeed * 1.3f) * breathingIntensity * 0.7f;
        
        Vector3 breathOffset = new Vector3(breathX, breathY, 0);
        crosshair.transform.localPosition = originalCrosshairPos + breathOffset;
    }
    
    #endregion
    
    #region Shooting System
    
    void TryFire()
    {
        if (!canFire || isReloading)
            return;
        
        if (currentAmmo <= 0)
        {
            // Empty gun sound
            if (audioSource && emptySound)
                audioSource.PlayOneShot(emptySound);
            return;
        }
        
        Fire();
    }
    
    void Fire()
    {
        // Consume ammo
        currentAmmo--;
        OnAmmoChanged?.Invoke(currentAmmo, totalAmmo);
        
        // Calculate accuracy based on scope status
        float currentAccuracy = isScoped ? scopedAccuracy : baseAccuracy;
        
        // Calculate shoot direction with spread
        Vector3 shootDirection = CalculateShootDirection(currentAccuracy);
        
        // Perform raycast
        RaycastHit hit;
        Vector3 startPoint = firePoint.position;
        Vector3 endPoint = startPoint + shootDirection * range;
        
        bool didHit = Physics.Raycast(startPoint, shootDirection, out hit, range, hitLayers);
        
        if (didHit)
        {
            endPoint = hit.point;
            HandleHit(hit);
        }
        
        // Effects
        StartCoroutine(FireEffects(startPoint, endPoint));
        
        // Apply recoil
        ApplyRecoil();
        
        // Fire rate control
        StartCoroutine(FireRateControl());
    }
    
    Vector3 CalculateShootDirection(float accuracy)
    {
        Vector3 baseDirection = playerCamera.transform.forward;
        
        // Calculate spread based on accuracy
        float spread = (1f - accuracy) * maxSpread;
        
        // Add random spread
        float spreadX = Random.Range(-spread, spread);
        float spreadY = Random.Range(-spread, spread);
        
        // Apply spread to direction
        Vector3 spreadDirection = Quaternion.Euler(spreadY, spreadX, 0) * baseDirection;
        
        return spreadDirection.normalized;
    }
    
    void HandleHit(RaycastHit hit)
    {
        // Apply damage if target has health component
        IDamageable damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            float finalDamage = damage;
            
            // Bonus damage for headshots
            if (hit.collider.CompareTag("Head"))
            {
                finalDamage *= 2f;
                Debug.Log("HEADSHOT!");
            }
            
            damageable.TakeDamage(finalDamage);
            OnDamageDealt?.Invoke(finalDamage);
            
            // Blood effect for living targets
            if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
            {
                SpawnEffect(bloodEffect, hit.point, hit.normal);
            }
        }
        
        // General hit effect
        SpawnEffect(hitEffect, hit.point, hit.normal);
        
        Debug.Log($"Hit: {hit.collider.name} at {hit.point}");
    }
    
    void SpawnEffect(GameObject effectPrefab, Vector3 position, Vector3 normal)
    {
        if (effectPrefab == null) return;
        
        GameObject effect = Instantiate(effectPrefab, position, Quaternion.LookRotation(normal));
        Destroy(effect, 5f);
    }
    
    IEnumerator FireEffects(Vector3 startPoint, Vector3 endPoint)
    {
        // Muzzle flash
        if (muzzleFlash)
            muzzleFlash.Play();
        
        // Fire sound
        if (audioSource && fireSound)
            audioSource.PlayOneShot(fireSound);
        
        // Bullet trail
        if (bulletTrail)
        {
            bulletTrail.enabled = true;
            bulletTrail.SetPosition(0, startPoint);
            bulletTrail.SetPosition(1, endPoint);
            
            yield return new WaitForSeconds(trailDuration);
            
            bulletTrail.enabled = false;
        }
    }
    
    IEnumerator FireRateControl()
    {
        canFire = false;
        yield return new WaitForSeconds(fireRate);
        canFire = true;
    }
    
    #endregion
    
    #region Recoil System
    
    void ApplyRecoil()
    {
        float recoilAmount = recoilForce;
        
        // Reduce recoil when scoped
        if (isScoped)
            recoilAmount *= scopedRecoilMultiplier;
        
        // Calculate random recoil
        float recoilX = Random.Range(-recoilAmount * 0.5f, recoilAmount * 0.5f);
        float recoilY = Random.Range(recoilAmount * 0.5f, recoilAmount);
        
        // Apply recoil to offset
        recoilOffset += new Vector2(recoilX, recoilY);
        
        Debug.Log($"Applied recoil: X={recoilX:F2}, Y={recoilY:F2}");
    }
    
    void HandleRecoilRecovery()
    {
        // Smoothly recover from recoil
        if (recoilOffset.magnitude > 0.01f)
        {
            recoilOffset = Vector2.Lerp(recoilOffset, Vector2.zero, Time.deltaTime * recoilRecoverySpeed);
            
            // Stop recovery when close enough to zero
            if (recoilOffset.magnitude < 0.01f)
                recoilOffset = Vector2.zero;
        }
    }
    
    #endregion
    
    #region Ammunition System
    
    void TryReload()
    {
        if (isReloading || currentAmmo >= magazineSize || totalAmmo <= 0)
            return;
        
        StartCoroutine(Reload());
    }
    
    IEnumerator Reload()
    {
        isReloading = true;
        OnReloadStart?.Invoke();
        
        // Play reload sound
        if (audioSource && reloadSound)
            audioSource.PlayOneShot(reloadSound);
        
        Debug.Log("Reloading...");
        
        yield return new WaitForSeconds(reloadTime);
        
        // Calculate ammo to reload
        int ammoNeeded = magazineSize - currentAmmo;
        int ammoToReload = Mathf.Min(ammoNeeded, totalAmmo);
        
        currentAmmo += ammoToReload;
        totalAmmo -= ammoToReload;
        
        isReloading = false;
        OnReloadComplete?.Invoke();
        OnAmmoChanged?.Invoke(currentAmmo, totalAmmo);
        
        Debug.Log("Reload complete!");
    }
    
    #endregion
    
    #region Public Methods
    
    public void AddAmmo(int amount)
    {
        totalAmmo += amount;
        OnAmmoChanged?.Invoke(currentAmmo, totalAmmo);
    }
    
    public void ChangeCrosshair(int index)
    {
        if (crosshair && crosshairSprites.Length > index && index >= 0)
            crosshair.sprite = crosshairSprites[index];
    }
    
    public void SetCrosshairColor(Color newColor)
    {
        crosshairColor = newColor;
        if (crosshair)
            crosshair.color = crosshairColor;
    }
    
    #endregion
    
    #region Public Properties
    
    public bool IsScoped => isScoped;
    public int CurrentAmmo => currentAmmo;
    public int TotalAmmo => totalAmmo;
    public bool IsReloading => isReloading;
    public bool CanFire => canFire && !isReloading && currentAmmo > 0;
    public bool HasAmmo => currentAmmo > 0 || totalAmmo > 0;
    
    #endregion
}