using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SOCreator
{
    public class SettingsProvider : UnityEditor.SettingsProvider
    {
        public const string k_AllAssemblies   = nameof(SOCreator) + ".AllAssemblies";
        public const string k_Width           = nameof(SOCreator) + ".Width";
        public const string k_MaxItems        = nameof(SOCreator) + ".MaxItems";
        public const string k_ShowNamespace   = nameof(SOCreator) + ".ShowNamespace";
        public const string k_PrefsFile       = nameof(SOCreator) + "Prefs.json";
        public const string k_PrefsPath       = "ProjectSettings\\" + k_PrefsFile;

        public const bool k_ShowNamespaceDefault = true;
        public const int  k_WeightDefault        = 320;
        public const int  k_MaxItemsDefault      = 40;

        public static List<AssemblyDefinitionAsset> s_Assemblies;
        
        private ReorderableList _list;

        // =======================================================================
        [Serializable]
        private class JsonWrapper
        {
            public List<string> Assemblies;
        }
        
        
        // =======================================================================
        private SettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope)
        {
            if (!EditorPrefs.HasKey(k_AllAssemblies))
                EditorPrefs.SetBool(k_AllAssemblies, false);
            
            if (!EditorPrefs.HasKey(k_ShowNamespace))
                EditorPrefs.SetBool(k_ShowNamespace, k_ShowNamespaceDefault);
            
            if (!EditorPrefs.HasKey(k_Width))
                EditorPrefs.SetInt(k_Width, k_WeightDefault);
            
            if (!EditorPrefs.HasKey(k_MaxItems))
                EditorPrefs.SetInt(k_MaxItems, k_MaxItemsDefault);
        }
        
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            s_Assemblies = new List<AssemblyDefinitionAsset>();
            if (File.Exists(k_PrefsPath))
            {
                using var file = File.OpenText(k_PrefsPath);
                var       data = JsonUtility.FromJson<JsonWrapper>(file.ReadToEnd());
                
                s_Assemblies = data.Assemblies
                                   .Select(guid => AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(AssetDatabase.GUIDToAssetPath(guid)))
                                   .ToList();
            }
        }

        public override void OnGUI(string searchContext)
        {
            //EditorGUILayout.ObjectField(null, typeof(AssemblyDefinitionAsset), false);
            EditorGUI.BeginChangeCheck();
            var allAssambles  = EditorGUILayout.Toggle(new GUIContent("All Assemblies", "Search in all assemblies by default"), EditorPrefs.GetBool(k_AllAssemblies));
            var showNamespace = EditorGUILayout.Toggle(new GUIContent("Full names", "show namespace in type name"), EditorPrefs.GetBool(k_ShowNamespace));
            var width         = EditorGUILayout.IntField(new GUIContent("Width", "Window width"), EditorPrefs.GetInt(k_Width));
            var maxItems      = EditorGUILayout.IntField(new GUIContent("Max items", "Max elements in popup window"), EditorPrefs.GetInt(k_MaxItems));
            
            EditorGUILayout.Space(7);
            _getList().DoLayoutList();
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(k_AllAssemblies, allAssambles);
                EditorPrefs.SetBool(k_ShowNamespace, showNamespace);
                EditorPrefs.SetInt(k_Width, Mathf.Max(width, PickerWindow.k_Width));
                EditorPrefs.SetInt(k_MaxItems, Mathf.Max(maxItems, 7));
            }
        }

        private ReorderableList _getList()
        {
            if (_list != null)
                return _list;
            
            _list = new ReorderableList(s_Assemblies, typeof(AssemblyDefinitionAsset), true, true, true, true);
            _list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                EditorGUI.BeginChangeCheck();
                var asm = EditorGUI.ObjectField(rect, GUIContent.none, s_Assemblies[index], typeof(AssemblyDefinitionAsset), false);
                if (EditorGUI.EndChangeCheck())
                {
                    s_Assemblies[index] = (AssemblyDefinitionAsset)asm;
                    _saveProjectPrefs();
                }
            };
            _list.elementHeight = EditorGUIUtility.singleLineHeight;
            _list.onRemoveCallback = list =>
            {
                var toRemove = list.selectedIndices.Select(index => s_Assemblies[index]).ToList();
                s_Assemblies.RemoveAll(n => toRemove.Contains(n));
                _saveProjectPrefs();
            };
            _list.onAddCallback = list =>
            {
                s_Assemblies.Add(null);
            };
            _list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, new GUIContent("Include assemblies", "Which assemblies to include in the search"));
            };
            return _list;
        
        }
        
        private void _saveProjectPrefs()
        {
            var json = new JsonWrapper()
            {
                Assemblies = s_Assemblies
                             .Select(asset => AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(asset)))
                             .Where(n => n.Empty() == false)
                             .Select(n => n.ToString())
                             .ToList()
            };
            
            File.WriteAllText(k_PrefsPath, JsonUtility.ToJson(json));
        }
        
        [SettingsProvider]
        public static UnityEditor.SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new SettingsProvider("Preferences/SOCreator", SettingsScope.User);

            // Automatically extract all keywords from the Styles.
            //provider.keywords = GetSearchKeywordsFromGUIContentProperties<Styles>();
            return provider;
        }
    }
}