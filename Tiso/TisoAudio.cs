using System;
using UnityEngine;

namespace FiveKnights.Tiso;

public static class TisoAudio
{
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
            Clip.SpikeHitWall => TisoFinder.TisoAud["AudSpikeHitWall"],
            Clip.Jump => TisoFinder.TisoAud["AudTisoJump"],
            Clip.Land => TisoFinder.TisoAud["AudTisoLand"],
            Clip.LandHard => TisoFinder.TisoAud["AudLand"],
            Clip.Shoot => TisoFinder.TisoAud["AudTisoShoot"],
            Clip.Spin => TisoFinder.TisoAud["AudTisoSpin"],
            Clip.ThrowShield => TisoFinder.TisoAud["AudTisoThrowShield"],
            Clip.Walk => TisoFinder.TisoAud["AudTisoWalk"],
            Clip.Death => TisoFinder.TisoAud["AudTisoDeath"],
            Clip.Roar => TisoFinder.TisoAud["AudTisoRoar"],
            Clip.Yell => TisoFinder.TisoAud["AudTisoYell"],
            _ => TisoFinder.TisoAud["AudTisoYell"],
        };
    }
}