using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CraDev.EditorTools
{
    /// <summary>
    /// Dunyo manbalarini tekshirish (batchmode): shader xatolari, modellar o'lchami va materiallari.
    /// <c>Unity.exe -batchmode -quit -projectPath . -executeMethod CraDev.EditorTools.CraDevDiagnostics.Run</c>
    /// </summary>
    public static class CraDevDiagnostics
    {
        public static void Run()
        {
            var foliage = Shader.Find("CraDev/Foliage");
            Debug.Log($"[CraDev] Foliage shader: {(foliage == null ? "YO'Q" : ShaderUtil.ShaderHasError(foliage) ? "XATO" : "OK")}");
            if (foliage != null)
                foreach (var message in ShaderUtil.GetShaderMessages(foliage))
                    Debug.Log($"[CraDev]   {message.severity}: {message.message} (qator {message.line})");

            foreach (var path in new[] { "Assets/CraDev/World/Models/Tree/island_tree_02.fbx", "Assets/CraDev/World/Models/Shrub/shrub_01.fbx" })
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null)
                {
                    Debug.Log($"[CraDev] {path}: YO'Q");
                    continue;
                }
                var go = Object.Instantiate(model);
                var bounds = new Bounds(go.transform.position, Vector3.zero);
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                    bounds.Encapsulate(r.bounds);
                Debug.Log($"[CraDev] {System.IO.Path.GetFileName(path)}: o'lcham {bounds.size}, scale {go.transform.localScale}");
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                    foreach (var m in r.sharedMaterials)
                        Debug.Log($"[CraDev]   {r.name}: {m?.name} shader={m?.shader?.name} tex={m?.mainTexture?.name}");
                Object.DestroyImmediate(go);
            }
            // Bosh menyu: kamera bilan qahramon oyog'i orasidagi obyektlar
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/CraDev/Scenes/MainMenu.unity");
            var menuCamera = Object.FindFirstObjectByType<CraDev.World.MenuCamera>();
            var so = new SerializedObject(menuCamera);
            var from = so.FindProperty("homePosition").vector3Value;
            foreach (var target in new[] { new Vector3(0f, 0.4f, 0f), new Vector3(0f, 0.9f, 0f), new Vector3(0.5f, 0.3f, -1.5f) })
            {
                var ray = new Ray(from, (target - from).normalized);
                float max = Vector3.Distance(from, target);
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    if (r.enabled && r.bounds.IntersectRay(ray, out float d) && d < max)
                        Debug.Log($"[CraDev] nur {target}: {r.name} ({r.GetType().Name}) d={d:0.00} bounds={r.bounds.size}");
            }
            EditorApplication.Exit(0);
        }
    }
}
