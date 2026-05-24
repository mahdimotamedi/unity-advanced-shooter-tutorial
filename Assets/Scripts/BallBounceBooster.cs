using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallBounceBooster : MonoBehaviour
{
    [Header("Bounce Feel")]
    [Tooltip("Wall bounce multiplier. Keep this near 1 so it feels punchy without gaining energy forever.")]
    public float wallBounceBoost = 0.98f;

    [Tooltip("Bumpers are a little stronger than walls, but still lose energy over time.")]
    public float bumperBounceBoost = 1.03f;

    [Tooltip("Floor and ceiling contacts should lose more energy to avoid endless hopping.")]
    public float floorBounceBoost = 0.82f;

    [Tooltip("Tiny contacts below this speed are ignored so the ball can settle naturally.")]
    public float minimumIncomingSpeed = 1.4f;

    [Tooltip("Hard cap so the ball never becomes uncontrollable.")]
    public float maxSpeed = 28f;

    [Range(0f, 1f)]
    [Tooltip("How strongly this script shapes the post-collision velocity.")]
    public float velocityBlend = 0.62f;

    [Header("Natural Slowdown")]
    [Tooltip("Extra air resistance applied every physics tick. Higher values make the ball slow down sooner.")]
    public float linearDampingPerSecond = 0.22f;

    [Tooltip("Spin damping applied every physics tick.")]
    public float angularDampingPerSecond = 0.32f;

    [Tooltip("When the ball is slower than this, it can go to sleep and stop sliding forever.")]
    public float sleepSpeed = 0.18f;

    [Tooltip("How long the ball must stay slow before it is put to sleep.")]
    public float sleepDelay = 1.25f;

    private Rigidbody rb;
    private Vector3 previousVelocity;
    private float slowTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Do not set this to 0 for this demo. A small threshold lets very weak bounces die out.
        Physics.bounceThreshold = 0.25f;
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        previousVelocity = rb.velocity;

        ApplyNaturalDamping();
        ClampSpeed();
        SleepWhenNearlyStopped();
    }

    private void ApplyNaturalDamping()
    {
        float linearFactor = Mathf.Exp(-linearDampingPerSecond * Time.fixedDeltaTime);
        float angularFactor = Mathf.Exp(-angularDampingPerSecond * Time.fixedDeltaTime);

        rb.velocity *= linearFactor;
        rb.angularVelocity *= angularFactor;
    }

    private void ClampSpeed()
    {
        float speed = rb.velocity.magnitude;
        if (speed > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }

    private void SleepWhenNearlyStopped()
    {
        if (rb.velocity.magnitude < sleepSpeed && rb.angularVelocity.magnitude < sleepSpeed)
        {
            slowTimer += Time.fixedDeltaTime;
            if (slowTimer >= sleepDelay)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }
        }
        else
        {
            slowTimer = 0f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (rb == null || collision.contactCount == 0)
        {
            return;
        }

        float incomingSpeed = previousVelocity.magnitude;
        if (incomingSpeed < minimumIncomingSpeed)
        {
            return;
        }

        float boost = GetBoostForCollision(collision.collider.gameObject.name);
        if (boost <= 0f)
        {
            return;
        }

        Vector3 averageNormal = Vector3.zero;
        for (int i = 0; i < collision.contactCount; i++)
        {
            averageNormal += collision.GetContact(i).normal;
        }
        averageNormal.Normalize();

        if (averageNormal.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 reflectedVelocity = Vector3.Reflect(previousVelocity, averageNormal);
        float targetSpeed = Mathf.Clamp(incomingSpeed * boost, 0f, maxSpeed);
        Vector3 targetVelocity = reflectedVelocity.normalized * targetSpeed;

        rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, velocityBlend);
    }

    private float GetBoostForCollision(string otherName)
    {
        if (otherName.Contains("Wall"))
        {
            return wallBounceBoost;
        }

        if (otherName.Contains("Bumper"))
        {
            return bumperBounceBoost;
        }

        if (otherName.Contains("Floor") || otherName.Contains("Ceiling"))
        {
            return floorBounceBoost;
        }

        return 0f;
    }
}
