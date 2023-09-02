using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager i;
    
    public float masterVolume = 1;
    public int poolSize = 20;

    public float randPitchAmplitude = 0.1f;

    private AudioSource[] audioSources;

    public AudioClip[] clips;

    private void Awake()
    {
        i = this;

        audioSources = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            audioSources[i] = new GameObject("Audio Source").AddComponent<AudioSource>();
        }
    }

    public static AudioSource PlaySound(AudioClip clip, float volume = 1, float pitch = 1, bool loop = false)
    {
        return i.PlaySoundInstance(clip, volume, pitch, loop);
    }

    public static AudioSource PlaySound(string clipName, float volume = 1, float pitch = 1, bool loop = false)
    {
        foreach (AudioClip clip in i.clips) // OPTI: use a map or something
        {
            if (clip.name == clipName)
            {
                return i.PlaySoundInstance(clip, volume, pitch, loop);
            }
        }

        Debug.LogError("No clip found with name " + clipName);
        return null;
    }

    public AudioSource PlaySoundInstance(AudioClip clip, float volume = 1, float pitch = 1, bool loop = false)
    {
        int pos = 0;
        for (pos = 0; pos < poolSize; pos++) // Loop through all sources
        {
            if (!audioSources[pos].isPlaying) // Find one that isn't playing
                break;
        }

        if (pos == poolSize) // All already playing
        {
            pos = 0;
            Debug.LogError("Audio sources pool size exceeded");
        }

        AudioSource source = audioSources[pos];

        source.spatialBlend = 0;
        source.clip = clip;
        source.volume = masterVolume * volume;
        source.pitch = pitch;
        source.loop = loop;
        source.Play();

        return source;
    }

    public static float RandPitch()
    {
        return UnityEngine.Random.Range(1 - i.randPitchAmplitude * 0.5f, 1 + i.randPitchAmplitude * 0.5f);
    }

    public static void FadeAndStop(AudioSource source, float duration)
    {
        LeanTween.value(source.volume, 0, duration).setOnUpdate(t => {
            source.volume = t;
        });
    }
}
