using UnityEditor;
using UnityEngine;

//  SoCreator © NullTale - https://twitter.com/NullTale/
namespace SoCreator
{
    public static class InternalGUI
    {
        public static GUIStyle HeaddingStyle;
        public static GUIStyle ButtonStyle;
        public static GUIStyle SelectedButtonStyle;

        private static GUIStyle s_SearchBarStyle;
        private static GUIStyle s_SearchBarEndStyle;
        private static GUIStyle s_SearchBarCancelStyle;


        public static float SearchBarHeight => EditorStyles.toolbar.fixedHeight;
        public static float ScrollBarHeight => 13;

        public static float LabelHeight => ButtonStyle.fixedHeight;

        public static GUIStyle SearchBarStyle
        {
            get
            {
                if (s_SearchBarStyle == null)
#if UNITY_2022_3_OR_NEWER
                    s_SearchBarStyle = GUI.skin.FindStyle("ToolbarSearchTextField");
#else
                    s_SearchBarStyle = GUI.skin.FindStyle("ToolbarSeachTextField");
#endif
                return s_SearchBarStyle;
            }
        }

        public static GUIStyle SearchBarEndStyle
        {
            get
            {
                if (s_SearchBarEndStyle == null)
#if UNITY_2022_3_OR_NEWER
                    s_SearchBarEndStyle = GUI.skin.FindStyle("ToolbarSearchCancelButtonEmpty");
#else
                    s_SearchBarEndStyle = GUI.skin.FindStyle("ToolbarSeachCancelButtonEmpty");
#endif
                return s_SearchBarEndStyle;
            }
        }

        public static GUIStyle SearchBarCancelStyle
        {
            get
            {
                if (s_SearchBarCancelStyle == null)
#if UNITY_2022_3_OR_NEWER
                    s_SearchBarCancelStyle = GUI.skin.FindStyle("ToolbarSearchCancelButton");
#else
                    s_SearchBarCancelStyle = GUI.skin.FindStyle("ToolbarSeachCancelButton");
#endif
                return s_SearchBarCancelStyle;
            }
        }

        static InternalGUI()
        {
            HeaddingStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize    = 12,
                alignment   = TextAnchor.MiddleLeft,
                fixedHeight = 22
            };

            ButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                alignment   = TextAnchor.MiddleLeft,
                fontSize    = 12,
                fixedHeight = 17
            };

            SelectedButtonStyle = new GUIStyle(ButtonStyle)
            {
                fontStyle = FontStyle.Bold
            };
        }
    }
}