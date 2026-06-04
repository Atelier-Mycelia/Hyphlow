using UnityEngine.Serialization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Abstract base class for TweenUI commands.
    /// </summary>
    [MovedFrom("AtMycelia.Amanita.VScripting.Commands")]
    public abstract class TweenUI : Command 
    {
        [Tooltip("List of objects to be affected by the tween")]
        [SerializeField]
        protected List<GameObjectData> _targetObjects = new List<GameObjectData>();

        [Tooltip("Whether to wait until this Command completes before continuing execution")]
        [SerializeField] [FormerlySerializedAs("waitUntilFinished")]
        protected BooleanData _waitUntilFinished = new BooleanData(true);
        
        [Tooltip("Time for the tween to complete")]
        [SerializeField] [FormerlySerializedAs("duration")]
        protected FloatData _duration = new FloatData(1f);

        protected override void Awake()
        {
            base.Awake();
            ValidateTweeners();
        }

        protected override void RefreshVariableCache()
        {
            base.RefreshVariableCache();
            _variableDataCache.Add(_waitUntilFinished);
            _variableDataCache.Add(_duration);
        }

        protected abstract void ValidateTweeners();

        protected virtual void ApplyTween()
        {
            ApplyToEachValidTarget();
            void ApplyToEachValidTarget()
            {
                for (int i = 0; i < _targetObjects.Count; i++)
                {
                    GameObject targetObject = _targetObjects[i].Value;
                    if (targetObject == null)
                    {
                        continue;
                    }
                    ApplyTweenToSingle(targetObject);
                }
            }

            if (_waitUntilFinished)
            {
                Invoke(nameof(OnComplete), _duration);
            }
        }

        protected abstract void ApplyTweenToSingle(GameObject go);

        protected virtual void OnComplete()
        {
            Continue();
        }

        protected virtual string GetSummaryValue()
        {
            return "";
        }

        #region Public members

        public override void OnEnter()
        {
            if (_targetObjects.Count == 0)
            {
                Continue();
                return;
            }
            
            ApplyTween();

            if (!_waitUntilFinished)
            {
                Continue();
            }
        }

        [SerializeField][FormerlySerializedAs("targetObjects")]
        [FormerlySerializedAs("_targetObjects")]
        protected List<GameObject> _targetObjectsOld = new List<GameObject>();

        public override void OnCommandAdded(IBlock parentBlock)
        {
            // Add an empty slot by default. Saves an unnecessary user click.
            if (_targetObjectsOld.Count == 0)
            {
                _targetObjectsOld.Add(null);
            }
        }

        public override string GetSummary()
        {
            if (_targetObjectsOld.Count == 0)
            {
                return "Error: No targetObjects selected";
            }
            else if (_targetObjectsOld.Count == 1)
            {
                if (_targetObjectsOld[0] == null)
                {
                    return "Error: No targetObjects selected";
                }
                return _targetObjectsOld[0].name + " = " + GetSummaryValue();
            }
            
            string namesOfGameObjects = "";
            for (int i = 0; i < _targetObjectsOld.Count; i++)
            {
                var go = _targetObjectsOld[i];
                if (go == null)
                {
                    continue;
                }
                if (namesOfGameObjects == "")
                {
                    namesOfGameObjects += go.name;
                }
                else
                {
                    namesOfGameObjects += ", " + go.name;
                }
            }
            
            return namesOfGameObjects + " = " + GetSummaryValue();
        }
        
        public override Color GetButtonColor()
        {
            return new Color32(180, 250, 250, 255);
        }

        public override bool IsReorderableArray(string propertyName)
        {
            if (propertyName == "targetObjects")
            {
                return true;
            }

            return false;
        }

        protected override void DelayedOnValidate()
        {
            base.DelayedOnValidate();
            if (this == null)
            {
                return;
            }

            if (_targetObjectsOld != null && _targetObjectsOld.Count > 0)
            {
                for (int i = 0; i < _targetObjectsOld.Count; i++)
                {
                    GameObject oldObj = _targetObjectsOld[i];
                    GameObjectData newObjData = new GameObjectData(oldObj);
                    _targetObjects.Add(newObjData);
                }
            }

            _targetObjectsOld = null;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public override bool HasReference(IVariable variable)
        {
            return ReferenceEquals(_waitUntilFinished.VarRef, variable) || 
                ReferenceEquals(_duration.VarRef, variable) || base.HasReference(variable);
        }

        #endregion
    }
}