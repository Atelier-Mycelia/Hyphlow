using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// AudioSource variable type.
    /// </summary>
    [VariableInfo("Audio", "AudioSource", typeof(AudioSource), false)]
    [AddComponentMenu("")]
    [System.Serializable]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class AudioSourceVariable : VariableBase<AudioSource>
    {
    }

    
}