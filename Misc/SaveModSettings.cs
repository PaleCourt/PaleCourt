using System;
using Modding;

namespace FiveKnights
{
    [Serializable]
    public class SaveModSettings
    {
        public BossStatue.Completion CompletionIsma = new BossStatue.Completion
        {
            isUnlocked = true,
            hasBeenSeen = true
        };
        public BossStatue.Completion CompletionIsma2 = new BossStatue.Completion()
        {
            isUnlocked = true,
            hasBeenSeen = true
        };
        public BossStatue.Completion CompletionZemer = new BossStatue.Completion
        {
            isUnlocked = true,
            hasBeenSeen = true
        }; 
        public BossStatue.Completion CompletionZemer2 = new BossStatue.Completion
        {
            isUnlocked = true,
            hasBeenSeen = true
        };
        public BossStatue.Completion CompletionDryya = new BossStatue.Completion
        {
            isUnlocked = true,
            hasBeenSeen = true
        };
        public BossStatue.Completion CompletionHegemol = new BossStatue.Completion
        {
            isUnlocked = true,
            hasBeenSeen = true
        };

        public bool AltStatueIsma;

        public bool AltStatueZemer;
    }
}