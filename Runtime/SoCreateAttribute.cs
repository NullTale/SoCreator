using System;
using UnityEngine;

namespace SoCreator
{
    /// <summary>
    /// Allows to show scriptable object in create menu
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SoCreateAttribute : PropertyAttribute
    {
        public Mode Visibility;
        public bool UseForChildren;
        
        // =======================================================================
        [Serializable]
        public enum Mode
        {
            Hidden,
            Visible,
            AlwaysVisible
        }
        
        // =======================================================================
        public SoCreateAttribute(bool visible, bool useForChildren = true)
        {
            Visibility = visible ? Mode.AlwaysVisible : Mode.Hidden;
            UseForChildren = useForChildren;
        }
        
        public SoCreateAttribute(Mode visibility, bool useForChildren = true)
        {
            Visibility = visibility;
            UseForChildren = useForChildren;
        }
    }
}