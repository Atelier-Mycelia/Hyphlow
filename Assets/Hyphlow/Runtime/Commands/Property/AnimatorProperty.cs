using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace AtMycelia.Hyphlow
{
	/// <summary>
	/// Get or Set a property of a Animator component
	/// </summary>
	[CommandInfo("Property", 
				 "Animator",
				 "Get or Set a property of a Animator component")]
	[AddComponentMenu("")]
	[MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
	public class AnimatorProperty : BaseVariableProperty
	{
		//generated property
		public enum Property 
		{ 
			IsOptimizable, 
			IsHuman, 
			HasRootMotion, 
			HumanScale, 
			IsInitialized, 
			DeltaPosition, 
			DeltaRotation, 
			Velocity, 
			AngularVelocity, 
			RootPosition, 
			RootRotation, 
			ApplyRootMotion, 
			HasTransformHierarchy, 
			GravityWeight, 
			BodyPosition, 
			BodyRotation, 
			StabilizeFeet, 
			LayerCount, 
			ParameterCount, 
			FeetPivotActive, 
			PivotWeight, 
			PivotPosition, 
			IsMatchingTarget, 
			Speed, 
			TargetPosition, 
			TargetRotation, 
			PlaybackTime, 
			RecorderStartTime, 
			RecorderStopTime, 
			HasBoundPlayables, 
			LayersAffectMassCenter, 
			LeftFeetBottomHeight, 
			RightFeetBottomHeight, 
			LogWarnings, 
			FireEvents, 
			KeepAnimatorControllerStateOnDisable, 
			KeepAnimatorStateOnDisable, 
			WriteDefaultValuesOnDisable
		}

		
		[SerializeField]
		[FormerlySerializedAs("property")]
		protected Property _property;

		[SerializeField]
		[ContentTypeConstraint(typeof(Animator))]
		protected VariableReference _animatorVar;

		[SerializeField]
		[ContentTypeConstraint(typeof(bool), typeof(float), typeof(Vector3), typeof(int))]
		protected VariableReference _inOutVar;

		public override void OnEnter()
		{
			var inOutBool = _inOutVar.Variable as IVariable<bool>;
			var inOutFloat = _inOutVar.Variable as IVariable<float>;
			var inOutVecThree = _inOutVar.Variable as IVariable<Vector3>;
			var inOutInt = _inOutVar.Variable as IVariable<int>;

			var target = _animatorVar.GetValue<Animator>();

			switch (getOrSet)
			{
				case GetSet.Get:
					switch (_property)
					{
						case Property.IsOptimizable:
							inOutBool.Value = target.isOptimizable;
							break;
						case Property.IsHuman:
							inOutBool.Value = target.isHuman;
							break;
						case Property.HasRootMotion:
							inOutBool.Value = target.hasRootMotion;
							break;
						case Property.HumanScale:
							inOutFloat.Value = target.humanScale;
							break;
						case Property.IsInitialized:
							inOutBool.Value = target.isInitialized;
							break;
						case Property.DeltaPosition:
							inOutVecThree.Value = target.deltaPosition;
							break;
						case Property.Velocity:
							inOutVecThree.Value = target.velocity;
							break;
						case Property.AngularVelocity:
							inOutVecThree.Value = target.angularVelocity;
							break;
						case Property.RootPosition:
							inOutVecThree.Value = target.rootPosition;
							break;
						case Property.ApplyRootMotion:
							inOutBool.Value = target.applyRootMotion;
							break;
						case Property.HasTransformHierarchy:
							inOutBool.Value = target.hasTransformHierarchy;
							break;
						case Property.GravityWeight:
							inOutFloat.Value = target.gravityWeight;
							break;
						case Property.BodyPosition:
							inOutVecThree.Value = target.bodyPosition;
							break;
						case Property.StabilizeFeet:
							inOutBool.Value = target.stabilizeFeet;
							break;
						case Property.LayerCount:
							inOutInt.Value = target.layerCount;
							break;
						case Property.ParameterCount:
							inOutInt.Value = target.parameterCount;
							break;
						case Property.FeetPivotActive:
							inOutFloat.Value = target.feetPivotActive;
							break;
						case Property.PivotWeight:
							inOutFloat.Value = target.pivotWeight;
							break;
						case Property.PivotPosition:
							inOutVecThree.Value = target.pivotPosition;
							break;
						case Property.IsMatchingTarget:
							inOutBool.Value = target.isMatchingTarget;
							break;
						case Property.Speed:
							inOutFloat.Value = target.speed;
							break;
						case Property.TargetPosition:
							inOutVecThree.Value = target.targetPosition;
							break;
						case Property.PlaybackTime:
							inOutFloat.Value = target.playbackTime;
							break;
						case Property.RecorderStartTime:
							inOutFloat.Value = target.recorderStartTime;
							break;
						case Property.RecorderStopTime:
							inOutFloat.Value = target.recorderStopTime;
							break;
						case Property.HasBoundPlayables:
							inOutBool.Value = target.hasBoundPlayables;
							break;
						case Property.LayersAffectMassCenter:
							inOutBool.Value = target.layersAffectMassCenter;
							break;
						case Property.LeftFeetBottomHeight:
							inOutFloat.Value = target.leftFeetBottomHeight;
							break;
						case Property.RightFeetBottomHeight:
							inOutFloat.Value = target.rightFeetBottomHeight;
							break;
						case Property.LogWarnings:
							inOutBool.Value = target.logWarnings;
							break;
						case Property.FireEvents:
							inOutBool.Value = target.fireEvents;
							break;
						case Property.KeepAnimatorStateOnDisable:
							inOutBool.Value = target.keepAnimatorStateOnDisable;
							break;
						case Property.WriteDefaultValuesOnDisable:
							inOutBool.Value = target.writeDefaultValuesOnDisable;
							break;
						default:
							Debug.Log("Unsupported get or set attempted");
							break;
					}

					break;
				case GetSet.Set:
					switch (_property)
					{
						case Property.RootPosition:
							target.rootPosition = inOutVecThree.Value;
							break;
						case Property.ApplyRootMotion:
							target.applyRootMotion = inOutBool.Value;
							break;
						case Property.BodyPosition:
							target.bodyPosition = inOutVecThree.Value;
							break;
						case Property.StabilizeFeet:
							target.stabilizeFeet = inOutBool.Value;
							break;
						case Property.FeetPivotActive:
							target.feetPivotActive = inOutFloat.Value;
							break;
						case Property.Speed:
							target.speed = inOutFloat.Value;
							break;
						case Property.PlaybackTime:
							target.playbackTime = inOutFloat.Value;
							break;
						case Property.RecorderStartTime:
							target.recorderStartTime = inOutFloat.Value;
							break;
						case Property.RecorderStopTime:
							target.recorderStopTime = inOutFloat.Value;
							break;
						case Property.LayersAffectMassCenter:
							target.layersAffectMassCenter = inOutBool.Value;
							break;
						case Property.LogWarnings:
							target.logWarnings = inOutBool.Value;
							break;
						case Property.FireEvents:
							target.fireEvents = inOutBool.Value;
							break;
						case Property.KeepAnimatorStateOnDisable:
							target.keepAnimatorStateOnDisable = inOutBool.Value;
							break;
						case Property.WriteDefaultValuesOnDisable:
							target.writeDefaultValuesOnDisable = inOutBool.Value;
							break;
						default:
							Debug.Log("Unsupported get or set attempted");
							break;
					}

					break;
				default:
					break;
			}

			Continue();
		}

		public override string GetSummary()
		{
			if (_animatorVar == null)
			{
				return "Error: no animatorVar set";
			}
			if (_inOutVar == null)
			{
				return "Error: no variable set to push or pull data to or from";
			}

			return getOrSet.ToString() + " " + _property.ToString();
		}

		public override Color GetButtonColor()
		{
			return CommandColors.Flow;
		}

		public override bool HasReference(IVariable variable)
		{
			if (ReferenceEquals(_animatorVar.Variable, variable) || 
				ReferenceEquals(_inOutVar.Variable, variable))
				return true;

			return false;
		}

		public override void ApplyBackwardsCompatibility()
		{
			base.ApplyBackwardsCompatibility();

			if (_oldInOutVar != null)
			{
				_inOutVar.Variable = _oldInOutVar;
				_oldInOutVar = null;
			}

			if (_oldAnimatorVar != null)
			{
				_animatorVar.Variable = _oldAnimatorVar;
				_oldAnimatorVar = null;
			}
		}

		[SerializeField]
		[HideInInspector]
		[VariableProperty(typeof(AnimatorVariable))]
		[FormerlySerializedAs("animatorVar")]
		protected AnimatorVariable _oldAnimatorVar;

		[SerializeField]
		[HideInInspector]
		[VariableProperty(typeof(BooleanVariable),
						  typeof(FloatVariable),
						  typeof(Vector3Variable),
						  typeof(IntegerVariable))]
		[FormerlySerializedAs("inOutVar")]
		protected Variable _oldInOutVar;
	}
}
