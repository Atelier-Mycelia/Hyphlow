using UnityEngine.Serialization;
using UnityEngine;

namespace AtMycelia.Hyphlow
{
	/// <summary>
	/// Get or Set a property of a Matrix4x4 component
	/// </summary>
	[CommandInfo("Property",
				 "Matrix4x4",
				 "Get or Set a property of a Matrix4x4 component")]
	[AddComponentMenu("")]
	public class Matrix4x4Property : BaseVariableProperty
	{
		//generated property
		public enum Property 
		{ 
			M00, 
			M10, 
			M20, 
			M30, 
			M01, 
			M11, 
			M21, 
			M31, 
			M02, 
			M12, 
			M22, 
			M32, 
			M03, 
			M13, 
			M23, 
			M33, 
			Inverse, 
			Transpose, 
			Rotation, 
			LossyScale, 
			IsIdentity, 
			Determinant, 
		}


		[SerializeField]
		[FormerlySerializedAs("property")]
		protected Property _property;

		[SerializeField]
		[FormerlySerializedAs("matrix4x4Data")]
		protected Matrix4x4Data _matrix4x4Data;

		[SerializeField]
		[VariableProperty(typeof(FloatVariable),
						  typeof(QuaternionVariable),
						  typeof(Vector3Variable),
						  typeof(BooleanVariable),
						  typeof(Matrix4x4Variable))]
		[FormerlySerializedAs("inOutVar")]
		protected Variable _inOutVar;

		public override void OnEnter()
		{
			var iof = _inOutVar as FloatVariable;
			var ioq = _inOutVar as QuaternionVariable;
			var iov = _inOutVar as Vector3Variable;
			var iob = _inOutVar as BooleanVariable;
			var iom4 = _inOutVar as Matrix4x4Variable;


			var target = _matrix4x4Data.Value;

			switch (getOrSet)
			{
				case GetSet.Get:
					switch (_property)
					{
						case Property.M00:
							iof.Value = target.m00;
							break;
						case Property.M10:
							iof.Value = target.m10;
							break;
						case Property.M20:
							iof.Value = target.m20;
							break;
						case Property.M30:
							iof.Value = target.m30;
							break;
						case Property.M01:
							iof.Value = target.m01;
							break;
						case Property.M11:
							iof.Value = target.m11;
							break;
						case Property.M21:
							iof.Value = target.m21;
							break;
						case Property.M31:
							iof.Value = target.m31;
							break;
						case Property.M02:
							iof.Value = target.m02;
							break;
						case Property.M12:
							iof.Value = target.m12;
							break;
						case Property.M22:
							iof.Value = target.m22;
							break;
						case Property.M32:
							iof.Value = target.m32;
							break;
						case Property.M03:
							iof.Value = target.m03;
							break;
						case Property.M13:
							iof.Value = target.m13;
							break;
						case Property.M23:
							iof.Value = target.m23;
							break;
						case Property.M33:
							iof.Value = target.m33;
							break;
						case Property.Rotation:
							ioq.Value = target.rotation;
							break;
						case Property.LossyScale:
							iov.Value = target.lossyScale;
							break;
						case Property.IsIdentity:
							iob.Value = target.isIdentity;
							break;
						case Property.Determinant:
							iof.Value = target.determinant;
							break;
						case Property.Inverse:
							iom4.Value = target.inverse;
							break;
						case Property.Transpose:
							iom4.Value = target.transpose;
							break;
						default:
							Debug.Log("Unsupported get or set attempted");
							break;
					}

					break;

				case GetSet.Set:
					switch (_property)
					{
						case Property.M00:
							target.m00 = iof.Value;
							break;
						case Property.M10:
							target.m10 = iof.Value;
							break;
						case Property.M20:
							target.m20 = iof.Value;
							break;
						case Property.M30:
							target.m30 = iof.Value;
							break;
						case Property.M01:
							target.m01 = iof.Value;
							break;
						case Property.M11:
							target.m11 = iof.Value;
							break;
						case Property.M21:
							target.m21 = iof.Value;
							break;
						case Property.M31:
							target.m31 = iof.Value;
							break;
						case Property.M02:
							target.m02 = iof.Value;
							break;
						case Property.M12:
							target.m12 = iof.Value;
							break;
						case Property.M22:
							target.m22 = iof.Value;
							break;
						case Property.M32:
							target.m32 = iof.Value;
							break;
						case Property.M03:
							target.m03 = iof.Value;
							break;
						case Property.M13:
							target.m13 = iof.Value;
							break;
						case Property.M23:
							target.m23 = iof.Value;
							break;
						case Property.M33:
							target.m33 = iof.Value;
							break;
						default:
							Debug.Log("Unsupported get or set attempted");
							break;
					}

					break;

				default:
					break;
			}

			_matrix4x4Data.Value = target;

			Continue();
		}

		public override string GetSummary()
		{
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
			if (ReferenceEquals(_matrix4x4Data.VarRef, variable) || ReferenceEquals(_inOutVar, variable))
				return true;

			return false;
		}
	}
}
