using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    public float lifeTime = 4f;
    public float impactForce = 6f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void Launch(Vector3 velocity)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.velocity = velocity;
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TargetDummy target = collision.collider.GetComponentInParent<TargetDummy>();
        if (target != null)
        {
            Vector3 hitPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            Vector3 hitDirection = rb != null && rb.velocity.sqrMagnitude > 0.01f ? rb.velocity.normalized : transform.forward;
            target.Hit(hitPoint, hitDirection, impactForce, WeaponMode.Gun);
        }

        Destroy(gameObject);
    }
}
