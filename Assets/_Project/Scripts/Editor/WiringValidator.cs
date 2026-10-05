using System.Reflection;
using System.Text;
using BoomerangGuardian.Core;
using UnityEditor;
using UnityEngine;

namespace BoomerangGuardian.EditorTools
{
    /// <summary>
    /// Reports empty object references on our components in the open scenes and in prefabs under Assets/_Project/Prefabs.
    /// Fields marked [OptionalReference] may stay empty. Added after a missing link
    /// (PlayerMotor.cameraRig) went unnoticed for two sprints (Sprint 4, PB-26).
    /// Menu: Boomerang Guardian → Validate Wiring. Also runs at the end of every setup script.
    /// </summary>
    public static class WiringValidator
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem("Boomerang Guardian/Validate Wiring")]
        public static void ValidateFromMenu()
        {
            string report = Report(out int problems);
            if (problems > 0) Debug.LogWarning(report);
            else Debug.Log(report);
            EditorUtility.DisplayDialog("Validate Wiring",
                problems == 0 ? "All required references are set." : $"{problems} empty reference(s). See the Console for details.", "OK");
        }

        public static string Report() => Report(out _);

        public static string Report(out int problems)
        {
            var sb = new StringBuilder("\nWiring check:\n");
            problems = 0;

            // Scene objects
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                problems += Check(behaviour, $"scene: {Path(behaviour != null ? behaviour.transform : null)}", sb);

            // Prefabs: model swaps happen here, e.g. a replaced Spinner breaks BoomerangProjectile.spinner.
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null) continue;
                foreach (MonoBehaviour behaviour in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                    problems += Check(behaviour, $"prefab: {assetPath} ({Path(behaviour != null ? behaviour.transform : null)})", sb);
            }

            sb.AppendLine(problems == 0 ? "  ✓ all required references are set" : $"  {problems} empty reference(s): assign them in the Inspector or rerun the setup menu.");
            return sb.ToString();
        }

        private static int Check(MonoBehaviour behaviour, string where, StringBuilder sb)
        {
            if (behaviour == null) return 0;
            System.Type type = behaviour.GetType();
            if (type.Namespace == null || !type.Namespace.StartsWith("BoomerangGuardian")) return 0;

            int problems = 0;
            var so = new SerializedObject(behaviour);
            SerializedProperty prop = so.GetIterator();
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.propertyType != SerializedPropertyType.ObjectReference || prop.objectReferenceValue != null) continue;
                if (prop.name == "m_Script" || IsOptional(type, prop.name)) continue;

                problems++;
                sb.AppendLine($"  ! EMPTY    {where} → {type.Name}.{prop.name}");
            }
            return problems;
        }

        private static bool IsOptional(System.Type type, string fieldName)
        {
            for (System.Type t = type; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
            {
                FieldInfo field = t.GetField(fieldName, FieldFlags);
                if (field != null) return field.GetCustomAttribute<OptionalReferenceAttribute>() != null;
            }
            return false;
        }

        private static string Path(Transform t)
        {
            if (t == null) return "?";
            string path = t.name;
            for (Transform p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            return path;
        }
    }
}
