using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoomerangGuardian.EditorTools
{
    /// <summary>
    /// Helpers for idempotent setup scripts (ADR-0009): find-or-create assets and scene
    /// objects, and set serialized references, logging what was created or skipped.
    /// Idempotent means running a setup menu twice gives the same result as running it once:
    /// every helper first looks for the existing thing and only creates or links what is missing.
    /// One instance is created per setup run; its <see cref="Log"/> is printed at the end
    /// (+ created, ~ linked, = exists, ! warning).
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

        /// <summary>Creates an asset folder and any missing parents (recursive, like "mkdir -p").</summary>
        public void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
            Created($"folder {path}");
        }

        /// <summary>Loads _Project/Materials/{name}.mat, or creates a URP Lit material with this color.</summary>
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
            // URP shaders read _BaseColor; material.color sets _Color (built-in fallback). Set both.
            material.SetColor("_BaseColor", color);
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
            Created(path);
            return material;
        }

        /// <summary>Unlit variant for minimap icons: flat color, unaffected by scene lighting (night theme).</summary>
        public Material GetOrCreateUnlitMaterial(string name, Color color)
        {
            string folder = $"{ProjectRoot}/Materials/Minimap";
            string path = $"{folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                Skipped(path);
                return material;
            }

            EnsureFolder(folder);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
            Created(path);
            return material;
        }

        /// <summary>Returns the index of a named user layer, creating it in the first free slot (8–31) if needed.</summary>
        public int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0)
            {
                Skipped($"layer {layerName} ({existing})");
                return existing;
            }

            // Layers live in ProjectSettings/TagManager.asset; there is no public API to add one,
            // so edit its "layers" array through SerializedObject. Slots 0–7 are reserved by Unity.
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty slot = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(slot.stringValue)) continue;
                slot.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                Created($"layer {layerName} ({i})");
                return i;
            }
            Warn($"No free layer slot for {layerName}");
            return -1;
        }

        /// <summary>Puts an object and all its children (inactive too) on a layer, e.g. Minimap icons.</summary>
        public static void SetLayerRecursively(GameObject go, int layer)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
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
                    // Register with Undo so the setup run can be undone (Ctrl/Cmd+Z) and the scene is marked dirty.
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

        /// <summary>Finds a top-level object by name in one scene (works for inactive objects, unlike GameObject.Find).</summary>
        public static Transform FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root.transform;
            return null;
        }

        /// <summary>Returns the existing component of type T, or adds one (with Undo support).</summary>
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
            // Private [SerializeField] fields cannot be set from outside in C#, but SerializedObject
            // can, by field name, the same way the Inspector does. It also records Undo and marks dirty.
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
            // CreatePrimitive adds a collider; physics belongs on the prefab root (project prefab convention).
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
