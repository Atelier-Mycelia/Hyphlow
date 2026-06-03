using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using System;
using UnityEngine.Scripting.APIUpdating;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Supported modes for calling a block.
    /// </summary>
    public enum CallMode
    {
        /// <summary> Stop executing the current block after calling. </summary>
        Stop,
        /// <summary> Continue executing the current block after calling  </summary>
        Continue,
        /// <summary> Wait until the called block finishes executing, then continue executing current block. </summary>
        WaitUntilFinished,
        /// <summary> Stop executing the current block before attempting to call. This allows for circular calls within the same frame </summary>
        StopThenCall,

        /// <summary>
        /// Mainly for debug. In production, functions the same as Stop.
        /// </summary>
        Null,
    }

    /// <summary>
    /// Execute another block in the same Flowchart as the command, or in a different Flowchart.
    /// </summary>
    [CommandInfo("Flow",
                 "Call",
                 "Execute another block in the same Flowchart as the command, or in a different Flowchart.")]
    [AddComponentMenu("")]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class Call : Command, IBlockCaller, ISerializationCallbackReceiver
    {
        [Tooltip("Block to start executing.")]
        [SerializeField] protected BlockReference _targetBlockReference = new BlockReference();

        // Legacy serialized fields retained for backwards compatibility migration.
        [Tooltip("Flowchart which contains the block to execute. If none is specified then the current Flowchart is used.")]
        [FormerlySerializedAs("targetFlowchart")]
        [SerializeField] [HideInInspector] protected Flowchart _targetFlowchart;

        [FormerlySerializedAs("targetSequence")]
        [Tooltip("Block to start executing")]
        [FormerlySerializedAs("targetBlock")]
        [SerializeField] [HideInInspector] protected Block _targetBlock;

        [Tooltip("Label to start execution at. Takes priority over startIndex.")]
        [FormerlySerializedAs("startLabel")]
        [SerializeField] protected StringData _startLabel = new StringData();

        [Tooltip("Command index to start executing")]
        [FormerlySerializedAs("startIndex")]
        [SerializeField] protected IntegerData _startIndex = new IntegerData(0);
    
        [Tooltip("Select if the calling block should stop or continue executing commands, " +
            "or wait until the called block finishes.")]
        [FormerlySerializedAs("callMode")]
        [SerializeField] protected CallMode _callMode = CallMode.WaitUntilFinished;

        protected override void Awake()
        {
            base.Awake();
        }
        protected override void RefreshVariableDataCache()
        {
            base.RefreshVariableDataCache();
            _variableDataCache.Add(_startLabel);
            _variableDataCache.Add(_startIndex);
        }

        private void EnsureTargetBlockReference()
        {
            _targetBlockReference ??= new BlockReference();
        }

        /// <summary>
        /// Migrates legacy _targetFlowchart + _targetBlock into _targetBlockReference.
        /// If _targetFlowchart is null and _targetBlock is not null, the owning Flowchart
        /// is assumed to be this command's Flowchart as the intended target owner.
        /// </summary>
        private bool MigrateLegacyFieldsToBlockRef()
        {
            EnsureTargetBlockReference();

            bool hasLegacyData = _targetBlock != null || _targetFlowchart != null;
            if (!hasLegacyData)
            {
                return false; // Important: do not overwrite existing block reference data.
            }

            bool migrated = false;

            if (_targetBlock != null)
            {
                _targetBlockReference.Block = _targetBlock;

                Flowchart resolvedOwner = _targetFlowchart ??
                    _targetBlock.ParentFlowchart ??
                    _targetBlock.GetFlowchart();

                if (resolvedOwner != null)
                {
                    _targetBlockReference.BlockOwner = resolvedOwner;
                }

                migrated = _targetBlockReference.ItemId != Block.InvalidId &&
                    _targetBlockReference.BlockOwner != null;
            }
            else if (_targetFlowchart != null)
            {
                _targetBlockReference.Block = null;
                _targetBlockReference.BlockOwner = _targetFlowchart;
                migrated = true;
            }

            if (!migrated)
            {
                return false; // Keep legacy fields so we can retry later.
            }

            _targetBlock = null;
            _targetFlowchart = null;

#if UNITY_EDITOR
            EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    EditorUtility.SetDirty(this);
                }
            };
