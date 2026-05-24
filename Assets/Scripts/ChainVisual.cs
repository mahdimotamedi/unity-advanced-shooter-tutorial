using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime 3D chain visual. It uses small capsule objects as alternating links.
/// A subtle sag curve and alternating link roll make it look more like a real chain.
/// </summary>
public class ChainVisual : MonoBehaviour
{
    [Header("Visual")]
    public int linkCount = 36;
    public float linkLength = 0.38f;
    public float linkThickness = 0.07f;
    public float sideOffset = 0.07f;
    public Material chainMaterial;

    [Header("Realistic Feel")]
    public float sagPerMeter = 0.018f;
    public float maxSag = 0.42f;
    public float twistDegrees = 90f;

    private readonly List<Transform> links = new List<Transform>();
    private Transform root;

    private void Awake()
    {
        BuildPool();
        SetVisible(false);
    }

    private void BuildPool()
    {
        if (root != null) return;

        GameObject rootGo = new GameObject("RuntimeSwingChainVisual");
        rootGo.transform.SetParent(transform);
        root = rootGo.transform;

        if (chainMaterial == null)
        {
            chainMaterial = new Material(Shader.Find("Standard"));
            chainMaterial.color = new Color(0.85f, 0.85f, 0.78f, 1f);
            chainMaterial.name = "Runtime Chain Material";
        }

        for (int i = 0; i < linkCount; i++)
        {
            GameObject link = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            link.name = "SwingChainLink_" + i.ToString("00");
            link.transform.SetParent(root);

            Collider col = link.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer renderer = link.GetComponent<Renderer>();
            renderer.sharedMaterial = chainMaterial;
            links.Add(link.transform);
        }
    }

    public void SetChain(Vector3 start, Vector3 end, bool visible)
    {
        if (root == null) BuildPool();
        SetVisible(visible);
        if (!visible) return;

        Vector3 line = end - start;
        float distance = line.magnitude;
        if (distance < 0.01f)
        {
            SetVisible(false);
            return;
        }

        Vector3 dir = line / distance;
        Vector3 side = Vector3.Cross(dir, Vector3.up);
        if (side.sqrMagnitude < 0.001f)
        {
            side = Vector3.Cross(dir, Vector3.right);
        }
        side.Normalize();

        int activeCount = Mathf.Clamp(Mathf.CeilToInt(distance / Mathf.Max(0.1f, linkLength)), 5, links.Count);
        float sag = Mathf.Min(maxSag, distance * sagPerMeter);

        for (int i = 0; i < links.Count; i++)
        {
            bool active = i < activeCount;
            links[i].gameObject.SetActive(active);
            if (!active) continue;

            float t = activeCount == 1 ? 0f : (i + 0.5f) / activeCount;
            Vector3 position = EvaluateSaggedLine(start, end, t, sag);
            Vector3 tangent = EvaluateSaggedLine(start, end, Mathf.Clamp01(t + 0.03f), sag) - EvaluateSaggedLine(start, end, Mathf.Clamp01(t - 0.03f), sag);
            if (tangent.sqrMagnitude < 0.0001f) tangent = dir;
            tangent.Normalize();

            Vector3 offset = side * ((i % 2 == 0) ? sideOffset : -sideOffset);
            links[i].position = position + offset;

            Quaternion alongTangent = Quaternion.FromToRotation(Vector3.up, tangent);
            Quaternion alternatingRoll = Quaternion.AngleAxis((i % 2 == 0) ? 0f : twistDegrees, tangent);
            links[i].rotation = alternatingRoll * alongTangent;
            links[i].localScale = new Vector3(linkThickness, linkLength * 0.5f, linkThickness);
        }
    }

    private static Vector3 EvaluateSaggedLine(Vector3 start, Vector3 end, float t, float sag)
    {
        Vector3 linear = Vector3.Lerp(start, end, t);
        float sagCurve = Mathf.Sin(t * Mathf.PI);
        return linear + Vector3.down * sag * sagCurve;
    }

    private void SetVisible(bool visible)
    {
        for (int i = 0; i < links.Count; i++)
        {
            links[i].gameObject.SetActive(visible);
        }
    }
}
