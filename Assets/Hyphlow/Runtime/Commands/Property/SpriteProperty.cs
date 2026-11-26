using UnityEngine.Serialization;
using UnityEngine;


using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
	/// <summary>
	/// Get or Set a property of a Sprite component
	/// </summary>
	[CommandInfo("Property",
				 "Sprite",
				 "Get or Set a property of a Sprite component")]
	[AddComponentMenu("")]
	[MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
	public class SpriteProperty : BaseVariableProperty
	{
		//generated property
		public enum Property 
		{ 
			Border, 
			PixelsPerUnit, 
			Pivot, 
			Packed, 
			TextureRectOffset, 
		}

		[SerializeField]
		[FormerlySerializedAs("property")]
		protected Property _property;
		
		[SerializeField]
		[VariableProperty(typeof(SpriteVariable))]
		[FormerlySerializedAs("spriteVar")]
		protected SpriteVariable _spriteVar;

		[SerializeField]
		[VariableProperty(typeof(FloatVariable),
						  typeof(Vector2Variable),
						  typeof(BooleanVariable))]
		[FormerlySerializedAs("inOutVar")]
		protected Variable _inOutVar;

		public override void OnEnter()
		{
			var iof = _inOutVar as FloatVariable;
			var iov2 = _inOutVar as Vector2Variable;
			var iob = _inOutVar as BooleanVariable;

			var target = _spriteVar.Value;

			switch (getOrSet)
			{
				case GetSet.Get:
					switch (_property)
					{
						case Property.PixelsPerUnit:
							iof.Value = target.pixelsPerUnit;
							break;
						case Property.Pivot:
							iov2.Value = target.pivot;
							break;
						case Property.Packed:
							iob.Value = target.packed;
							break;
						case Property.TextureRectOffset:
							iov2.Value = target.textureRectOffset;
							break;
						default:
							Debug.Log("Unsupported get or set attempted");
							break;
					}

					break;
				case GetSet.Set:
					switch (_property)
					{
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
			if (_spriteVar == null)
			{
				return "Error: no spriteVar set";
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
          if (ReferenceEquals(_spriteVar, variable) || ReferenceEquals(_inOutVar, variable))
				return true;

			return false;
		}

	}
}
