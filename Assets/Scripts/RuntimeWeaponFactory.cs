using UnityEngine;

public static class RuntimeWeaponFactory
{
    public static GameObject CreateBulletPrefab()
    {
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "RuntimeBulletPrefab";
        bullet.transform.localScale = Vector3.one * 0.16f;
        Object.DontDestroyOnLoad(bullet);
        bullet.SetActive(false);

        Rigidbody rb = bullet.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.mass = 0.08f;

        bullet.AddComponent<Projectile>();

        Renderer renderer = bullet.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0.82f, 0.18f, 1f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.05f, 1f));
        renderer.sharedMaterial = mat;

        return bullet;
    }

    public static GameObject CreateSpearPrefab()
    {
        GameObject spear = new GameObject("RuntimeSpearPrefab");
        Object.DontDestroyOnLoad(spear);
        spear.SetActive(false);

        Rigidbody rb = spear.AddComponent<Rigidbody>();
        rb.mass = 0.65f;
        rb.drag = 0.02f;

        CapsuleCollider col = spear.AddComponent<CapsuleCollider>();
        col.direction = 2;
        col.radius = 0.07f;
        col.height = 1.65f;
        col.center = new Vector3(0f, 0f, 0.05f);

        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Shaft";
        shaft.transform.SetParent(spear.transform);
        shaft.transform.localPosition = new Vector3(0f, 0f, -0.15f);
        shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        shaft.transform.localScale = new Vector3(0.045f, 0.72f, 0.045f);
        Object.Destroy(shaft.GetComponent<Collider>());

        GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        tip.name = "Tip";
        tip.transform.SetParent(spear.transform);
        tip.transform.localPosition = new Vector3(0f, 0f, 0.66f);
        tip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        tip.transform.localScale = new Vector3(0.09f, 0.16f, 0.09f);
        Object.Destroy(tip.GetComponent<Collider>());

        Material wood = new Material(Shader.Find("Standard"));
        wood.color = new Color(0.42f, 0.24f, 0.12f, 1f);
        shaft.GetComponent<Renderer>().sharedMaterial = wood;

        Material steel = new Material(Shader.Find("Standard"));
        steel.color = new Color(0.72f, 0.72f, 0.76f, 1f);
        tip.GetComponent<Renderer>().sharedMaterial = steel;

        spear.AddComponent<SpearProjectile>();
        return spear;
    }
}
