using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace St.EditorExtension
{
    public class DebugButtonManager : MonoBehaviour
    {
        private class DebugButtonInfo
        {
            public string Name;
            public MethodInfo Method;
            public MonoBehaviour Target;
            public ParameterInfo[] Parameters;
            public object[] ParameterValues;
        }

        private List<DebugButtonInfo> _buttons = new List<DebugButtonInfo>();
        private Vector2 _scrollPosition;
        private bool _isMinimized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            var go = new GameObject("RuntimeDebugButtonManager");
            go.AddComponent<DebugButtonManager>();
            DontDestroyOnLoad(go);
        }

        private void Start()
        {
            RefreshDebugButtons();
        }

        public void RefreshDebugButtons()
        {
            _buttons.Clear();
            var allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

            foreach (var comp in allComponents)
            {
                if (comp == null) continue;
                if (comp.GetType().Assembly.GetName().Name != "Assembly-CSharp") continue;

                var methods = comp.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttribute<DebugButtonAttribute>();
                    if (attr == null) continue;

                    var parameters = method.GetParameters();
                    var paramValues = new object[parameters.Length];

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var pType = parameters[i].ParameterType;
                        if (pType == typeof(int) || pType == typeof(float)) paramValues[i] = 0;
                        else if (pType == typeof(string)) paramValues[i] = "";
                        else if (pType == typeof(bool)) paramValues[i] = false;
                        else if (pType.IsEnum)
                        {
                            var enumValues = Enum.GetValues(pType);
                            paramValues[i] = enumValues.Length > 0 ? enumValues.GetValue(0) : null;
                        }
                        else paramValues[i] = null;
                    }

                    string btnName = string.IsNullOrEmpty(attr.ButtonName) ? method.Name : attr.ButtonName;

                    _buttons.Add(new DebugButtonInfo
                    {
                        Name = $"{comp.gameObject.name} : {btnName}",
                        Method = method,
                        Target = comp,
                        Parameters = parameters,
                        ParameterValues = paramValues
                    });
                }
            }
        }

        private void OnGUI()
        {
            if (_buttons == null || _buttons.Count == 0) return;
            
            // UIスキンの基本文字色を強制ホワイト化（同化対策）
            GUI.skin.button.normal.textColor = Color.white;
            GUI.skin.button.hover.textColor = Color.cyan;
            GUI.skin.box.normal.textColor = Color.white;
            GUI.skin.label.normal.textColor = Color.white;
            GUI.skin.textField.normal.textColor = Color.white;
            GUI.skin.toggle.normal.textColor = Color.white;

            // 最小化状態に応じてウィンドウサイズを可変
            float width = _isMinimized ? 120f : 480f;
            float height = _isMinimized ? 40f : Mathf.Min(_buttons.Count * 70f + 45f, Screen.height - 50f);
            
            GUILayout.BeginArea(new Rect(10, 10, width, height), GUI.skin.box);
            
            // コントロールバー（最上部）
            GUILayout.BeginHorizontal();
            if (_isMinimized)
            {
                GUILayout.Label("🛠️ Debug", GUILayout.Width(70));
                if (GUILayout.Button("📁", GUILayout.Width(30), GUILayout.Height(20)))
                {
                    _isMinimized = false;
                }
            }
            else
            {
                GUILayout.Label("🛠️ Runtime Debug Buttons", GUILayout.ExpandWidth(true));
                if (GUILayout.Button("＿", GUILayout.Width(30), GUILayout.Height(20)))
                {
                    _isMinimized = true;
                }
            }
            GUILayout.EndHorizontal();

            // 最小化中、またはボタン未検出ならここでエリアを閉じる
            if (_isMinimized || _buttons.Count == 0)
            {
                GUILayout.EndArea();
                return;
            }

            GUILayout.Space(5);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            foreach (var btn in _buttons)
            {
                if (btn.Target == null) continue;

                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.BeginHorizontal();

                // メインの関数実行ボタン
                if (GUILayout.Button(btn.Name, GUILayout.Width(220), GUILayout.Height(35)))
                {
                    btn.Method.Invoke(btn.Target, btn.ParameterValues);
                }

                // 引数エリアの自動構築
                if (btn.Parameters.Length > 0)
                {
                    GUILayout.BeginVertical();
                    for (int i = 0; i < btn.Parameters.Length; i++)
                    {
                        var param = btn.Parameters[i];
                        object val = btn.ParameterValues[i];

                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"{param.Name}:", GUILayout.Width(70));

                        // 1. int 型
                        if (param.ParameterType == typeof(int))
                        {
                            string rawStr = GUILayout.TextField(val.ToString(), GUILayout.Width(80));
                            int.TryParse(rawStr, out int parsed);
                            btn.ParameterValues[i] = parsed;
                        }
                        // 2. float 型
                        else if (param.ParameterType == typeof(float))
                        {
                            string rawStr = GUILayout.TextField(val.ToString(), GUILayout.Width(80));
                            float.TryParse(rawStr, out float parsed);
                            btn.ParameterValues[i] = parsed;
                        }
                        // 3. string 型
                        else if (param.ParameterType == typeof(string))
                        {
                            btn.ParameterValues[i] = GUILayout.TextField((string)val, GUILayout.Width(130));
                        }
                        // 4. bool 型（押しやすい大きめのトグルボタン方式に最適化）
                        else if (param.ParameterType == typeof(bool))
                        {
                            bool currentBool = (bool)val;
                            string boolLabel = currentBool ? "True" : "False";

                            // 現在の状態によって文字色を変更
                            GUI.skin.button.normal.textColor = currentBool ? Color.green : Color.red;

                            if (GUILayout.Button(boolLabel, GUILayout.Width(80), GUILayout.Height(20)))
                            {
                                btn.ParameterValues[i] = !currentBool;
                            }

                            // 他のUIへの色移りを防ぐためにリセット
                            GUI.skin.button.normal.textColor = Color.white;
                        }
                        // 5. Enum（列挙型）
                        else if (param.ParameterType.IsEnum)
                        {
                            Array enumValues = Enum.GetValues(param.ParameterType);
                            int currentIndex = Array.IndexOf(enumValues, val);

                            if (GUILayout.Button($"{val}", GUILayout.Width(130), GUILayout.Height(20)))
                            {
                                currentIndex = (currentIndex + 1) % enumValues.Length;
                                btn.ParameterValues[i] = enumValues.GetValue(currentIndex);
                            }
                        }

                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.Space(4);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}