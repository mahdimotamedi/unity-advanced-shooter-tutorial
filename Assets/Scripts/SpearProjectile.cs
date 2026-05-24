using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpearProjectile : MonoBehaviour
{
    public float lifeTime = 9f;
    public float impactForce = 14f;

    private Rigidbody rb;
    private bool stuck;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Destroy(gameObject, lifeTime);
    }

    public void Launch(Vector3 velocity)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.velocity = velocity;
        if (velocity.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(velocity.normalized);
        }
    }

    private void Update()
    {
        if (!stuck && rb != null && rb.velocity.sqrMagnitude > 1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.velocity.normalized);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (stuck) return;
        stuck = true;

        Vector3 hitVelocity = rb != null ? rb.velocity : transform.forward;
        Vector3 hitDirection = hitVelocity.sqrMagnitude > 0.01f ? hitVelocity.normalized : transform.forward;
        Vector3 hitPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;

        TargetDummy target = collision.collider.GetComponentInParent<TargetDummy>();
        if (target != null)
        {
            target.Hit(hitPoint, hitDirection, impactForce, WeaponMode.Spear);
        }

        ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default(ContactPoint);
        if (collision.contactCount > 0)
        {
            transform.position = contact.point - transform.forward * 0.55f;
        }

        transform.SetParent(collision.collider.transform, true);

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, target != null ? 1.25f : 4f);
    }
}
