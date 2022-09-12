using UnityEditor;
using UnityEngine;

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

        public static float LabelHeight => ButtonStyle.fixedHeight;

        public static GUIStyle SearchBarStyle
        {
            get
            {
                if (s_SearchBarStyle == null)
                    s_SearchBarStyle = GUI.skin.FindStyle("ToolbarSeachTextField");
                return s_SearchBarStyle;
            }
        }

        public static GUIStyle SearchBarEndStyle
        {
            get
            {
                if (s_SearchBarEndStyle == null)
                    s_SearchBarEndStyle = GUI.skin.FindStyle("ToolbarSeachCancelButtonEmpty");
                return s_SearchBarEndStyle;
            }
        }

        public static GUIStyle SearchBarCancelStyle
        {
            get
            {
                if (s_SearchBarCancelStyle == null)
                    s_SearchBarCancelStyle = GUI.skin.FindStyle("ToolbarSeachCancelButton");
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