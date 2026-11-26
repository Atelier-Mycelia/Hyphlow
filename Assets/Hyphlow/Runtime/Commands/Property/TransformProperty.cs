using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace AtMycelia.Hyphlow
{
	/// <summary>
	/// Get or Set a property of a Transform component
	/// </summary>
	[CommandInfo("Property",
				 "Transform",
				 "Get or Set a property of a Transform component")]
	[AddComponentMenu("")]
	[MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
	public class TransformProperty : BaseVariableProperty
	{
		//generated property
		public enum Property 
		{ 
			ChildCount, 
			EulerAngles, 
			Forward, 
			HasChanged, 
			HierarchyCapacity, 
			HierarchyCount, 
			LocalEulerAngles, 
			LocalPosition, 
			LocalScale, 
			LossyScale, 
			Parent, 
			Position, 
			Right, 
			Root, 
			Up, 
			Rotation, 
			LocalRotation, 
			WorldToLocalMatrix, 
			LocalToWorldMatrix, 
		}

		[SerializeField]
		[FormerlySerializedAs("property")]
		protected Property _property = Property.Position;

		[SerializeField]
		[FormerlySerializedAs("transformData")]
		protected TransformData _transformData;

		[SerializeField]
		[ContentTypeConstraint(typeof(Vector3), typeof(Vector4), typeof(Transform), typeof(int), typeof(bool))]
		protected VariableReference _inOutVar;

		protected override void RefreshVariableDataCache()
		{
			base.RefreshVariableDataCache();
			_variableDataCache.Add(_transformData);
		}

		public override void OnEnter()
		{
			var iot = _inOutVar.Variable as IVariable<Transform>;
			var iob = _inOutVar.Variable as IVariable<bool>;

			var target = _transformData.Value;

			switch (getOrSet)
			{
				case GetSet.Get:
					switch (_property)
					{
						case Property.Position:
							_inOutVar.SetValue(target.position);
							break;
						case Property.LocalPosition:
							_inOutVar.SetValue(target.localPosition);
							break;
						case Property.EulerAngles:
							_inOutVar.SetValue(target.eulerAngles);
							break;
						case Property.LocalEulerAngles:
							_inOutVar.SetValue(target.localEulerAngles);
							break;
						case Property.Right:
							_inOutVar.SetValue(target.right);
							break;
						case Property.Up:
							_inOutVar.SetValue(target.up);
							break;
						case Property.Forward:
							_inOutVar.SetValue(target.forward);
							break;
						case Property.LocalScale:
							_inOutVar.SetValue(target.localScale);
							break;
						case Property.Parent:
							iot.Value = target.parent;
							break;
						case Property.Root:
							iot.Value = target.root;
							break;
						case Property.ChildCount:
							_inOutVar.SetValue(target.childCount);
							break;
						case Property.LossyScale:
							_inOutVar.SetValue(target.lossyScale);
							break;
						case Property.HasChanged:
							iob.Value = target.hasChanged;
							break;
						case Property.HierarchyCapacity:
							_inOutVar.SetValue(target.hierarchyCapacity);
							break;
						case Property.HierarchyCount:
							_inOutVar.SetValue(target.hierarchyCount);
							break;
						default:
							Debug.Log("Unsupported get or set attempted");
							break;
					}
					break;
				case GetSet.Set:
					switch (_property)
					{
						case Property.Position:
							target.position = _inOutVar.GetValue<Vector3>();
							break;
						case Property.LocalPosition:
							target.localPosition = _inOutVar.GetValue<Vector3>();
							break;
						case Property.EulerAngles:
							target.eulerAngles = _inOutVar.GetValue<Vector3>();
							break;
						case Property.LocalEulerAngles:
							target.localEulerAngles = _inOutVar.GetValue<Vector3>();
							break;
						case Property.Right:
							target.right = _inOutVar.GetValue<Vector3>();
							break;
						case Property.Up:
							target.up = _inOutVar.GetValue<Vector3>();
							break;
						case Property.Forward:
							target.forward = _inOutVar.GetValue<Vector3>();
							break;
						case Property.LocalScale:
							target.localScale = _inOutVar.GetValue<Vector3>();
							break;
						case Property.Parent:
							target.parent = iot.Value;
							break;
						case Property.HasChanged:
							target.hasChanged = iob.Value;
							break;
						case Property.HierarchyCapacity:
							target.hierarchyCapacity = _inOutVar.GetValue<int>();
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
			if (_transformData.Value == null)
			{
				return "Error: no transform set";
			}
			if (_inOutVar == null)
			{
				return "Error: no variable set to push or pull data to or from";
			}

			//We could do further checks here, eg, you have selected childcount but set a vec3variable
			string result = getOrSet.ToString() + " " + _property.ToString();
			if (_inOutVar.Variable != null)
			{
				if (getOrSet == GetSet.Get)
				{
					result += $" from {_transformData.Value.name} & put into ";
				}
				else
				{
					result += " to ";
				}
				result +=  _inOutVar.Variable.Key;
			}
			return result;
		}

		public override Color GetButtonColor()
		{
			return CommandColors.Flow;
		}

		public override bool HasReference(IVariable variable)
		{
			if (ReferenceEquals(_transformData.VarRef, variable) || 
				ReferenceEquals(_inOutVar.Variable, variable))
				return true;

			return false;
		}

		public override void ApplyBackwardsCompatibility()
		{
			base.ApplyBackwardsCompatibility();

			if (_inOutVarOld != null)
			{
				_inOutVar.Variable = _inOutVarOld;
				_inOutVarOld = null;
			}
		}

		[SerializeField]
		[HideInInspector]
		[FormerlySerializedAs("inOutVar")]
		[FormerlySerializedAs("inOutVarOld")]
		protected Variable _inOutVarOld;

	}
}
