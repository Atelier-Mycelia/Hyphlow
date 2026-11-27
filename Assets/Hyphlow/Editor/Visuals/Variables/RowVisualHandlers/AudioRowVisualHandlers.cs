using UnityEngine;
using UnityEngine.Audio;

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

    // The three below were left behind in AmanitaLegacyCollection when their Muscariables came
    // back to Hyphlow; without them, these rows fell back to the UnityObject row.

    [RowVisualHandler(menuName: "Audio",
        contentType: typeof(AudioMixer),
        typeDisplayName: "AudioMixer",
        pathToTemplate: "Editor/Uxml/VarRows/Audio/AudioMixerVariableRow")]
    public class AudioMixerRowVisualHandler : RowVisualHandler<AudioMixer>
    {
    }

    [RowVisualHandler(menuName: "Audio",
        contentType: typeof(AudioMixerGroup),
        typeDisplayName: "AudioMixerGroup",
        pathToTemplate: "Editor/Uxml/VarRows/Audio/AudioMixerGroupVariableRow")]
    public class AudioMixerGroupRowVisualHandler : RowVisualHandler<AudioMixerGroup>
    {
    }

    [RowVisualHandler(menuName: "Audio",
        contentType: typeof(AudioMixerSnapshot),
        typeDisplayName: "AudioMixerSnapshot",
        pathToTemplate: "Editor/Uxml/VarRows/Audio/AudioMixerSnapshotVariableRow")]
    public class AudioMixerSnapshotRowVisualHandler : RowVisualHandler<AudioMixerSnapshot>
    {
    }
}
