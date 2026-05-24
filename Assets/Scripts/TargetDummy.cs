using System.Collections;
using UnityEngine;

public class TargetDummy : MonoBehaviour
{
    [Header("Target Rules")]
    public TargetWeaponRequirement requiredWeapon = TargetWeaponRequirement.Any;
    public int maxHealth = 3;
    public int pointsPerHit = 5;
    public int pointsOnDestroyed = 35;
    public float destroyDelay = 0.5f;

    [Header("Feedback")]
    public float resetColorDelay = 0.16f;
    public Color correctHitColor = Color.white;
    public Color wrongHitColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    [Header("Random Respawn Area")]
    public Vector3 spawnAreaMin = new Vector3(-13f, 1.25f, 20f);
    public Vector3 spawnAreaMax = new Vector3(13f, 3.2f, 34f);
    public float minDistanceFromOldPosition = 3.5f;

    private int currentHealth;
    private Renderer[] renderers;
    private Color[] originalColors;
    private Collider[] colliders;
    private Rigidbody rb;
    private bool respawning;

    public int CurrentHealth { get { return currentHealth; } }

    private void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);

        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }

        colliders = GetComponentsInChildren<Collider>();

        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = 3f;
        rb.useGravity = false;
        rb.drag = 1.25f;
        rb.angularDrag = 3.5f;
    }

    public void Hit(Vector3 point, Vector3 direction, float force, WeaponMode weaponMode)
    {
        if (respawning) return;

        if (!AcceptsWeapon(weaponMode))
        {
            FlashColor(wrongHitColor);
            ScoreManager score = ScoreManager.Instance;
            if (score != null)
            {
                score.SetMessage(GetDisplayName() + " needs " + GetRequiredWeaponName() + ".");
            }
            return;
        }

        currentHealth--;
        FlashColor(correctHitColor);

        if (rb != null)
        {
            rb.AddForceAtPosition(direction.normalized * force, point, ForceMode.Impulse);
        }

        ScoreManager scoreManager = ScoreManager.Instance;
        if (scoreManager != null)
        {
            scoreManager.AddScore(pointsPerHit, "+" + pointsPerHit + " hit: " + GetDisplayName());
        }

        if (currentHealth <= 0)
        {
            StartCoroutine(DestroyAndRespawn());
        }
    }

    private bool AcceptsWeapon(WeaponMode weaponMode)
    {
        if (requiredWeapon == TargetWeaponRequirement.Any) return true;
        if (requiredWeapon == TargetWeaponRequirement.GunOnly) return weaponMode == WeaponMode.Gun;
        if (requiredWeapon == TargetWeaponRequirement.SpearOnly) return weaponMode == WeaponMode.Spear;
        return true;
    }

    private IEnumerator DestroyAndRespawn()
    {
        respawning = true;

        ScoreManager scoreManager = ScoreManager.Instance;
        if (scoreManager != null)
        {
            scoreManager.AddScore(pointsOnDestroyed, "+" + pointsOnDestroyed + " destroyed: " + GetDisplayName());
        }

        yield return new WaitForSeconds(destroyDelay);

        SetVisibleAndCollidable(false);
        ResetPhysics();

        Vector3 oldPosition = transform.position;
        transform.position = GetRandomSpawnPosition(oldPosition);
        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        currentHealth = Mathf.Max(1, maxHealth);
        RestoreColor();

        yield return null;

        SetVisibleAndCollidable(true);
        respawning = false;
    }

    private Vector3 GetRandomSpawnPosition(Vector3 oldPosition)
    {
        Vector3 result = oldPosition;
        for (int i = 0; i < 20; i++)
        {
            result = new Vector3(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                Random.Range(spawnAreaMin.y, spawnAreaMax.y),
                Random.Range(spawnAreaMin.z, spawnAreaMax.z)
            );

            if (Vector3.Distance(result, oldPosition) >= minDistanceFromOldPosition)
            {
                return result;
            }
        }

        return result;
    }

    private void ResetPhysics()
    {
        if (rb == null) return;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void SetVisibleAndCollidable(bool value)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].enabled = value;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null) colliders[i].enabled = value;
        }
    }

    private void FlashColor(Color color)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].material.color = color;
            }
        }

        CancelInvoke(nameof(RestoreColor));
        Invoke(nameof(RestoreColor), resetColorDelay);
    }

    private void RestoreColor()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].material.color = originalColors[i];
            }
        }
    }

    public string GetRequiredWeaponName()
    {
        if (requiredWeapon == TargetWeaponRequirement.GunOnly) return "Gun";
        if (requiredWeapon == TargetWeaponRequirement.SpearOnly) return "Spear";
        return "Any Weapon";
    }

    private string GetDisplayName()
    {
        return string.IsNullOrEmpty(gameObject.name) ? "Target" : gameObject.name;
    }
}
