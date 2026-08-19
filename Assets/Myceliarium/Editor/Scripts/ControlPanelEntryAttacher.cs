using System;
using System.Collections.Generic;
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
        public void Init(VisualElement rootElement)
        {
            _isDisposed = false;
            GetVisualElements();
            void GetVisualElements()
            {
                _rootElement = rootElement ?? throw new ArgumentNullException(nameof(rootElement));
                _mainTabSet = _rootElement.Q<VisualElement>("MainTabSet");
                _subwindowDisplay = _rootElement.Q<ScrollView>("CategorySubwindowDisplay");
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

            ToggleSubs(true);
        }

        private bool _isDisposed = false;
        private VisualElement _rootElement;
        private VisualElement _mainTabSet;
        private ScrollView _subwindowDisplay;

        private void ToggleSubs(bool on)
        {
            if (on)
            {
                ControlPanelSignals.OnEntryTabClicked += OnEntryTabClicked;
            }
            else
            {
                ControlPanelSignals.OnEntryTabClicked -= OnEntryTabClicked;
            }
        }

        private void OnEntryTabClicked(IControlPanelEntry entryForClicked)
        {
            bool ignoreIt = _entries == null || !_entries.Contains(entryForClicked);
            if (ignoreIt)
            {
                return;
            }

            bool currentlyShowingEntry = _entryBeingDisplayed != null;
            bool switchToOtherOne = entryForClicked != _entryBeingDisplayed;
            bool shouldHideCurrentOneFirst = currentlyShowingEntry && switchToOtherOne;
            if (shouldHideCurrentOneFirst)
            {
                var subwindowShowing = _entryBeingDisplayed.Subwindow;
                subwindowShowing.style.display = DisplayStyle.None;
            }

            if (switchToOtherOne)
            {
                _entryBeingDisplayed = entryForClicked;
                var subwindow = entryForClicked.Subwindow;
                subwindow.style.display = DisplayStyle.Flex;
                DeselectAllTabsExceptFor(entryForClicked.Tab);
            }
        }

        private IControlPanelEntry _entryBeingDisplayed;

        private void DeselectAllTabsExceptFor(IControlPanelTab toLeaveAlone)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var elem = _entries[i];
                var tab = elem.Tab;
                if (tab != toLeaveAlone)
                {
                    tab.IsSelected = false;
                }
            }
        }

        public void Attach(IList<IControlPanelEntry> toAttach)
        {
            _entries = toAttach ?? throw new ArgumentNullException(nameof(toAttach));
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

                _mainTabSet.Add(entry.Tab.Root);

                // We want the subwindows parented to the holder, but until
                // the user clicks on the tab, we don't want them to be visible.
                entry.Subwindow.style.display = DisplayStyle.None;
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
            if (_isDisposed)
            {
                return;
            }

            ToggleSubs(false);
            _entryBeingDisplayed = null;
            _entries = null;
            _mainTabSet = null;
            _subwindowDisplay = null;
            _rootElement = null;
            _isDisposed = true;
        }

    }
}