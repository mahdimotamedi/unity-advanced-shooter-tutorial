using UnityEngine;

/// <summary>
/// Put this component on the parent object of the sphere/chain that the player can grab.
/// Child colliders are enough because SwingController searches with GetComponentInParent.
/// </summary>
public class SwingAnchor : MonoBehaviour
{
    [Tooltip("The actual pivot point for the swing. Leave empty to use this transform.")]
    public Transform pivot;

    [Tooltip("Optional point used only as the visual hanging end of the chain in the demo.")]
    public Transform hangingChainEnd;

    [Tooltip("A small helper value used by the editor demo builder and gizmos.")]
    public float grabRadius = 1.25f;

    [Header("Static Chain Visual")]
    [Tooltip("The chain that is visible before the player grabs it. This is hidden while swinging and shown again on release.")]
    public GameObject staticChainVisualRoot;

    public Vector3 PivotPosition
    {
        get { return pivot != null ? pivot.position : transform.position; }
    }

    public Vector3 HangingEndPosition
    {
        get { return hangingChainEnd != null ? hangingChainEnd.position : PivotPosition; }
    }

    public void HideStaticChain()
    {
        if (staticChainVisualRoot != null)
        {
            staticChainVisualRoot.SetActive(false);
        }
    }

    public void ShowStaticChain()
    {
        if (staticChainVisualRoot != null)
        {
            staticChainVisualRoot.SetActive(true);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.1f, 0.75f, 1f, 0.7f);
        Gizmos.DrawWireSphere(PivotPosition, grabRadius);

        if (hangingChainEnd != null)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.9f);
            Gizmos.DrawLine(PivotPosition, hangingChainEnd.position);
            Gizmos.DrawWireSphere(hangingChainEnd.position, grabRadius * 0.45f);
        }
    }
}
