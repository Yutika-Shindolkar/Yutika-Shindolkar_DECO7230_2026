#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

// Builds the beige floor grid from the reference mockup, with the grid fading into a
// matching-colour fog toward the horizon so it reads as "infinite" rather than a bounded
// patch. Run via Tools > IP2a > Build Grid Environment with the IP2a scene open.
public static class IP2aGridEnvironment
{
    const string MaterialsDir = "Assets/Materials";

    // Sized so its edge sits well past where fog has already fully hidden it - see
    // fogEndDistance below. Adjust to your actual play space if needed.
    const float floorSize = 60f;

    static readonly Color backgroundColor = new Color(1f, 244f / 255f, 230f / 255f); // #FFF4E6

    // Linear fog in the same colour as the grid background: close up the grid reads
    // clearly, then it fades smoothly to flat colour and blends into the background with
    // no visible edge - the "infinite floor" effect without needing a vertical wall.
    const float fogStartDistance = 3f;
    const float fogEndDistance = 12f;

    [MenuItem("Tools/IP2a/Build Grid Environment")]
    public static void Build()
    {
        Directory.CreateDirectory(MaterialsDir);
        Material gridMat = GetOrCreateGridMaterial();

        GameObject floor = GameObject.Find("Floor");
        if (floor == null)
        {
            Debug.LogWarning("IP2aSceneBuilder: no 'Floor' GameObject found - creating one at the origin. " +
                "If your scene already has a floor under a different name, rename this one or move its " +
                "collider over, then delete the extra.");
            floor = new GameObject("Floor");
        }
        ApplyGridQuad(floor, floorSize, gridMat, Quaternion.Euler(90, 0, 0));

        // Superseded by fog (see below) - a single nearby vertical plane read as "a wall
        // right behind the floor" rather than infinite, per your feedback. Left inactive
        // instead of deleted in case you want to revisit a backdrop-based approach later.
        GameObject backdrop = GameObject.Find("GridBackdrop");
        if (backdrop != null) backdrop.SetActive(false);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = backgroundColor;
        RenderSettings.fogStartDistance = fogStartDistance;
        RenderSettings.fogEndDistance = fogEndDistance;

        // The camera's own solid background colour was still plain white (left over from
        // the XRI demo rig) - matching it to the fog colour is what makes the fade-out
        // blend seamlessly instead of fading to grid-beige and then hard-cutting to white.
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
        }
        else
        {
            Debug.LogWarning("IP2aGridEnvironment: no Camera found in the scene - couldn't match its " +
                "background colour to the fog. Set it to #FFF4E6 by hand if there's still a visible seam " +
                "at the horizon.");
        }

        Selection.objects = new Object[] { floor };
        Debug.Log("IP2aGridEnvironment: build complete. Floor grid updated, fog enabled, GridBackdrop disabled.");
    }

    static void ApplyGridQuad(GameObject go, float size, Material mat, Quaternion rotation)
    {
        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf == null) mf = go.AddComponent<MeshFilter>();
        Mesh mesh = BuildGroundQuadMesh(size);
        mf.sharedMesh = mesh;

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr == null) mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        // If this object already has a MeshCollider (e.g. the XRI demo floor's teleport/
        // locomotion collider), keep it matched to the new visual mesh - otherwise it
        // stays sized to whatever the OLD mesh was, so walking/teleporting would be
        // bounded by a much smaller patch than what's visible.
        MeshCollider meshCol = go.GetComponent<MeshCollider>();
        if (meshCol != null) meshCol.sharedMesh = mesh;

        go.transform.rotation = rotation;
        // The real-world size is baked into the mesh vertices (BuildGroundQuadMesh), so
        // scale must be reset here - a pre-existing "Floor" object (e.g. from the XRI
        // demo scene) can carry a leftover scale from its ORIGINAL mesh (I found one at
        // 50x), which would silently blow this mesh up by the same factor.
        go.transform.localScale = Vector3.one;
        // Existing colliders (e.g. a floor collider already used for teleport/locomotion)
        // are left alone - this only replaces the visual mesh.
    }

    // Flat quad in the XZ plane, vertices at +-size/2. UVs are baked directly in metres
    // (not 0..1), so with the texture's wrapMode set to Repeat, one tile is always
    // exactly 1 metre regardless of the quad's size - no separate tiling math needed.
    static Mesh BuildGroundQuadMesh(float size)
    {
        float half = size * 0.5f;
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-half, -half, 0),
            new Vector3(half, -half, 0),
            new Vector3(half, half, 0),
            new Vector3(-half, half, 0),
        };
        mesh.uv = new Vector2[]
        {
            new Vector2(-half, -half),
            new Vector2(half, -half),
            new Vector2(half, half),
            new Vector2(-half, half),
        };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Material GetOrCreateGridMaterial()
    {
        string matPath = MaterialsDir + "/GridMaterial.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existing != null) return existing;

        Texture2D tex = GetOrCreateGridTexture();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        Material mat = new Material(shader);
        // Drive the colour through emission rather than the lit albedo, so it renders
        // exactly the texture's authored colour no matter how bright the scene lighting
        // is. This project's tonemapping is off, so a near-white ALBEDO under a strong
        // overhead light was blowing straight past pure white - that's what was washing
        // the floor out even though the texture itself is correctly beige.
        mat.SetColor("_BaseColor", Color.black);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", Color.white);
            mat.SetTexture("_EmissionMap", tex);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        // Backdrop plane visibility shouldn't depend on getting triangle winding/normal
        // direction exactly right without being able to render-check it - Off guarantees
        // it's visible from both sides.
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
        AssetDatabase.CreateAsset(mat, matPath);
        return mat;
    }

    static Texture2D GetOrCreateGridTexture()
    {
        string texPath = MaterialsDir + "/GridTexture.png";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (existing != null) return existing;

        int size = 256;
        int lineThickness = 5;
        Color background = new Color(1f, 244f / 255f, 230f / 255f); // #FFF4E6
        Color line = new Color(0.70f, 0.66f, 0.60f);

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool onLine = x < lineThickness || x >= size - lineThickness ||
                              y < lineThickness || y >= size - lineThickness;
                pixels[y * size + x] = onLine ? line : background;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(texPath, png);
        AssetDatabase.ImportAsset(texPath);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
    }
}
#endif
