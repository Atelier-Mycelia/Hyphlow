using System;
using System.Collections;
using UnityEngine;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// The block will execute when the game starts playing. Hyphandler successor to
    /// <see cref="Legacy.GameStarted"/>.
    /// </summary>
    [EventHandlerInfo("",
                      "Game Started",
                      "The block will execute when the game starts playing.")]
    [Serializable]
    public class GameStarted : Hyphandler, IGameStartedHandler
    {
        [Tooltip("Wait for a number of frames after startup before executing the Block. Can help fix startup order issues.")]
        [SerializeField] protected int _waitForFrames = 1;

        /// <summary>
        /// Called by the Flowchart's BlockExecutionManagerComponent when it starts. Does nothing
        /// while the handler isn't subscribed (say, because its Block is disabled), the way a
        /// disabled Block shouldn't react to anything else either.
        /// </summary>
        public virtual void Trigger()
        {
            if (!IsEnabled)
            {
                return;
            }

            StartCoroutine(GameStartCoroutine());
        }

        protected virtual IEnumerator GameStartCoroutine()
        {
            int frameCount = _waitForFrames;
            while (frameCount > 0)
            {
                yield return new WaitForEndOfFrame();
                frameCount--;
            }

            ExecuteBlock();
        }

        public override void Validate()
        {
            base.Validate();
            if (_waitForFrames <= 0)
            {
                _waitForFrames = 1;
            }
        }

        public override string GetSummary()
        {
            return _waitForFrames > 1 ?
                $"After {_waitForFrames} frames" :
                "";
        }
    }
}
