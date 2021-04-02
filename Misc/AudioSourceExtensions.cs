using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace FiveKnights
{
    public static class AudioSourceExtensions
    {
        private static GameObject _fc = FiveKnights.preloadedGO["fk"];
        private static GameObject _lk = FiveKnights.preloadedGO["Kin"];
        private static GameObject _pv = FiveKnights.preloadedGO["PV"];
        private static GameObject _wd = FiveKnights.preloadedGO["WD"];

        private static PlayMakerFSM _fCtrl = _fc.LocateMyFSM("FalseyControl");
        private static PlayMakerFSM _ikCtrl = _lk.LocateMyFSM("IK Control");
        private static PlayMakerFSM _pvCtrl = _pv.LocateMyFSM("Control");
        private static PlayMakerFSM _dd = _wd.LocateMyFSM("Dung Defender");
        
        
        public static void Play(this AudioSource audio, string clipName, float pitchMin = 1.0f, float pitchMax = 1.0f, float time = 0.0f)
        {
            AudioClip GetAudioClip()
            {
                switch (clipName)
                {
                    case "Counter":
                        return (AudioClip) _pvCtrl.GetAction<AudioPlayerOneShotSingle>("Counter Stance", 1).audioClip.Value;
                    case "Dash":
                        return (AudioClip) _pvCtrl.GetAction<AudioPlayerOneShotSingle>("Dash", 1).audioClip.Value;
                    case "Dive":
                        return (AudioClip) _ikCtrl.GetAction<AudioPlaySimple>("Dstab Fall", 0).oneShotClip.Value;
                    case "Dung Pillar":
                        return (AudioClip) _dd.GetAction<AudioPlayerOneShotSingle>("Pillar", 0).audioClip.Value;
                    case "Heavy Land":
                        return (AudioClip) _ikCtrl.GetAction<AudioPlaySimple>("Dstab Land", 0).oneShotClip.Value;
                    case "Jump":
                        return (AudioClip) _ikCtrl.GetAction<AudioPlaySimple>("Jump").oneShotClip.Value;
                    case "Light Land":
                        return (AudioClip) _ikCtrl.GetAction<AudioPlaySimple>("Land", 0).oneShotClip.Value;
                    case "Mace Slam":
                        return (AudioClip) _fCtrl.GetAction<AudioPlayerOneShotSingle>("Slam", 1).audioClip.Value;
                    case "Mace Swing":
                        return (AudioClip) _fCtrl.GetAction<AudioPlayerOneShotSingle>("S Attack", 0).audioClip.Value;
                    case "Slash":
                        return (AudioClip) _pvCtrl.GetAction<AudioPlayerOneShotSingle>("Slash1", 1).audioClip.Value;
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