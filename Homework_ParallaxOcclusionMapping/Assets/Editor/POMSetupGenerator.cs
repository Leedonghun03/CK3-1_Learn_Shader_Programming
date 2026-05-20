using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class POMSetupGenerator
{
    private const int TextureSize = 1024;
    private const string TextureFolder = "Assets/Textures";
    private const string MaterialFolder = "Assets/Materials";
    private const string SceneFolder = "Assets/Scenes";
    private const string ShaderPath = "Assets/Shaders/ParallaxOcclusionMapping.shader";

    [MenuItem("Homework/Generate POM Demo")]
    public static void GenerateDemo()
    {
        Directory.CreateDirectory(TextureFolder);
        Directory.CreateDirectory(MaterialFolder);
        Directory.CreateDirectory(SceneFolder);

        float[] heightData = BuildHeightData(TextureSize);
        WriteTexture($"{TextureFolder}/POM_Brick_Albedo.tga", BuildAlbedo(TextureSize, heightData), false);
        WriteTexture($"{TextureFolder}/POM_Brick_Height.tga", BuildHeight(TextureSize, heightData), false);
        WriteTexture($"{TextureFolder}/POM_Brick_Normal.tga", BuildNormal(TextureSize, heightData), true);
        AssetDatabase.Refresh();

        var material = CreateMaterial();
        CreateScene(material);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static Material CreateMaterial()
    {
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        var material = new Material(shader)
        {
            name = "M_ParallaxOcclusion_Brick"
        };

        material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/POM_Brick_Albedo.tga"));
        material.SetTexture("_HeightMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/POM_Brick_Height.tga"));
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/POM_Brick_Normal.tga"));
        material.SetFloat("_HeightScale", 0.052f);
        material.SetFloat("_MinLayers", 10f);
        material.SetFloat("_MaxLayers", 56f);
        material.SetFloat("_Gloss", 54f);
        material.SetColor("_POMSpecColor", new Color(0.18f, 0.16f, 0.14f, 1f));

        string materialPath = $"{MaterialFolder}/M_ParallaxOcclusion_Brick.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null)
        {
            AssetDatabase.DeleteAsset(materialPath);
        }

        AssetDatabase.CreateAsset(material, materialPath);
        return material;
    }

    private static void CreateScene(Material pomMaterial)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 0.55f;

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 1.2f, -4.4f), Quaternion.Euler(13f, 0f, 0f));
        camera.fieldOfView = 52f;
        camera.clearFlags = CameraClearFlags.Skybox;

        var lightObject = new GameObject("Directional Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);

        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "POM Brick Plane";
        plane.transform.localScale = new Vector3(1.8f, 1f, 1.8f);
        plane.GetComponent<MeshRenderer>().sharedMaterial = pomMaterial;

        var label = new GameObject("Assignment Note");
        label.transform.position = new Vector3(0f, -0.02f, 1.9f);

        EditorSceneManager.SaveScene(scene, $"{SceneFolder}/POM_Demo.unity");
    }

    private static float[] BuildHeightData(int size)
    {
        var data = new float[size * size];
        const int bricksX = 8;
        const int bricksY = 8;
        const float mortarWidth = 0.075f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;
                float v = (float)y / size;
                float row = Mathf.Floor(v * bricksY);
                float offset = Mathf.Repeat(row, 2f) * 0.5f / bricksX;
                float brickU = Mathf.Repeat((u + offset) * bricksX, 1f);
                float brickV = Mathf.Repeat(v * bricksY, 1f);
                float edge = Mathf.Min(Mathf.Min(brickU, 1f - brickU), Mathf.Min(brickV, 1f - brickV));
                float mortar = Mathf.SmoothStep(0f, mortarWidth, edge);

                float chipped = Mathf.PerlinNoise(u * 34f + 12.4f, v * 34f + 3.7f) * 0.11f;
                float broad = Mathf.PerlinNoise(u * 7f, v * 7f) * 0.08f;
                float height = Mathf.Clamp01(0.18f + mortar * (0.68f + chipped + broad));
                data[y * size + x] = height;
            }
        }

        return data;
    }

    private static Texture2D BuildAlbedo(int size, float[] heightData)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float height = heightData[y * size + x];
                float noise = Mathf.PerlinNoise(x * 0.032f, y * 0.032f);
                Color brick = Color.Lerp(new Color(0.42f, 0.13f, 0.08f), new Color(0.78f, 0.32f, 0.18f), noise);
                Color mortar = new Color(0.29f, 0.27f, 0.24f);
                texture.SetPixel(x, y, Color.Lerp(mortar, brick, Mathf.SmoothStep(0.2f, 0.75f, height)));
            }
        }

        texture.Apply();
        return texture;
    }

    private static Texture2D BuildHeight(int size, float[] heightData)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float h = heightData[y * size + x];
                texture.SetPixel(x, y, new Color(h, h, h, 1f));
            }
        }

        texture.Apply();
        return texture;
    }

    private static Texture2D BuildNormal(int size, float[] heightData)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        const float strength = 5.8f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int left = Mathf.Max(x - 1, 0);
                int right = Mathf.Min(x + 1, size - 1);
                int down = Mathf.Max(y - 1, 0);
                int up = Mathf.Min(y + 1, size - 1);
                float dx = (heightData[y * size + right] - heightData[y * size + left]) * strength;
                float dy = (heightData[up * size + x] - heightData[down * size + x]) * strength;
                Vector3 normal = new Vector3(-dx, -dy, 1f).normalized;
                texture.SetPixel(x, y, new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f));
            }
        }

        texture.Apply();
        return texture;
    }

    private static void WriteTexture(string path, Texture2D texture, bool normalMap)
    {
        File.WriteAllBytes(path, texture.EncodeToTGA());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normalMap && !path.Contains("Height");
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = true;
        importer.SaveAndReimport();
    }
}