#endif

            return true;
        }

        public override void OnEnter()
        {
            if (_callMode == CallMode.Null)
            {
                string errorMessage = "CallMode is set to Null. This is not a valid mode for production. " +
                    "Skipping Call Command execution.";
                Debug.LogWarning(errorMessage, this);
                Continue();
                return;
            }

            IBlock targetBlock = _targetBlockReference.Block;
            if (targetBlock != null)
            {
                // Check if calling your own parent block
                bool callingOwnParent = ParentBlock != null && targetBlock.Equals(ParentBlock);
                if (callingOwnParent)
                {
                    // Just ignore the callmode in this case, and jump to first command in list
                    Continue(0);
                    return;
                }

                if (targetBlock.IsExecuting)
                {
                    if (_callMode == CallMode.StopThenCall)
                    {
                        targetBlock.Stop();
                    }
                    else
                    {
                        string logMessage = $"{targetBlock.BlockName} is already running.";
                        Debug.LogWarning(logMessage, this);
                        Continue();
                        return;
                    }
                }

                Action onComplete = null;
                if (_callMode == CallMode.WaitUntilFinished)
                {
                    onComplete = delegate
                    {
                        Continue();
                    };
                }

                // Find the command index to start execution at
                int index = _startIndex;
                if (_startLabel.Value != "")
                {
                    int labelIndex = targetBlock.GetLabelIndex(_startLabel.Value);
                    if (labelIndex != -1)
                    {
                        index = labelIndex;
                    }
                }

                Flowchart targetFlowchart = _targetBlockReference.BlockOwner as Flowchart;
                if (targetFlowchart != null)
                {
                    if (_callMode == CallMode.StopThenCall)
                    {
                        OnExit();
                        ParentBlock.Stop();
                    }

                    targetFlowchart.ExecuteBlock(targetBlock, index, onComplete);
                }
            }

            if (_callMode == CallMode.Stop)
            {
                OnExit();
                ParentBlock.Stop();
            }
            else if (_callMode == CallMode.Continue)
            {
                Continue();
            }
        }

        public override void GetConnectedBlocks(ref IList<IBlock> connectedBlocks)
        {
            IBlock targetBlock = _targetBlockReference.Block;
            if (targetBlock != null)
            {
                connectedBlocks.Add(targetBlock);
            }       
        }
        
        public override string GetSummary()
        {
            IBlock targetBlock = _targetBlockReference.Block;
            string summary = GetSummaryFor(targetBlock);

            summary += " : " + _callMode.ToString();

            return summary;
        }

        private string GetSummaryFor(IBlock block)
        {
            string result;
            if (block == null)
            {
                result = "<None>";
            }
            else
            {
                string blockName = block.BlockName;
                if (blockName.Length > 18)
                {
                    blockName = blockName.Substring(0, 15) + "...";
                }
                string flowchartName = block.ParentFlowchart != null ? 
                    block.ParentFlowchart.name : 
                    "No Flowchart";
                bool belongsToAnotherFlowchart = block.ParentFlowchart != null &&
                    block.ParentFlowchart != this.ParentBlock.ParentFlowchart;
                if (!belongsToAnotherFlowchart)
                {
                    flowchartName = "this";
                }
                
                if (flowchartName.Length > 18)
                {
                    flowchartName = flowchartName.Substring(0, 15) + "...";
                }

                result = $"{flowchartName}.{blockName}";
            }
            return result;
        }

        public override Color GetButtonColor()
        {
            return CommandColors.Flow;
        }

        public override bool HasReference(IVariable variable)
        {
            return ReferenceEquals(_startLabel.VarRef, variable) ||
                ReferenceEquals(_startIndex.VarRef, variable) ||
                base.HasReference(variable);
        }

        public bool MayCallBlock(IBlock block)
        {
            return ReferenceEquals(block, _targetBlockReference.Block);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            //MigrateLegacyFieldsToBlockRef();
        }

        public override void ApplyBackwardsCompatibility()
        {
            base.ApplyBackwardsCompatibility();
            if (_oldStartIndex >= 0)
            {
                _startIndex.LiteralValue = _oldStartIndex;
                _oldStartIndex = -1;
            }

            MigrateLegacyFieldsToBlockRef();
        }

        [FormerlySerializedAs("startIndex")]
        [SerializeField] protected int _oldStartIndex;

        protected override void DelayedOnValidate()
        {
            base.DelayedOnValidate();

            if (_callMode == CallMode.Null)
            {
                _callMode = CallMode.Stop;
            }

            MigrateLegacyFieldsToBlockRef();
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            EnsureTargetBlockReference();

#if UNITY_EDITOR
            EditorApplication.delayCall += () =>
            {
                if (this == null)
                {
                    return;
                }

                MigrateLegacyFieldsToBlockRef();
            };
#else
            MigrateLegacyFieldsToBlockRef();
#endif
        }
    }
}