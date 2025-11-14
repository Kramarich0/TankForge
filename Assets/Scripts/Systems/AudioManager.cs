using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioMixer masterMixer;
    private AudioMixerGroup masterGroup;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (masterMixer != null)
        {
            var groups = masterMixer.FindMatchingGroups("Master");
            if (groups.Length > 0) masterGroup = groups[0];
        }
    }

    public static void AssignToMaster(AudioSource source)
    {
        if (Instance.masterGroup != null)
            source.outputAudioMixerGroup = Instance.masterGroup;
    }
}