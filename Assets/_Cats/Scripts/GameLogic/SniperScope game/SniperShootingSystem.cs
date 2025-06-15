using System.Collections;
using UnityEngine;

public class SniperShootingSystem : MonoBehaviour
{
    [Header("Shooting Settings")]
    public float damage = 100f;
    public float range = 1000f;
    public float fireRate = 0.5f; // Time between shots
    public LayerMask hitLayers = -1;
    
    [Header("Accuracy")]
    public float baseAccuracy = 0.8f;
    public float scopedAccuracy = 0.98f;
    public float maxSpread = 2f; // Maximum bullet spread in degrees
    
    [Header("Ammunition")]
    public int magazineSize = 5;
    public int totalAmmo = 30;
    public float reloadTime = 3f;
    
    [Header("Recoil")]
    public float recoilForce = 2f;
    public float scopedRecoilMultiplier = 0.3f;
    public float recoilRecoverySpeed = 5f;
    
    [Header("References")]
    public Transform firePoint;
    public Camera playerCamera;
    public SniperScopeSystem scopeSystem;
    public ParticleSystem muzzleFlash;
    public LineRenderer bulletTrail;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    
    [Header("Input")]
    public KeyCode fireKey = KeyCode.Mouse0;
    public KeyCode reloadKey = KeyCode.R;
    
    [Header("Visual Effects")]
    public GameObject hitEffect;
    public GameObject bloodEffect;
    public float trailDuration = 0.1f;
    
    // Private variables
    private int currentAmmo;
    private bool isReloading = false;
    private bool canFire = true;
    private Vector2 currentRecoil;
    private Vector2 targetRecoil;
    
    // Events
    public System.Action<int, int> OnAmmoChanged; // current, total
    public System.Action OnReloadStart;
    public System.Action OnReloadComplete;
    public System.Action<float> OnDamageDealt; // damage amount
    
    void Start()
    {
        currentAmmo = magazineSize;
        
        // Get components if not assigned
        if (playerCamera == null)
            playerCamera = Camera.main;
        
        if (scopeSystem == null)
            scopeSystem = GetComponent<SniperScopeSystem>();
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        if (firePoint == null)
            firePoint = playerCamera.transform;
        
        // Setup bullet trail
        if (bulletTrail)
        {
            bulletTrail.enabled = false;
            bulletTrail.useWorldSpace = true;
        }
        
        // Initialize UI
        OnAmmoChanged?.Invoke(currentAmmo, totalAmmo);
    }
    
    void Update()
    {
        HandleInput();
        HandleRecoilRecovery();
    }
    
    void HandleInput()
    {
        // Fire input
        if (Input.GetKeyDown(fireKey))
        {
            TryFire();
        }
        
        // Reload input
        if (Input.GetKeyDown(reloadKey))
        {
            TryReload();
        }
    }
    
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
        float currentAccuracy = scopeSystem && scopeSystem.IsScopeActive ? scopedAccuracy : baseAccuracy;
        
        // Apply spread
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
        
        // Visual and audio effects
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
        
        // Auto-destroy effect after a few seconds
        Destroy(effect, 5f);
    }
    
    IEnumerator FireEffects(Vector3 startPoint, Vector3 endPoint)
    {
        // Muzzle flash
        if (muzzleFlash)
        {
            muzzleFlash.Play();
        }
        
        // Fire sound
        if (audioSource && fireSound)
        {
            audioSource.PlayOneShot(fireSound);
        }
        
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
    
    void ApplyRecoil()
    {
        float recoilAmount = recoilForce;
        
        // Reduce recoil when scoped
        if (scopeSystem && scopeSystem.IsScopeActive)
        {
            recoilAmount *= scopedRecoilMultiplier;
        }
        
        // Apply random recoil
        float recoilX = Random.Range(-recoilAmount * 0.5f, recoilAmount * 0.5f);
        float recoilY = Random.Range(recoilAmount * 0.5f, recoilAmount);
        
        targetRecoil += new Vector2(recoilX, recoilY);
        
        // Apply recoil to camera
        if (playerCamera)
        {
            playerCamera.transform.Rotate(-recoilY, recoilX, 0, Space.Self);
        }
    }
    
    void HandleRecoilRecovery()
    {
        if (targetRecoil.magnitude > 0.1f)
        {
            targetRecoil = Vector2.Lerp(targetRecoil, Vector2.zero, Time.deltaTime * recoilRecoverySpeed);
        }
    }
    
    IEnumerator FireRateControl()
    {
        canFire = false;
        yield return new WaitForSeconds(fireRate);
        canFire = true;
    }
    
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
    
    // Public methods for external access
    public void AddAmmo(int amount)
    {
        totalAmmo += amount;
        OnAmmoChanged?.Invoke(currentAmmo, totalAmmo);
    }
    
    public bool HasAmmo()
    {
        return currentAmmo > 0 || totalAmmo > 0;
    }
    
    public bool CanReload()
    {
        return !isReloading && currentAmmo < magazineSize && totalAmmo > 0;
    }
    
    public void ForceReload()
    {
        if (totalAmmo > 0)
            StartCoroutine(Reload());
    }
    
    // Public properties
    public int CurrentAmmo => currentAmmo;
    public int TotalAmmo => totalAmmo;
    public bool IsReloading => isReloading;
    public bool CanFire => canFire && !isReloading && currentAmmo > 0;
}