using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoomerangGuardian.EditorTools
{
    /// <summary>
    /// Helpers for idempotent setup scripts (ADR-0009): find-or-create assets and scene
    /// objects, and set serialized references, logging what was created or skipped.
    /// </summary>
    public class SetupUtils
    {
        public const string ProjectRoot = "Assets/_Project";

        private readonly StringBuilder log = new StringBuilder();

        public string Log => log.ToString();

        public void Created(string what) => log.AppendLine($"  + created  {what}");
        public void Linked(string what) => log.AppendLine($"  ~ linked   {what}");
        public void Skipped(string what) => log.AppendLine($"  = exists   {what}");
        public void Warn(string what) => log.AppendLine($"  ! WARNING  {what}");

        // ---------- Assets ----------

        public void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
            Created($"folder {path}");
        }

        public Material GetOrCreateMaterial(string name, Color color)
        {
            string folder = $"{ProjectRoot}/Materials";
            string path = $"{folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                Skipped(path);
                return material;
            }

            EnsureFolder(folder);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
            Created(path);
            return material;
        }

        // ---------- Scene objects ----------

        /// <summary>Finds a GameObject by hierarchy path ("A/B/C") in the scene, creating missing parts.</summary>
        public GameObject FindOrCreate(Scene scene, string path)
        {
            string[] parts = path.Split('/');
            Transform current = null;
            for (int i = 0; i < parts.Length; i++)
            {
                Transform next = current == null ? FindRoot(scene, parts[i]) : current.Find(parts[i]);
                if (next == null)
                {
                    var go = new GameObject(parts[i]);
                    Undo.RegisterCreatedObjectUndo(go, "Setup");
                    SceneManager.MoveGameObjectToScene(go, scene);
                    if (current != null) go.transform.SetParent(current, false);
                    next = go.transform;
                    Created($"scene object {string.Join("/", parts, 0, i + 1)}");
                }
                current = next;
            }
            return current.gameObject;
        }

        public static Transform FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root.transform;
            return null;
        }

        public T GetOrAdd<T>(GameObject go) where T : Component
        {
            if (go.TryGetComponent(out T existing))
            {
                Skipped($"{typeof(T).Name} on {go.name}");
                return existing;
            }
            T added = Undo.AddComponent<T>(go);
            Created($"{typeof(T).Name} on {go.name}");
            return added;
        }

        /// <summary>Sets an object reference field only if it is empty (never overwrites user wiring).</summary>
        public void LinkIfEmpty(Object owner, string field, Object value)
        {
            var so = new SerializedObject(owner);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Warn($"{owner.GetType().Name}.{field} not found");
                return;
            }
            if (prop.objectReferenceValue != null) return;
            if (value == null)
            {
                Warn($"{owner.GetType().Name}.{field}: nothing to link");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
            Linked($"{owner.GetType().Name}.{field} → {value.name}");
        }

        /// <summary>Sets a serialized value unconditionally (use only on freshly created components).</summary>
        public static void SetValue(Object owner, string field, System.Action<SerializedProperty> assign)
        {
            var so = new SerializedObject(owner);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null) return;
            assign(prop);
            so.ApplyModifiedProperties();
        }

        /// <summary>Creates a primitive as a visual-only child (its collider removed).</summary>
        public static GameObject Visual(PrimitiveType type, Transform parent, string name, Vector3 localPosition,
            Vector3 localScale, Material material, Vector3 localEuler = default)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localEulerAngles = localEuler;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }
    }
}
