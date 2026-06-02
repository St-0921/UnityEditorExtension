using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace St.EditorExtension
{
    [InitializeOnLoad]
    public class MissingReferenceHighlighter
    {
        private static readonly Color HighlightColor = new Color(1f, 0f, 0f, 0.2f);
        
        // パフォーマンス向上のため、一度解析した型のフィールド情報をキャッシュする
        private static readonly Dictionary<Type, List<CachedFieldInfo>> CachedFields = new Dictionary<Type, List<CachedFieldInfo>>();

        private class CachedFieldInfo
        {
            public FieldInfo Field;
            public bool IsCollection;
        }

        static MissingReferenceHighlighter()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null) return;

            if (HasEmptySerializeField(obj))
            {
                // 背景全体を薄く赤くする
                EditorGUI.DrawRect(selectionRect, HighlightColor);

                /*
                // 右端に小さなラベルを表示
                Rect labelRect = new Rect(selectionRect.xMax - 40, selectionRect.yMin, 40, selectionRect.height);
                GUI.Label(labelRect, "None!", EditorStyles.miniLabel);
                */
            }
        }

        private static bool HasEmptySerializeField(GameObject obj)
        {
            // GetComponent<Component>() はインスペクターに表示されない内部コンポーネントも含むため注意
            foreach (var c in obj.GetComponents<Component>())
            {
                if (c == null || !(c is MonoBehaviour)) continue;

                // 自分が作成したスクリプト以外（Unity標準やパッケージ）は無視する
                if (c.GetType().Assembly.GetName().Name != "Assembly-CSharp") continue;

                var type = c.GetType();
                
                // キャッシュからフィールド一覧を取得、なければ作成
                if (!CachedFields.TryGetValue(type, out var fieldList))
                {
                    fieldList = CacheFieldInfos(type);
                    CachedFields[type] = fieldList;
                }

                // 各フィールドのチェック
                foreach (var cachedField in fieldList)
                {
                    object value = cachedField.Field.GetValue(c);
                    if (value == null)
                    {
                        // 値そのものが null の場合は、単一参照・リスト問わず未アサイン（None）状態
                        return true;
                    }

                    if (cachedField.IsCollection)
                    {
                        // List や 配列の中身を走査
                        if (value is IEnumerable collection)
                        {
                            foreach (var item in collection)
                            {
                                // 要素が Unity オブジェクト、かつ None（または Missing）かチェック
                                if (item is UnityEngine.Object objRef)
                                {
                                    if (objRef == null || objRef.Equals(null))
                                    {
                                        return true; // 1つでも None があれば即座に赤くする
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // 通常の単一オブジェクト参照のチェック
                        if (value.Equals(null))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 指定された型のシリアライズ対象フィールドを抽出し、キャッシュ用リストを作成する
        /// </summary>
        private static List<CachedFieldInfo> CacheFieldInfos(Type type)
        {
            var list = new List<CachedFieldInfo>();
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                // [SerializeField] がついている、または [HideInInspector] がついていないパブリックフィールドか
                bool isPublic = field.IsPublic;
                bool hasSerialize = field.GetCustomAttribute<SerializeField>() != null;
                bool isHidden = field.GetCustomAttribute<HideInInspector>() != null;

                if (!hasSerialize && (!isPublic || isHidden)) continue;

                // 型の判定
                bool isUnityObject = typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType);
                bool isCollection = typeof(IEnumerable).IsAssignableFrom(field.FieldType) && field.FieldType != typeof(string);

                if (isUnityObject)
                {
                    list.Add(new CachedFieldInfo { Field = field, IsCollection = false });
                }
                else if (isCollection)
                {
                    // ジェネリックリストや配列の「要素の型」が Unity オブジェクトであるかを確認
                    Type elementType = null;
                    if (field.FieldType.IsArray)
                    {
                        elementType = field.FieldType.GetElementType();
                    }
                    else if (field.FieldType.IsGenericType)
                    {
                        elementType = field.FieldType.GetGenericArguments()[0];
                    }

                    if (elementType != null && typeof(UnityEngine.Object).IsAssignableFrom(elementType))
                    {
                        list.Add(new CachedFieldInfo { Field = field, IsCollection = true });
                    }
                }
            }
            return list;
        }
    }
}
