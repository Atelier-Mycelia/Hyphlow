using UnityEngine;
using UnityEngine.Serialization;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Stop velocity and angular velocity on a Rigidbody2D
    /// </summary>
    [CommandInfo("Physics/Rigidbody2D",
                 "StopMotion2D",
                 "Stop velocity and angular velocity on a Rigidbody2D")]
    [AddComponentMenu("")]
    public class StopMotionRigidBody2D : Command
    {
        [SerializeField]
        [FormerlySerializedAs("rb")]
        protected RigidbodyTwoDData _rb;

        public enum Motion
        {
            Velocity,
            AngularVelocity,
            AngularAndLinearVelocity
        }

        [SerializeField]
        [FormerlySerializedAs("motionToStop")]
        protected Motion _motionToStop = Motion.AngularAndLinearVelocity;

        public override void OnEnter()
        {
            switch (_motionToStop)
            {
                case Motion.Velocity:
                    Velocity = Vector2.zero;
                    break;
                case Motion.AngularVelocity:
                    AngularVelocity = 0;
                    break;
                case Motion.AngularAndLinearVelocity:
                    AngularVelocity = 0;
                    Velocity = Vector2.zero;
                    break;
                default:
                    break;
            }

            Continue();
        }

        private Vector2 Velocity
        {
            get
            {
                #if UNITY_6000
                    return _rb.Value.linearVelocity;
                #else
                    return _rb.Value.velocity;
                #endif
            }
            set
            {
                #if UNITY_6000
                    _rb.Value.linearVelocity = value;
                #else
                    _rb.Value.velocity = value;
                #endif
            }
        }

        private float AngularVelocity
        {
            get => _rb.Value.angularVelocity;
            set => _rb.Value.angularVelocity = value;
        }

        public override string GetSummary()
        {
            string result;
            bool weHaveRb = _rb.rigidbody2DRef != null;
            if (!weHaveRb)
            {
                result = "Error: No Rigidbody2D referenced";
            }
            else
            {
                result = $"{_motionToStop} on {GetRbTwoDSummary()}";
            }
            return _motionToStop.ToString();
        }

        private string GetRbTwoDSummary()
        {
            string result;
            if (_rb.rigidbody2DRef == null)
            {
                result = "No Rigidbody2D referenced";
            }
            else
            {
                if (_rb.RepresentingVar)
                {
                    result = $"{_rb.VarRef.Key}";
                }
                else
                {
                    result = $"{_rb.Value.name}"; 
                }
            }

            return result;
        }

        public override Color GetButtonColor()
        {
            return CommandColors.Physics;
        }

        public override bool HasReference(Variable variable)
        {
            if (_rb.rigidbody2DRef == variable)
                return true;

            return false;
        }

    }
}