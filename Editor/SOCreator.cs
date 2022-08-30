using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using Assembly = System.Reflection.Assembly;

namespace SOCreator
{
    public static class SOCreator
    {
        private static Texture2D s_ScriptableObjectIcon = (EditorGUIUtility.IconContent("ScriptableObject Icon").image as Texture2D);

        // =======================================================================
        private class DoCreateFile : EndNameEditAction
        {
            public Type ObjectType;
            
            // =======================================================================
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                _create(pathName);
            }

            public override void Cancelled(int instanceId, string pathName, string resourceFile)
            {
                _create(pathName);
            }

            // =======================================================================
            private void _create(string pathName)
            {
                var so = ScriptableObject.CreateInstance(ObjectType);

                AssetDatabase.CreateAsset(so, Path.ChangeExtension(pathName, ".asset"));
                ProjectWindowUtil.ShowCreatedAsset(so);
            }
        }
        
        [Serializable]
        public class AssemblyDefinitionData
        {
            public string name;
        }

        // =======================================================================
        [MenuItem("Assets/Create/Scriptable Object", false, -1000)]
        private static void CreateScriptableObject(MenuCommand menuCommand)
        {
            var showNamespace  = EditorPrefs.GetBool(SettingsProvider.k_ShowNamespace);
            var keepSearchText = EditorPrefs.GetBool(SettingsProvider.k_KeepSearchText);
            var mainAssambly   = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(n => n.GetName().Name == "Assembly-CSharp");
            var onlyMain       = !EditorPrefs.GetBool(SettingsProvider.k_AllAssemblies);
            var additional     = SettingsProvider.s_Assemblies
                                                 .Where(n => n != null)
                                                 .Select(n => JsonUtility.FromJson<AssemblyDefinitionData>(n.text).name)
                                                 .Select(Assembly.Load)
                                                 .Where(n => n != null)
                                                 .ToList();
            
            if (GetGUIEvent()?.shift == true)
                onlyMain = false;
            
            var types = TypeCache.GetTypesDerivedFrom<ScriptableObject>()
                                 .Where(type =>
                                 {
                                     if (type.IsAbstract || type.IsGenericTypeDefinition)
                                         return false;

                                     var vibilityAttribute = type.GetCustomAttribute<SOCreateAttribute>();
                                     
                                     if (vibilityAttribute == null)
                                         vibilityAttribute = _getFirstInheritAttribute(type);
                                     
                                     if (vibilityAttribute != null)
                                     {
                                         switch (vibilityAttribute.Visibility)
                                         {
                                             case SOCreateAttribute.Mode.Hidden:
                                                 return false;
                                             case SOCreateAttribute.Mode.Visible:
                                                 return _defaultCheck(type);
                                             case SOCreateAttribute.Mode.AlwaysVisible:
                                                 return true;
                                             default:
                                                 throw new ArgumentOutOfRangeException();
                                         }
                                     }

                                     return _defaultCheck(type);
                                 })
                                 .ToList();

            var wndWidth = (float)EditorPrefs.GetInt(SettingsProvider.k_Width);
            var wndMaxItems =  EditorPrefs.GetInt(SettingsProvider.k_MaxItems);
            PickerWindow.Show(picked =>
            {
                var pickedType = (Type)picked;
                var doCreateFile = ScriptableObject.CreateInstance<DoCreateFile>();
                doCreateFile.ObjectType = pickedType;

                ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                    0,
                    doCreateFile,
                    pickedType.Name,
                    s_ScriptableObjectIcon,
                    string.Empty);
            }, null, types.Distinct().ToList(), 0, s => new GUIContent(showNamespace ? s.FullName : s.Name), 
            title: "ScriptableObject Type", 
            firstClickTrigger: true, 
            width: wndWidth, 
            maxElements: wndMaxItems,
            searchText: keepSearchText ? EditorPrefs.GetString(SettingsProvider.k_SearchText) : string.Empty,
            onClose: wnd =>
            { 
                EditorPrefs.SetString(SettingsProvider.k_SearchText, wnd.SearchText);
            });

            // -----------------------------------------------------------------------
            bool _defaultCheck(Type type)
            {
                if (additional.Contains(type.Assembly))
                    return true;

                if (onlyMain && type.Assembly != mainAssambly)
                    return false;

                return true;
            }
            
            SOCreateAttribute _getFirstInheritAttribute(Type type)
            {
                var current = type;
                while (current != null)
                {
                    var attribute = type.GetCustomAttribute<SOCreateAttribute>();
                    if (attribute != null && attribute.UseForChildren)
                        return attribute;
                    
                    current = current.BaseType;
                }
                
                return null;
            }
        }

        private static Event GetGUIEvent()
        {
            var field = typeof(Event).GetField("s_Current", BindingFlags.Static | BindingFlags.NonPublic);
            
            if (field != null && field.GetValue(null) is Event current)
                return current;
            
            return null;
        }
    }
}