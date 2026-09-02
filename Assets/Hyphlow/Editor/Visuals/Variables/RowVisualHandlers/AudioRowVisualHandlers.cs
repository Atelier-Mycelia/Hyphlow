using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    [RowVisualHandler(menuName: "Audio",
        contentType: typeof(AudioClip),
        typeDisplayName: "AudioClip",
        pathToTemplate: "Editor/Uxml/VarRows/Audio/AudioClipVariableRow")]
    public class AudioClipRowVisualHandler : RowVisualHandler<AudioClip>
    {
    }

    [RowVisualHandler(menuName: "Audio",
        contentType: typeof(AudioSource),
        typeDisplayName: "AudioSource",
        pathToTemplate: "Editor/Uxml/VarRows/Audio/AudioSourceVariableRow")]
    public class AudioSourceRowVisualHandler : RowVisualHandler<AudioSource>
    {
    }

}