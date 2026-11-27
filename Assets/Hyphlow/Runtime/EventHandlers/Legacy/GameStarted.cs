using UnityEngine.Serialization;
using UnityEngine;
using System.Collections;

using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow.Legacy
{
    /// <summary>
    /// The block will execute when the game starts playing.
    /// Hidden from the menu now that <see cref="Hyphlow.GameStarted"/> replaces it; Blocks that
    /// already use it keep working.
    /// </summary>
    [EventHandlerInfo("",
                      "Game Started",
                      "The block will execute when the game starts playing.",
                      showInMenu: false)]
    [AddComponentMenu("")]
    [MovedFrom("AtMycelia.Hyphlow")]
    public class GameStarted : EventHandler, IGameStartedHandler
    {
        [Tooltip("Wait for a number of frames after startup before executing the Block. Can help fix startup order issues.")]
        [SerializeField] [FormerlySerializedAs("waitForFrames")]
        protected int waitForFrames = 1;

        public virtual void Trigger()
        {
            StartCoroutine(GameStartCoroutine());
        }

        protected virtual IEnumerator GameStartCoroutine()
        {
            int frameCount = waitForFrames;
            while (frameCount > 0)
            {
                yield return new WaitForEndOfFrame();
                frameCount--;
            }

            ExecuteBlock();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (waitForFrames <= 0)
            {
                waitForFrames = 1;
            }
        }
    }
}
