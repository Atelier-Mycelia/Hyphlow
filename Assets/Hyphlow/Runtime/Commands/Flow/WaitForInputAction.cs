#if ENABLE_INPUT_SYSTEM

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Waits for a duration, but continues early if any configured InputAction is performed.
    /// </summary>
    [CommandInfo("Flow",
                 "Wait For Input Action",
                 "Waits for a period of time before executing the next command, or finishes early " +
                 "if any specified Input Action is performed.")]
    [AddComponentMenu("")]
    [ExecuteInEditMode]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class WaitForInputAction : Wait
    {
        [Tooltip("Objects expected to be InputActionReference instances (directly or via ObjectVariables).")]
        [SerializeField] protected List<ObjectData> _inputActionRefs =
            new List<ObjectData>() { new ObjectData() };

        [Tooltip("If true, enables an action while waiting when it is currently disabled.")]
        [SerializeField] protected BooleanData _enableActionIfNeeded = new BooleanData(true);

        [HideInInspector]
        [FormerlySerializedAs("_inputActionRef")]
        [SerializeField] protected ObjectData _oldInputActionRef;

        protected readonly List<InputAction> _subscribedActions = new List<InputAction>();
        protected readonly HashSet<InputAction> _actionsEnabledForWait = new HashSet<InputAction>();

        protected bool _completed;

        protected override void OnEnable()
        {
            base.OnEnable();
            TryMigrateOldInputActionField();
        }

        protected virtual void TryMigrateOldInputActionField()
        {
            if (_oldInputActionRef == null)
            {
                return;
            }

            EnsureInputActionRefList();

            bool oldInputActionIsEmpty = IsInputActionRefEntryEmpty(_oldInputActionRef);
            bool listAlreadyHasAtLeastOneItem = _inputActionRefs.Count > 0;

            // Prevent play-mode/domain-reload noise from appending an empty legacy entry
            // once the new list-based field is already in use.
            if (oldInputActionIsEmpty && listAlreadyHasAtLeastOneItem)
            {
                _oldInputActionRef = null;
                return;
            }

            bool replacedEmptyEntry = false;
            for (int i = 0; i < _inputActionRefs.Count; i++)
            {
                if (!IsInputActionRefEntryEmpty(_inputActionRefs[i]))
                {
                    continue;
                }

                _inputActionRefs[i] = _oldInputActionRef;
                replacedEmptyEntry = true;
                break;
            }

            if (!replacedEmptyEntry && !oldInputActionIsEmpty)
            {
                _inputActionRefs.Add(_oldInputActionRef);
            }

            _oldInputActionRef = null;//
        }

        protected virtual void EnsureInputActionRefList()
        {
            _inputActionRefs ??= new List<ObjectData>();
            if (_inputActionRefs.Count == 0)
            {
                _inputActionRefs.Add(new ObjectData());
            }
        }

        protected virtual bool IsInputActionRefEntryEmpty(ObjectData inputActionRef)
        {
            return inputActionRef == null ||
                   (inputActionRef.Value == null && inputActionRef.VarRef == null);
        }

        protected override void RefreshVariableDataCache()
        {
            base.RefreshVariableDataCache();
            EnsureInputActionRefList();

            for (int i = 0; i < _inputActionRefs.Count; i++)
            {
                ObjectData inputActionRef = _inputActionRefs[i];
                if (inputActionRef == null)
                {
                    inputActionRef = new ObjectData();
                    _inputActionRefs[i] = inputActionRef;
                }

                _variableDataCache.Add(inputActionRef);
            }

            _variableDataCache.Add(_enableActionIfNeeded);
        }

        public override void OnEnter()
        {
            _completed = false;
            CleanupInputActionSubs();
            TrySubscribeToInputActions();
            base.OnEnter();
        }

        protected virtual void CleanupInputActionSubs()
        {
            for (int i = 0; i < _subscribedActions.Count; i++)
            {
                InputAction action = _subscribedActions[i];
                if (action == null)
                {
                    continue;
                }

                action.performed -= OnInputActionPerformed;

                if (_actionsEnabledForWait.Contains(action))
                {
                    action.Disable();
                }
            }

            _subscribedActions.Clear();
            _actionsEnabledForWait.Clear();
        }

        protected virtual void TrySubscribeToInputActions()
        {
            EnsureInputActionRefList();

            for (int i = 0; i < _inputActionRefs.Count; i++)
            {
                ObjectData inputActionData = _inputActionRefs[i];
                if (inputActionData == null)
                {
                    continue;
                }

                InputActionReference inputActionReference = inputActionData.Value as InputActionReference;
                if (inputActionReference == null)
                {
                    if (inputActionData.Value != null)
                    {
                        string wrongTypeMessage = $"WaitForInputAction on {name} has an entry at index {i} " +
                            $"that is not an InputActionReference.";
                        Debug.LogError(wrongTypeMessage, this);
                    }

                    continue;
                }

                InputAction action = inputActionReference.action;
                if (action == null)
                {
                    string noActionMessage = $"WaitForInputAction on {name} has an InputActionReference " +
                        $"at index {i} with no action set.";
                    Debug.LogWarning(noActionMessage, this);
                    continue;
                }

                if (_subscribedActions.Contains(action))
                {
                    continue;
                }

                if (_enableActionIfNeeded.Value && !action.enabled)
                {
                    action.Enable();
                    _actionsEnabledForWait.Add(action);
                }

                action.performed += OnInputActionPerformed;
                _subscribedActions.Add(action);
            }
        }

        protected virtual void OnInputActionPerformed(InputAction.CallbackContext callbackContext)
        {
            string actionName = callbackContext.action != null ? 
                callbackContext.action.name : 
                "<unknown>";
            string logMessage = $"WaitForInputAction on {name} received input from action {actionName}, " +
                "finishing wait early.";
            Debug.Log(logMessage, this);

            CancelInvoke(nameof(OnWaitComplete));
            OnWaitComplete();
        }

        protected override void OnWaitComplete()
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
            CleanupInputActionSubs();
            base.OnWaitComplete();
        }

        public override void OnStopExecuting()
        {
            base.OnStopExecuting();
            CancelInvoke(nameof(OnWaitComplete));
            CleanupInputActionSubs();
            _completed = false;
        }

        public override string GetSummary()
        {
            string waitSummary = base.GetSummary();
            int configuredActionCount = 0;

            EnsureInputActionRefList();
            for (int i = 0; i < _inputActionRefs.Count; i++)
            {
                InputActionReference inputActionReference = _inputActionRefs[i]?.Value as InputActionReference;
                if (inputActionReference?.action != null)
                {
                    configuredActionCount++;
                }
            }

            return configuredActionCount > 0
                ? $"{waitSummary} or any of {configuredActionCount} input action(s)"
                : $"{waitSummary} (no input actions)";
        }

        public override bool HasReference(IVariable variable)
        {
            for (int i = 0; i < _inputActionRefs.Count; i++)
            {
                if (ReferenceEquals(_inputActionRefs[i]?.VarRef, variable))
                {
                    return true;
                }
            }

            bool result = ReferenceEquals(_enableActionIfNeeded.VarRef, variable) ||
                          base.HasReference(variable);

            return result;
        }

        

        

    }
}

#endif