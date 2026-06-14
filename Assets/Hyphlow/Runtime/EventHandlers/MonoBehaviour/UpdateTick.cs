using UnityEngine.Serialization;
using UnityEngine;

using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// The block will execute every chosen Update, or FixedUpdate or LateUpdate.
    /// </summary>
    [EventHandlerInfo("MonoBehaviour",
                      "Update",
                      "The block will execute every chosen Update, or FixedUpdate or LateUpdate.")]
    [AddComponentMenu("")]
    [MovedFrom("AtMycelia.Mycorrhiza.EventHandlers")]
    public class UpdateTick : EventHandler
    {
        [System.Flags]
        public enum UpdateMessageFlags
        {
            Update = 1 << 0,
            FixedUpdate = 1 << 1,
            LateUpdate = 1 << 2,
        }

        [Tooltip("Which of the Update messages to trigger on.")]
        [SerializeField]
        [EnumFlag]
        [FormerlySerializedAs("FireOn")]
        protected UpdateMessageFlags _fireOn = UpdateMessageFlags.Update;

        private void Update()
        {
            if (ShouldFireOn(UpdateMessageFlags.Update))
            {
                ExecuteBlock();
            }
        }

        private bool ShouldFireOn(UpdateMessageFlags flags)
        {
            bool result = (_fireOn & flags) != 0;
            return result;
        }

        private void FixedUpdate()
        {
            if (ShouldFireOn(UpdateMessageFlags.FixedUpdate))
            {
                ExecuteBlock();
            }
        }

        private void LateUpdate()
        {
            if (ShouldFireOn(UpdateMessageFlags.LateUpdate))
            {
                ExecuteBlock();
            }
        }
    }
}