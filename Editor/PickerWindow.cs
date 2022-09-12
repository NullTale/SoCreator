using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace SoCreator
{
    public sealed class PickerWindow : EditorWindow
    {
        public const int k_Width = 212;
        
        private readonly List<GUIContent>       Labels = new List<GUIContent>();
        private readonly List<GUIContent>       SearchedLabels = new List<GUIContent>();
        private readonly List<object>           SearchedObjects = new List<object>();

        private object               m_PickedObject;
        private object               m_SelectedObject;
        private object               m_HoverObject;
        private IList                m_Objects;
        private int                  m_Suggestions;
        private int                  m_LabelWidthCalculationProgress;
        private float                m_MaxLabelWidth;
        private string               m_SearchText = "";
        private Vector2              m_ScrollPosition;
 
        private bool                 HasSearchText => string.IsNullOrEmpty(m_SearchText) == false;
        public string                SearchText => m_SearchText;
        public bool                  IsPeeked => m_PickedObject != null;

        private Action<object>       m_OnPicked;
        private Action<object>       m_OnHover;
        private Action<object>       m_OnSeleceted;
        private Action<PickerWindow> m_OnClose;
        private bool                 m_FirstClickTrigger;

        private bool                 m_HoverObjectPicked;
        private bool                 m_Minimalistic;
        private bool                 m_InitializedPosition;

        // =======================================================================
        public static void Show<T>(Action<object> onPicked, T selected, List<T> objects, int suggestions, Func<T, GUIContent> getLabel,
                                   string title = null, bool firstClickTrigger = true, Action<object> onSelected = null, Action<object> onHover = null,
                                   Action<PickerWindow> onClose = null, float width = k_Width, int maxElements = 7, string searchText = null)
        {
            if (objects == null || objects.Count == 0)
            {
                Debug.LogWarning("'objects' list is null or empty.");
                return;
            }

            var window = CreateInstance<PickerWindow>();
            window.titleContent = title == null ? new GUIContent("Pick a " + typeof(T).FullName) : new GUIContent(title);
            window.minSize = new Vector2(112, 20);

            if (objects.Count <= 7)
                window.m_Minimalistic = true;

            if (window.Labels.Capacity < objects.Count)
                window.Labels.Capacity = objects.Count;

            foreach (var item in objects)
                window.Labels.Add(getLabel(item));

            window.m_SelectedObject    = selected;
            window.m_Objects           = objects;
            window.m_Suggestions       = suggestions;
            window.m_FirstClickTrigger = firstClickTrigger;
            window.m_OnPicked          = onPicked;
            window.m_OnSeleceted       = onSelected;
            window.m_OnHover           = onHover;
            window.m_OnClose           = onClose;
            window.position            = new Rect(0, 0, Mathf.Max(width, k_Width), Mathf.Min(objects.Count, maxElements) * (InternalGUI.ButtonStyle.fixedHeight) + 6 + (window.m_Minimalistic ? 0 : InternalGUI.SearchBarHeight));
            
            // Auto-Scroll to the selected object.
            if (selected != null)
            {
                object sel = selected;

                for (var i = 0; i < window.m_Objects.Count; i++)
                {
                    if (Equals(sel, window.m_Objects[i]))
                    {
                        window.m_ScrollPosition = new Vector2(0, i * InternalGUI.LabelHeight);
                        break;
                    }
                }
            }
            
            if (string.IsNullOrEmpty(searchText) == false)
                window._onSearchTextChanged(searchText);

            window.ShowPopup();
        }

        private void Update()
        {
            if (focusedWindow != this)
                Close();
        }

        private void OnGUI()
        {
            if (m_InitializedPosition == false)
            {
                var mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                position             = new Rect(mousePos.x, mousePos.y, position.width, position.height);
                m_InitializedPosition = true;
            }

            switch (Event.current.type)
            {
                case EventType.MouseMove:
                case EventType.Layout:
                case EventType.DragUpdated:
                case EventType.DragPerform:
                case EventType.DragExited:
                case EventType.Ignore:
                case EventType.Used:
                case EventType.ValidateCommand:
                case EventType.ExecuteCommand:
                case EventType.ContextClick:
                    return;

                default:
                    break;
            }

            if (_checkInput())
            {
                Event.current.Use();
                return;
            }

            _updateLabelWidthCalculation();

            var area = new Rect(0, 0, position.width, position.height);

            if (m_Minimalistic == false)
                _drawSearchBar(ref area);

            area.yMax = position.height;

            var viewRect = _calculateViewRect(area.height);

            // Selection List.
            m_ScrollPosition = GUI.BeginScrollView(area, m_ScrollPosition, viewRect);
            {
                // Figure out how many fields are actually visible.
                _determineVisibleRange(out var firstVisibleField, out var lastVisibleField);

                if (HasSearchText)// Active Search.
                {
                    _drawSearchedOptions(viewRect, firstVisibleField, lastVisibleField);
                }
                else// No Search.
                {
                    _drawAllOptions(viewRect, firstVisibleField, lastVisibleField);
                }
            }
            GUI.EndScrollView(true);
        }

        private void OnDisable()
        {
            m_OnClose?.Invoke(this);
        }
    
        // =======================================================================
        private void _updateLabelWidthCalculation()
        {
            if (m_LabelWidthCalculationProgress < Labels.Count)
            {
                var calculationCount = 0;
                do
                {
                    var label = Labels[m_LabelWidthCalculationProgress];

                    var width = InternalGUI.ButtonStyle.CalcSize(label).x;
                    if (m_MaxLabelWidth < width)
                        m_MaxLabelWidth = width;
                }
                while (++m_LabelWidthCalculationProgress < Labels.Count && calculationCount++ < 100);

                Repaint();
            }
        }
    
        private bool _checkInput()
        {
            var currentEvent = Event.current;
            if (currentEvent.type == EventType.KeyDown)
            {
                switch (currentEvent.keyCode)
                {
                    case KeyCode.Return:
                        _pickAndClose();
                        return true;

                    case KeyCode.Escape:
                        Close();
                        return true;

                    case KeyCode.UpArrow:
                        _offsetSelectedIndex(-1);
                        return true;

                    case KeyCode.DownArrow:
                        _offsetSelectedIndex(1);
                        return true;
                }
            }

            return false;
        }
    
        private void _drawSearchBar(ref Rect area)
        {
            area.height = InternalGUI.SearchBarHeight;
            GUI.BeginGroup(area, EditorStyles.toolbar);
            {
                area.x += 2;
                area.y += 2;
                area.width -= InternalGUI.SearchBarEndStyle.fixedWidth + 4;

                GUI.SetNextControlName("SearchFilter");
                EditorGUI.BeginChangeCheck();
                var searchText = GUI.TextField(area, m_SearchText, InternalGUI.SearchBarStyle);
                if (EditorGUI.EndChangeCheck())
                    _onSearchTextChanged(searchText);
                EditorGUI.FocusTextInControl("SearchFilter");

                area.x = area.xMax;
                area.width = InternalGUI.SearchBarEndStyle.fixedWidth;
                if (HasSearchText)
                {
                    if (GUI.Button(area, "", InternalGUI.SearchBarCancelStyle))
                    {
                        m_SearchText = "";
                    }
                }
                else
                    GUI.Box(area, "", InternalGUI.SearchBarEndStyle);
            }
            GUI.EndGroup();

            area.x = 0;
            area.width = position.width;
            area.y += area.height;
        }
    
        private void _onSearchTextChanged(string text)
        {
            if (text == m_SearchText)
                return;

            if (string.IsNullOrEmpty(text))
            {
                SearchedLabels.Clear();
                SearchedObjects.Clear();
            }
            // If the search text starts the same as before, it will include only a subset of the previous options.
            // So we can just remove objects from the previous search list instead of checking the full list again.
            else if (SearchedLabels.Count > 0 && text.StartsWith(m_SearchText))
            {
                for (var i = SearchedLabels.Count - 1; i >= 0; i--)
                {
                    if (!_isVisibleInSearch(text, SearchedLabels[i].text))
                    {
                        SearchedLabels.RemoveAt(i);
                        SearchedObjects.RemoveAt(i);
                    }
                }
            }
            // Otherwise clear the search list and re-gather any visible objects from the full list.
            else
            {
                SearchedLabels.Clear();
                SearchedObjects.Clear();

                for (var i = 0; i < Labels.Count; i++)
                {
                    var label = Labels[i];
                    if (_isVisibleInSearch(text, label.text))
                    {
                        SearchedLabels.Add(label);
                        SearchedObjects.Add(m_Objects[i]);
                    }
                }
            }

            m_SearchText = text;

            if (!SearchedObjects.Contains(m_SelectedObject))
                m_SelectedObject = SearchedObjects.Count > 0 ? SearchedObjects[0] : null;
        }

        private bool _isVisibleInSearch(string search, string text)
        {
            return CultureInfo.CurrentCulture.CompareInfo.IndexOf(text, search, CompareOptions.IgnoreCase) >= 0;
        }
    
        private Rect _calculateViewRect(float height)
        {
            var area = new Rect();

            if (HasSearchText)
            {
                area.height = InternalGUI.LabelHeight * SearchedLabels.Count;
            }
            else
            {
                area.height = InternalGUI.LabelHeight * Labels.Count;

                if (m_Suggestions > 0)
                    area.height += InternalGUI.HeaddingStyle.fixedHeight * 2;
            }

            if (m_MaxLabelWidth < position.width)
            {
                area.width = position.width;

                if (area.height > height)
                    area.width -= 16;
            }
            else
                area.width = m_MaxLabelWidth;

            return area;
        }
    
        private void _determineVisibleRange(out int firstVisibleField, out int lastVisibleField)
        {
            var top = m_ScrollPosition.y;
            var bottom = top + position.height - InternalGUI.SearchBarHeight;
            if (m_Suggestions > 0)
            {
                top -= InternalGUI.HeaddingStyle.fixedHeight * 2;
                bottom += InternalGUI.HeaddingStyle.fixedHeight;
            }

            firstVisibleField = Mathf.Max(0, (int)(top / InternalGUI.LabelHeight));
            lastVisibleField = Mathf.Min(Labels.Count, Mathf.CeilToInt(bottom / InternalGUI.LabelHeight));
        }
    
        private void _drawAllOptions(Rect area, int firstVisibleField, int lastVisibleField)
        {
            if (m_Suggestions == 0 || m_Suggestions >= Labels.Count)
            {
                area.y = firstVisibleField * InternalGUI.LabelHeight;
                _drawRange(ref area, Labels, m_Objects, firstVisibleField, lastVisibleField);
            }
            else
            {
                area.height = InternalGUI.HeaddingStyle.fixedHeight;
                GUI.Label(area, "Suggestions", InternalGUI.HeaddingStyle);

                area.y = area.yMax + firstVisibleField * InternalGUI.LabelHeight;
                _drawRange(ref area, Labels, m_Objects, firstVisibleField, Mathf.Min(lastVisibleField, m_Suggestions));

                area.height = InternalGUI.HeaddingStyle.fixedHeight;
                GUI.Label(area, "Other Options", InternalGUI.HeaddingStyle);
                area.y = area.yMax;

                _drawRange(ref area, Labels, m_Objects, Mathf.Max(m_Suggestions, firstVisibleField), lastVisibleField);
            }
        }
    
        private void _drawSearchedOptions(Rect area, int firstVisibleField, int lastVisibleField)
        {
            area.y = firstVisibleField * InternalGUI.LabelHeight;
            _drawRange(ref area, SearchedLabels, SearchedObjects, firstVisibleField, lastVisibleField);
        }
    
        private void _drawRange(ref Rect area, List<GUIContent> labels, IList objects, int start, int end)
        {
            area.height = InternalGUI.LabelHeight;

            if (end > labels.Count)
                end = labels.Count;

            m_HoverObjectPicked = false;
            for (; start < end; start++)
            {
                _drawOption(area, labels, objects, start);
                area.y = area.yMax;
            }
            if (m_HoverObjectPicked == false)
                _setHoverObject(null);
        }
    
        private void _drawOption(Rect area, List<GUIContent> labels, IList objects, int index)
        {
            var obj = objects[index];
            var wasOn = Equals(obj, m_SelectedObject);
            
            var isOn = GUI.Toggle(area, wasOn, labels[index], wasOn ? InternalGUI.SelectedButtonStyle : InternalGUI.ButtonStyle);
            if (isOn != wasOn)
            {
                m_SelectedObject = obj;
                m_OnSeleceted?.Invoke(m_SelectedObject);
                if (wasOn || m_FirstClickTrigger)
                    _pickAndClose();
            }
            
            if (area.Contains(Event.current.mousePosition))
            {
                m_HoverObjectPicked = true;
                _setHoverObject(obj);
            }
        }

        private void _setHoverObject(object obj)
        {
            if (Equals(obj, m_HoverObject) == false)
            {
                m_HoverObject = obj;
                m_OnHover?.Invoke(m_HoverObject);
            }
        }
    
        private void _pickAndClose()
        {
            m_PickedObject = m_SelectedObject;
            Close();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

            m_OnPicked?.Invoke(m_PickedObject);
        }
    
        private void _offsetSelectedIndex(int offset)
        {
            var objects = HasSearchText ? SearchedObjects : m_Objects;

            if (objects.Count == 0)
                return;

            var index = objects.IndexOf(m_SelectedObject);
            m_SelectedObject = objects[Mathf.Clamp(index + offset, 0, objects.Count - 1)];
            m_OnSeleceted?.Invoke(m_SelectedObject);
        }
    }
}
