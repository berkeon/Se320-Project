using UnityEngine;

// Plays a short sound effect once. A separate temporary object is used so the sound keeps
// playing even if the object that triggered it (a projectile, for example) is destroyed right away.
// Spatial blend 0 keeps the volume the same wherever the camera is.
public static class SoundEffects
{
    public static void Play(AudioClip clip, float volume, float pitchVariation = 0.05f)
    {
        if (clip == null) return;

        GameObject soundObject = new GameObject("SoundEffect_" + clip.name);
        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.spatialBlend = 0f;
        source.Play();
        Object.Destroy(soundObject, clip.length / source.pitch + 0.1f);
    }
}
