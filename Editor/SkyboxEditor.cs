using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window to easily assign 6-sided cube textures usualy used as skybox
/// </summary>
/// <see cref="https://github.com/stefanak-michal/Unity-Stuff"/>
/// <author>Michal Stefanak</author>
public class SkyboxEditor : EditorWindow
{
    static int size = 200;

    Material material;
    Texture tex;

    [MenuItem("Window/Skybox Editor")]
    public static void ShowWindow()
    {
        var w = EditorWindow.GetWindow(typeof(SkyboxEditor));
        w.minSize = new Vector2(size * 4.1f, size * 3.2f);
        w.maxSize = w.minSize;
        w.Show();
    }

    void OnGUI()
    {
        material = (Material)EditorGUILayout.ObjectField("Skybox material", material, typeof(Material), true);

        if (material != null && material.shader != Shader.Find("Skybox/6 Sided"))
            EditorGUILayout.HelpBox("Choosed material must be \"Skybox/6 Sided\" shader", MessageType.Warning);

        EditorGUILayout.Space();

        var group = EditorGUILayout.BeginHorizontal();

        TextureBox(new Rect(10, group.y + size, size, size), "Left");
        TextureBox(new Rect(10 + size, group.y, size, size), "Up");
        TextureBox(new Rect(10 + size, group.y + size, size, size), "Front");
        TextureBox(new Rect(10 + size, group.y + size + size, size, size), "Down");
        TextureBox(new Rect(10 + size * 2, group.y + size, size, size), "Right");
        TextureBox(new Rect(10 + size * 3, group.y + size, size, size), "Back");

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal(GUILayout.Width(size * .8f));

        GUILayout.Label("Rotate");
        if (GUILayout.Button("Left") && material != null)
        {
            tex = material.GetTexture("_LeftTex");
            material.SetTexture("_LeftTex", material.GetTexture("_FrontTex"));
            material.SetTexture("_FrontTex", material.GetTexture("_RightTex"));
            material.SetTexture("_RightTex", material.GetTexture("_BackTex"));
            material.SetTexture("_BackTex", tex);
        }

        if (GUILayout.Button("Right") && material != null)
        {
            tex = material.GetTexture("_BackTex");
            material.SetTexture("_BackTex", material.GetTexture("_RightTex"));
            material.SetTexture("_RightTex", material.GetTexture("_FrontTex"));
            material.SetTexture("_FrontTex", material.GetTexture("_LeftTex"));
            material.SetTexture("_LeftTex", tex);
        }

        EditorGUILayout.EndHorizontal();
    }

    void TextureBox(Rect pos, string side)
    {
        tex = null;
        tex = (Texture)EditorGUI.ObjectField(pos, material != null ? material.GetTexture("_" + side + "Tex") : null, typeof(Texture), false);
        EditorGUI.LabelField(pos, side);

        if (material != null)
            material.SetTexture("_" + side + "Tex", tex);
    }
}
