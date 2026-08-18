using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Handles attaching IControlPanelEntry instances to the ControlPanel window.
    /// Initializes entries and adds their UI elements to the appropriate containers.
    /// </summary>
    public sealed class ControlPanelEntryAttacher : IDisposable
    {
        public ControlPanelEntryAttacher(VisualElement rootElement)
        {
            GetVisualElements();
            void GetVisualElements()
            {
                _rootElement = rootElement ?? throw new ArgumentNullException(nameof(rootElement));
                _mainTabSet = _rootElement.Q<VisualElement>("MainTabSet");
                _subwindowDisplay = _rootElement.Q<VisualElement>("CategorySubwindowDisplay");
            }

            ReportErrorsAsNeeded();
            void ReportErrorsAsNeeded()
            {
                string errorMessage = "";
                if (_mainTabSet == null)
                {
                    errorMessage += "Could not find 'MainTabSet' in " +
                        "ControlPanel UXML";
                }

                if (_subwindowDisplay == null)
                {
                    errorMessage += "\n\nCould not find 'CategorySubwindowDisplay' " +
                        "in ControlPanel UXML";
                }

                bool anyErrorsFound = !string.IsNullOrEmpty(errorMessage);
                if (anyErrorsFound)
                {
                    throw new InvalidOperationException(errorMessage);
                }
            }
        }

        private VisualElement _rootElement;
        private VisualElement _mainTabSet;
        private VisualElement _subwindowDisplay;

        public void AttachAllEntries()
        {
            _entries = ControlPanelEntryRegistry.Entries.ToList();

            foreach (var elem in _entries)
            {
                Attach(elem);
            }
        }

        private IList<IControlPanelEntry> _entries;

        private void Attach(IControlPanelEntry entry)
        {
            try
            {
                entry.Init(forceReinit: false); // To save on clock cycles

                _mainTabSet.Add(entry.TabButton);
                _subwindowDisplay.Add(entry.Subwindow);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to attach {entry.GetType().Name} " +
                    $"to ControlPanel: {ex.Message}");
            }
        }

        public void Dispose()
        {
            DisposeExistingEntries();
        }

        private void DisposeExistingEntries()
        {
            if (_entries == null)
            {
                return;
            }

            foreach (var elem in _entries)
            {
                if (elem is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error disposing {elem.GetType().Name}: {ex.Message}");
                    }
                }
            }

            _entries.Clear();
            _entries = null;
        }

        
        

        
        
    }
}