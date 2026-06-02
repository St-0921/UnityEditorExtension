using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace St.EditorExtention
{
    public class ScriptCreateLauncher : EditorWindow
    {
        private enum ScriptType { MonoBehaviour, PureClass, ScriptableObject }

        private string _namespaceName = "Scripts";
        private string _className = "NewScript";
        private ScriptType _scriptType = ScriptType.MonoBehaviour;

        // 名前空間のキャッシュ
        private static List<string> _allNamespaces = new List<string>();
        private Vector2 _scrollPos;

        [MenuItem("Assets/Create/Custom C# Script", false, -240)]
        public static void ShowWindow()
        {
            // 名前空間のリストを初期化
            RefreshNamespaces();
            var window = GetWindow<ScriptCreateLauncher>("Create Script");
            window.minSize = new Vector2(350, 300);
        }

        private void OnGUI()
        {
            GUILayout.Label("Create New Script Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // --- 名前空間の入力とサジェスト ---
            EditorGUILayout.LabelField("Namespace");
            _namespaceName = EditorGUILayout.TextField(_namespaceName);

            // 入力中の文字に合わせてサジェストを表示
            DrawNamespaceSuggestions();

            EditorGUILayout.Space();
            _className = EditorGUILayout.TextField("Class Name", _className);
            _scriptType = (ScriptType)EditorGUILayout.EnumPopup("Script Type", _scriptType);

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Script", GUILayout.Height(30)))
            {
                CreateScript();
                Close();
            }
        }

        private void DrawNamespaceSuggestions()
        {
            if (string.IsNullOrEmpty(_namespaceName)) return;

            // 入力内容に一致する名前空間をフィルタリング（上位5つまで）
            var suggestions = _allNamespaces
                .Where(n => n.StartsWith(_namespaceName, StringComparison.OrdinalIgnoreCase) && n != _namespaceName)
                .Take(5)
                .ToList();

            if (suggestions.Count > 0)
            {
                // 少し背景を暗くしてリストを表示
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                foreach (var suggestion in suggestions)
                {
                    if (GUILayout.Button(suggestion, EditorStyles.label))
                    {
                        _namespaceName = suggestion;
                        GUI.FocusControl(null); // フォーカスを外して入力を確定させる
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        // プロジェクト内の全名前空間を取得してキャッシュする
        private static void RefreshNamespaces()
        {
            _allNamespaces = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => {
                    try { return a.GetTypes(); }
                    catch { return new Type[0]; } // 読み込めないアセンブリは無視
                })
                .Select(t => t.Namespace)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct()
                .OrderBy(n => n)
                .ToList();
        }

        private void CreateScript()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path)) path = "Assets";
            if (!Directory.Exists(path)) path = Path.GetDirectoryName(path);

            string fullPath = Path.Combine(path, _className + ".cs");

            if (File.Exists(fullPath))
            {
                EditorUtility.DisplayDialog("Error", "File already exists!", "OK");
                return;
            }

            string template = GenerateTemplate();
            File.WriteAllText(fullPath, template);
            AssetDatabase.Refresh();

            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(fullPath);
            ProjectWindowUtil.ShowCreatedAsset(asset);
        }

        private string GenerateTemplate()
        {
            // (以前のテンプレート生成ロジックと同じなので省略)
            switch (_scriptType)
            {
                case ScriptType.MonoBehaviour:
                    return $"using UnityEngine;\n\nnamespace {_namespaceName}\n{{\n    public class {_className} : MonoBehaviour\n    {{\n        void Start()\n        {{\n        }}\n    }}\n}}";
                case ScriptType.ScriptableObject:
                    return $"using UnityEngine;\n\nnamespace {_namespaceName}\n{{\n    [CreateAssetMenu(fileName = \"{_className}\", menuName = \"ScriptableObjects/{_className}\")]\n    public class {_className} : ScriptableObject\n    {{\n    }}\n}}";
                case ScriptType.PureClass:
                default:
                    return $"namespace {_namespaceName}\n{{\n    public class {_className}\n    {{\n        public {_className}()\n        {{\n        }}\n    }}\n}}";
            }
        }
    }
}