using UnityEngine;
using UnityEngine.Scripting.APIUpdating;


namespace AtMycelia.Hyphlow
{
	/// <summary>
	/// AudioClip variable type.
	/// </summary>
	[VariableInfo("Audio", "AudioClip", typeof(AudioClip), false)]
	[AddComponentMenu("")]
	[System.Serializable]
	[MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
	public class AudioClipVariable : VariableBase<UnityEngine.AudioClip>
	{ }

	
}
