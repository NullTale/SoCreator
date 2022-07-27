using System;
using UnityEngine;

namespace SOCreator
{
    /// <summary>
    /// Allows to show scriptable object in create menu
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SOCreateAttribute : PropertyAttribute
    {
        public Mode Visibility;
        
        [Serializable]
        public enum Mode
        {
            Hidden,
            Visible,
            AlwaysVisible
        }
        
        // =======================================================================
        public SOCreateAttribute(bool visible = true)
        {
            Visibility = visible ? Mode.Visible : Mode.Hidden;
        }
        
        public SOCreateAttribute(Mode visibility)
        {
            Visibility = visibility;
        }
    }
}