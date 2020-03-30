using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace FiveKnights
{
    public static class AudioExtensions
    {


        public static void Play(this AudioSource audio, string clipName, float pitchMin = 1.0f, float pitchMax = 1.0f, float time = 0.0f)
        {
            AudioClip GetAudioClip()
            {
                switch (clipName)
                {
                    case "Dung Pillar":
                        return (AudioClip) FiveKnights.preloadedGO["WD"].LocateMyFSM("Dung Defender").GetAction<AudioPlayerOneShotSingle>("Pillar", 0).audioClip.Value;
                    case "Mace Slam":
                        return (AudioClip) FiveKnights.preloadedGO["fk"].LocateMyFSM("FalseyControl").GetAction<AudioPlayerOneShotSingle>("Slam", 1).audioClip.Value;
                    case "Mace Swing":
                        return (AudioClip) FiveKnights.preloadedGO["fk"].LocateMyFSM("FalseyControl").GetAction<AudioPlayerOneShotSingle>("S Attack", 0).audioClip.Value;
                    default:
                        return null;
                }   
            }
            
            audio.pitch = Random.Range(pitchMin, pitchMax);
            audio.time = time; 
            audio.PlayOneShot(GetAudioClip());
        }
    }
}