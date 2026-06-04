using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// This holds configuration settings for how an individual Flowchart 
    /// should handle QOL for the editor. For example, how long to wait
    /// between executing each Command, or whether to show line numbers 
    /// in the Block inspector. Both editor and runtime logic may want 
    /// to know this, hence it being in the Runtime assembly.
    /// </summary>
    [CreateAssetMenu(fileName = "FlowchartEditorQOL",
        menuName = "Atelier Mycelia/Hyphlow/FlowchartEditorQOL", 
        order = 1)]
    public class FlowchartEditorQol : ScriptableObject
    {
        [Range(0f, 5f)]
        [Tooltip("Adds a pause after each execution step to make it easier to visualise " +
            "program flow. Editor only, has no effect in platform builds.")]
        [FormerlySerializedAs("stepPause")]
        [SerializeField] protected float _stepPause = 0f;

        [Tooltip("Saves the selected block and commands when saving the scene. Helps " +
            "avoid version control conflicts if you've only changed the active selection.")]
        [FormerlySerializedAs("saveSelection")]
        [SerializeField] protected bool _saveSelection = true;

        [Tooltip("Display line numbers in the command list in the Block inspector.")]
        [FormerlySerializedAs("showLineNumbers")]
        [SerializeField] protected bool _showLineNumbers = false;

        [Tooltip("List of commands to hide in the Add Command menu. Use this to restrict " +
            "the set of commands available when editing a Flowchart.")]
        [FormerlySerializedAs("hideCommands")]
        [SerializeField] protected List<string> _commandsToHide = new List<string>();

        public virtual float StepPause
        {
            get => _stepPause;
        }

        public virtual bool SaveSelection
        {
            get => _saveSelection;
        }

        public virtual bool ShowLineNumbers
        {
            get => _showLineNumbers;
        }

        public virtual IReadOnlyList<string> CommandsToHide
        {
            get => _commandsToHide;
        }
    }
}