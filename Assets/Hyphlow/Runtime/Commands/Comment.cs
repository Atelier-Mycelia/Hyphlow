using UnityEngine;

using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Use comments to record design notes and reminders about your game.
    /// </summary>
    [CommandInfo("", 
                 "Comment", 
                 "Use comments to record design notes and reminders about your game.")]
    [AddComponentMenu("")]
    [MovedFrom(true, "AtMycelia.Hyphlow", "AtMycelia.Amanita.Core")]
    public class Comment : Command
    {   
        [Tooltip("Name of Commenter")]
        [FormerlySerializedAs("commenterName")]
        [SerializeField] protected string _commenterName = "";

        [Tooltip("Text to display for this comment")]
        [FormerlySerializedAs("commentText")]
        [TextArea(2,4)]
        [SerializeField] protected string _commentText = "";

        public override bool SkipExecution => true;

        #region Public members

        public override void OnEnter()
        {
            Continue();
        }

        public override string GetSummary()
        {
            if (_commenterName != "")
            {
                return _commenterName + ": " + _commentText;
            }
            return _commentText;
        }

        public override Color GetButtonColor()
        {
            return new Color32(220, 220, 220, 255);
        }

        #endregion
    }
}
