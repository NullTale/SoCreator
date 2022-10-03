using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Assembly = System.Reflection.Assembly;
using Object = UnityEngine.Object;

namespace SoCreator
{
    public static class SoCreator
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
                //_create(pathName);
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
        [Shortcut("SoCreator/Create Scriptable Object using Type paths", KeyCode.I, ShortcutModifiers.Shift)]
        public static void CreateScriptableObjectToPath(ShortcutArguments sa)
        {
            CreateScriptableObject(true, true);
        }
        
        [Shortcut("SoCreator/Create Scriptable Object", KeyCode.I, ShortcutModifiers.Shift | ShortcutModifiers.Action)]
        public static void CreateScriptableObject(ShortcutArguments sa)
        {
            CreateScriptableObject(true, false);
        }
        
        [MenuItem("Assets/Create/Scriptable Object", false, -1000)]
        public static void CreateScriptableObject(MenuCommand menuCommand)
        {
            CreateScriptableObject(false, false);
        }
        
        public static void CreateScriptableObject(bool ignoreShift, bool forcePath)
        {
            var allAssemblies = (ignoreShift == false && GetGUIEvent()?.shift == true);

            var types = GetSoTypes(allAssemblies, type => type.IsAbstract == false && type.IsGenericTypeDefinition == false);

            if (types.Count == 0)
            {
                Debug.Log("SoCreator: no visible Scriptable Object types found");
                return;
            }
            
            var showNamespace  = EditorPrefs.GetBool(SettingsProvider.k_ShowNamespace);
            var keepSearchText = EditorPrefs.GetBool(SettingsProvider.k_KeepSearchText);
            var wndWidth       = (float)EditorPrefs.GetInt(SettingsProvider.k_Width);
            var wndMaxItems    =  EditorPrefs.GetInt(SettingsProvider.k_MaxItems);
            PickerWindow.Show(picked =>
                              {
                                  var pickedType   = (Type)picked;
                                  var doCreateFile = ScriptableObject.CreateInstance<DoCreateFile>();
                                  var path = pickedType.Name;
                                  if (forcePath)
                                  {
                                      var typeFolder = GetTypeFolder(pickedType);
                                      if (string.IsNullOrEmpty(typeFolder) == false)
                                        path = $"{typeFolder}\\{pickedType.Name}";
                                  }
                                  
                                  doCreateFile.ObjectType = pickedType;
                                  
                                  ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                                      0,
                                      doCreateFile,
                                      path,
                                      s_ScriptableObjectIcon,
                                      string.Empty);
                              }, null, types, 0, s => new GUIContent(showNamespace ? s.FullName : s.Name), 
                              title: "ScriptableObject Type", 
                              firstClickTrigger: true, 
                              width: wndWidth, 
                              maxElements: wndMaxItems,
                              searchText: keepSearchText ? EditorPrefs.GetString(SettingsProvider.k_SearchText) : string.Empty,
                              onClose: wnd =>
                              { 
                                  EditorPrefs.SetString(SettingsProvider.k_SearchText, wnd.SearchText);
                              });
        }
        
        public static List<Type> GetSoTypes(bool allAssemblies, Predicate<Type> filter)
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
            
            if (allAssemblies)
                onlyMain = false;
            
            var types = TypeCache.GetTypesDerivedFrom<ScriptableObject>()
                                 .Where(type =>
                                 {
                                     if (filter(type) == false)
                                         return false;

                                     var vibilityAttribute = type.GetCustomAttribute<SoCreateAttribute>();
                                     
                                     if (vibilityAttribute == null)
                                         vibilityAttribute = _getFirstInheritAttribute(type);
                                     
                                     if (vibilityAttribute != null)
                                     {
                                         switch (vibilityAttribute.Visibility)
                                         {
                                             case SoCreateAttribute.Mode.Hidden:
                                                 return false;
                                             case SoCreateAttribute.Mode.Visible:
                                                 return _defaultCheck(type);
                                             case SoCreateAttribute.Mode.AlwaysVisible:
                                                 return true;
                                             default:
                                                 throw new ArgumentOutOfRangeException();
                                         }
                                     }

                                     return _defaultCheck(type);
                                 })
                                 .Distinct()
                                 .ToList();

            return types;

            // -----------------------------------------------------------------------
            bool _defaultCheck(Type type)
            {
                if (additional.Contains(type.Assembly))
                    return true;

                if (onlyMain && type.Assembly != mainAssambly)
                    return false;

                return true;
            }
            
            SoCreateAttribute _getFirstInheritAttribute(Type type)
            {
                var current = type;
                while (current != null)
                {
                    var attribute = type.GetCustomAttribute<SoCreateAttribute>();
                    if (attribute != null && attribute.UseForChildren)
                        return attribute;
                    
                    current = current.BaseType;
                }
                
                return null;
            }
        }

        public static string GetTypeFolder(Type type)
        {
            foreach (var marker in _getBaseClasses().Prepend(type))
            {
                var path = SettingsProvider.s_TypeFolders.FirstOrDefault(typePath => isDerivedFrom(marker, typePath.Type));
                if (path != null)
                    return AssetDatabase.GetAssetPath(path.Path);
            }
            
            return string.Empty;

            // -----------------------------------------------------------------------
            bool isDerivedFrom(Type type, Type marker)
            {
                if (type == marker)
                    return true;
                
                if (type.IsGenericType && marker.IsGenericTypeDefinition)
                    return type.GetGenericTypeDefinition() == marker;
                
                return type.IsSubclassOf(marker) && type.BaseType.IsSubclassOf(marker) == false;
            }
            
            IEnumerable<Type> _getBaseClasses()
            {
                var current = type;
                do
                {
                    current = current.BaseType;
                    yield return current;
                }
                while (current != typeof(ScriptableObject));
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