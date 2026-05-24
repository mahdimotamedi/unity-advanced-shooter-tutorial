using UnityEngine;

public class ShooterController : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform firePoint;
    public GameObject bulletPrefab;
    public GameObject spearPrefab;
    public GameObject gunVisual;
    public GameObject spearVisual;

    [Header("Input")]
    public KeyCode switchModeKey = KeyCode.Q;

    [Header("Gun")]
    public float gunProjectileSpeed = 55f;
    public float gunFireCooldown = 0.11f;
    public float gunSpread = 0.012f;

    [Header("Spear")]
    public float spearSpeed = 27f;
    public float spearUpArc = 3.2f;
    public float spearCooldown = 0.85f;

    [Header("Aim")]
    public LayerMask aimMask = ~0;
    public float aimDistance = 100f;

    private WeaponMode currentMode = WeaponMode.Gun;
    private float nextFireTime;

    public WeaponMode CurrentMode { get { return currentMode; } }

    private void Awake()
    {
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();

        if (firePoint == null && playerCamera != null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(playerCamera.transform);
            fp.transform.localPosition = new Vector3(0.28f, -0.2f, 0.75f);
            fp.transform.localRotation = Quaternion.identity;
            firePoint = fp.transform;
        }

        EnsureRuntimePrefabs();
        RefreshWeaponVisuals();
    }

    private void Update()
    {
        if (Input.GetKeyDown(switchModeKey) || Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f)
        {
            ToggleMode();
        }

        if (Input.GetMouseButton(0))
        {
            TryFire();
        }
    }

    public void ToggleMode()
    {
        currentMode = currentMode == WeaponMode.Gun ? WeaponMode.Spear : WeaponMode.Gun;
        RefreshWeaponVisuals();
    }

    private void TryFire()
    {
        if (Time.time < nextFireTime) return;

        if (currentMode == WeaponMode.Gun)
        {
            FireGun();
            nextFireTime = Time.time + gunFireCooldown;
        }
        else
        {
            ThrowSpear();
            nextFireTime = Time.time + spearCooldown;
        }
    }

    private void FireGun()
    {
        Vector3 direction = GetAimDirection();
        direction += playerCamera.transform.right * Random.Range(-gunSpread, gunSpread);
        direction += playerCamera.transform.up * Random.Range(-gunSpread, gunSpread);
        direction.Normalize();

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(direction));
        bullet.SetActive(true);
        Projectile projectile = bullet.GetComponent<Projectile>();
        projectile.Launch(direction * gunProjectileSpeed);
    }

    private void ThrowSpear()
    {
        Vector3 direction = GetAimDirection();
        Vector3 velocity = direction * spearSpeed + Vector3.up * spearUpArc;

        GameObject spear = Instantiate(spearPrefab, firePoint.position, Quaternion.LookRotation(direction));
        spear.SetActive(true);
        SpearProjectile spearProjectile = spear.GetComponent<SpearProjectile>();
        spearProjectile.Launch(velocity);
    }

    private Vector3 GetAimDirection()
    {
        if (playerCamera == null) return transform.forward;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = ray.origin + ray.direction * aimDistance;
        if (Physics.Raycast(ray, out RaycastHit hit, aimDistance, aimMask, QueryTriggerInteraction.Ignore))
        {
            targetPoint = hit.point;
        }

        return (targetPoint - firePoint.position).normalized;
    }

    private void RefreshWeaponVisuals()
    {
        if (gunVisual != null) gunVisual.SetActive(currentMode == WeaponMode.Gun);
        if (spearVisual != null) spearVisual.SetActive(currentMode == WeaponMode.Spear);
    }

    private void EnsureRuntimePrefabs()
    {
        if (bulletPrefab == null)
        {
            bulletPrefab = RuntimeWeaponFactory.CreateBulletPrefab();
        }

        if (spearPrefab == null)
        {
            spearPrefab = RuntimeWeaponFactory.CreateSpearPrefab();
        }
    }
}
