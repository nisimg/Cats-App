using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SniperSystem : MonoBehaviour
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
    
    private Vector2 mouseLook;
    private Vector2 smoothV;
    private float startingHorizontalRotation;
    private Vector2 recoilOffset;
    
    private bool isScoped = false;
    private Vector3 originalCrosshairPos;
    
    private int currentAmmo;
    private bool isReloading = false;
    private bool canFire = true;
    

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
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        if (firePoint == null)
            firePoint = playerCamera.transform;
        
        Cursor.lockState = CursorLockMode.Locked;
        startingHorizontalRotation = transform.eulerAngles.y;
        
        currentAmmo = magazineSize;
        OnAmmoChanged?.Invoke(currentAmmo, totalAmmo);
        
        SetupScopeUI();
        
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
        if (Input.GetKeyDown(scopeKey))
        {
            if (isScoped)
                ExitScope();
            else
                EnterScope();
        }
        
        if (Input.GetKeyDown(fireKey))
        {
            TryFire();
        }
        
        if (Input.GetKeyDown(reloadKey))
        {
            TryReload();
        }
    }
    
    void HandleMouseLook()
    {
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        mouseDelta = Vector2.Scale(mouseDelta, new Vector2(mouseSensitivity, mouseSensitivity));
        
        smoothV.x = Mathf.Lerp(smoothV.x, mouseDelta.x, 1f / smoothing);
        smoothV.y = Mathf.Lerp(smoothV.y, mouseDelta.y, 1f / smoothing);
        mouseLook += smoothV;
        
        Vector2 finalLook = mouseLook + recoilOffset;
        
        finalLook.y = Mathf.Clamp(finalLook.y, minVerticalAngle, maxVerticalAngle);
        
        float targetHorizontalRotation = startingHorizontalRotation + finalLook.x;
        float clampedHorizontalRotation = Mathf.Clamp(targetHorizontalRotation, 
            startingHorizontalRotation - maxHorizontalAngle, 
            startingHorizontalRotation + maxHorizontalAngle);
        
        float clampedMouseLookX = clampedHorizontalRotation - startingHorizontalRotation - recoilOffset.x;
        mouseLook.x = clampedMouseLookX;
        
        transform.localRotation = Quaternion.AngleAxis(clampedHorizontalRotation - startingHorizontalRotation, Vector3.up);
        playerCamera.transform.localRotation = Quaternion.AngleAxis(-finalLook.y, Vector3.right);
    }
    
    #region Scope System
    
    void EnterScope()
    {
        isScoped = true;
        
        if (scopeOverlay)
            scopeOverlay.SetActive(true);
        
        if (gunObject)
            gunObject.SetActive(false);
        
        if (audioSource && scopeInSound)
            audioSource.PlayOneShot(scopeInSound);
        
        if (scopeCanvasGroup)
            StartCoroutine(FadeScope(1f));
        
        StartCoroutine(SmoothZoom(scopedFOV));
        
        Cursor.visible = false;
    }
    
    void ExitScope()
    {
        isScoped = false;
        
        if (gunObject)
            gunObject.SetActive(true);
        
        if (audioSource && scopeOutSound)
            audioSource.PlayOneShot(scopeOutSound);
        
        if (scopeCanvasGroup)
            StartCoroutine(FadeScope(0f));
        else if (scopeOverlay)
            scopeOverlay.SetActive(false);
        
        if (crosshair)
            crosshair.transform.localPosition = originalCrosshairPos;
        
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
            if (audioSource && emptySound)
                audioSource.PlayOneShot(emptySound);
            return;
        }
        
        Fire();
    }
    
    void Fire()
    {
        currentAmmo--;
        OnAmmoChanged?.Invoke(currentAmmo, totalAmmo);
        
        float currentAccuracy = isScoped ? scopedAccuracy : baseAccuracy;
        
        Vector3 shootDirection = CalculateShootDirection(currentAccuracy);
        
        RaycastHit hit;
        Vector3 startPoint = firePoint.position;
        Vector3 endPoint = startPoint + shootDirection * range;
        
        bool didHit = Physics.Raycast(startPoint, shootDirection, out hit, range, hitLayers);
        
        if (didHit)
        {
            endPoint = hit.point;
            HandleHit(hit);
        }
        
        StartCoroutine(FireEffects(startPoint, endPoint));
        
        ApplyRecoil();
        
        StartCoroutine(FireRateControl());
    }
    
    Vector3 CalculateShootDirection(float accuracy)
    {
        Vector3 baseDirection = playerCamera.transform.forward;
        
        float spread = (1f - accuracy) * maxSpread;
        
        float spreadX = Random.Range(-spread, spread);
        float spreadY = Random.Range(-spread, spread);
        
        Vector3 spreadDirection = Quaternion.Euler(spreadY, spreadX, 0) * baseDirection;
        
        return spreadDirection.normalized;
    }
    
    void HandleHit(RaycastHit hit)
    {
        IDamageable damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            float finalDamage = damage;
            
            if (hit.collider.CompareTag("Head"))
            {
                finalDamage *= 2f;
                Debug.Log("HEADSHOT!");
            }
            
            damageable.TakeDamage(finalDamage);
            OnDamageDealt?.Invoke(finalDamage);
            
            if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
            {
                SpawnEffect(bloodEffect, hit.point, hit.normal);
            }
        }
        
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
        if (muzzleFlash)
            muzzleFlash.Play();
        
        if (audioSource && fireSound)
            audioSource.PlayOneShot(fireSound);
        
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
        
        if (isScoped)
            recoilAmount *= scopedRecoilMultiplier;
        
        float recoilX = Random.Range(-recoilAmount * 0.5f, recoilAmount * 0.5f);
        float recoilY = Random.Range(recoilAmount * 0.5f, recoilAmount);
        
        recoilOffset += new Vector2(recoilX, recoilY);
        
        Debug.Log($"Applied recoil: X={recoilX:F2}, Y={recoilY:F2}");
    }
    
    void HandleRecoilRecovery()
    {
        if (recoilOffset.magnitude > 0.01f)
        {
            recoilOffset = Vector2.Lerp(recoilOffset, Vector2.zero, Time.deltaTime * recoilRecoverySpeed);
            
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
        
        if (audioSource && reloadSound)
            audioSource.PlayOneShot(reloadSound);
        
        Debug.Log("Reloading...");
        
        yield return new WaitForSeconds(reloadTime);
        
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