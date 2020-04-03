using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using UnityEngine;

namespace FiveKnights
{
    public static class AudioSourceExtensions
    {
        public static void Play(this AudioSource audio, string clipName, float pitchMin = 1.0f, float pitchMax = 1.0f, float time = 0.0f)
        {
            AudioClip GetAudioClip()
            {
                switch (clipName)
                {
                    case "Counter":
                        return (AudioClip) FiveKnights.preloadedGO["PV"].LocateMyFSM("Control").GetAction<AudioPlayerOneShotSingle>("Counter Stance", 1).audioClip.Value;
                    case "Dash":
                        return (AudioClip) FiveKnights.preloadedGO["PV"].LocateMyFSM("Control").GetAction<AudioPlayerOneShotSingle>("Dash", 1).audioClip.Value;
                    case "Dive":
                        return (AudioClip) FiveKnights.preloadedGO["Kin"].LocateMyFSM("IK Control").GetAction<AudioPlaySimple>("Dstab Fall", 0).oneShotClip.Value;
                    case "Dung Pillar":
                        return (AudioClip) FiveKnights.preloadedGO["WD"].LocateMyFSM("Dung Defender").GetAction<AudioPlayerOneShotSingle>("Pillar", 0).audioClip.Value;
                    case "Heavy Land":
                        return (AudioClip) FiveKnights.preloadedGO["Kin"].LocateMyFSM("IK Control").GetAction<AudioPlaySimple>("Dstab Land", 0).oneShotClip.Value;
                    case "Light Land":
                        return (AudioClip) FiveKnights.preloadedGO["Kin"].LocateMyFSM("IK Control").GetAction<AudioPlaySimple>("Land", 0).oneShotClip.Value;
                    case "Mace Slam":
                        return (AudioClip) FiveKnights.preloadedGO["fk"].LocateMyFSM("FalseyControl").GetAction<AudioPlayerOneShotSingle>("Slam", 1).audioClip.Value;
                    case "Mace Swing":
                        return (AudioClip) FiveKnights.preloadedGO["fk"].LocateMyFSM("FalseyControl").GetAction<AudioPlayerOneShotSingle>("S Attack", 0).audioClip.Value;
                    case "Slash":
                        return (AudioClip) FiveKnights.preloadedGO["PV"].LocateMyFSM("Control").GetAction<AudioPlayerOneShotSingle>("Slash1", 1).audioClip.Value;
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