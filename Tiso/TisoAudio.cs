using System;
using System.Collections.Generic;
using UnityEngine;

namespace FiveKnights.Tiso;

public static class TisoAudio
{
    public static Dictionary<string, AudioClip> TisoAud;
    public enum Clip
    {
        SpikeHitWall,
        Jump,
        Land,
        LandHard,
        Shoot,
        Spin,
        ThrowShield,
        Walk,
        Death,
        Roar,
        Yell
    }

    public static AudioSource PlayAudio(MonoBehaviour from, Clip clip, float vol = 1f, float pitchVar = 0f,
        Func<bool> loopUntil = null)
    {
        AudioClip aClip = GetClipFromEnum(clip);
        return from.PlayAudio(aClip, vol, pitchVar, null, loopUntil);
    }

    private static AudioClip GetClipFromEnum(Clip clip)
    {
        return clip switch
        {
            Clip.SpikeHitWall => TisoAud["AudSpikeHitWall"],
            Clip.Jump => TisoAud["AudTisoJump"],
            Clip.Land => TisoAud["AudTisoLand"],
            Clip.LandHard => TisoAud["AudLand"],
            Clip.Shoot => TisoAud["AudTisoShoot"],
            Clip.Spin => TisoAud["AudTisoSpin"],
            Clip.ThrowShield => TisoAud["AudTisoThrowShield"],
            Clip.Walk => TisoAud["AudTisoWalk"],
            Clip.Death => TisoAud["AudTisoDeath"],
            Clip.Roar => TisoAud["AudTisoRoar"],
            Clip.Yell => TisoAud["AudTisoYell"],
            _ => TisoAud["AudTisoYell"],
        };
    }
}