using System;
using UnityEngine;

//  SoCreator © NullTale - https://twitter.com/NullTale/
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
        public int  Priority;
        
        // =======================================================================
        [Serializable]
        public enum Mode
        {
            Hidden,
            Visible,
            AlwaysVisible
        }
        
        // =======================================================================
        public SoCreateAttribute() : this(true)
        {
        }
        
        public SoCreateAttribute(bool visible, bool useForChildren = true, int priority = 0)
        {
            Priority = priority;
            Visibility = visible ? Mode.AlwaysVisible : Mode.Hidden;
            UseForChildren = useForChildren;
        }
        
        public SoCreateAttribute(Mode visibility, bool useForChildren = true, int priority = 0)
        {
            Priority = priority;
            Visibility = visibility;
            UseForChildren = useForChildren;
        }
    }
}