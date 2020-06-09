using System;
using Modding;
using UnityEngine;

namespace FiveKnights
{
    [Serializable]
    public class GlobalModSettings : ModSettings
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

        public bool AltStatueIsma
        {
            get => GetBool();
            set => SetBool(value);
        }
        
        public bool AltStatueZemer
        {
            get => GetBool();
            set => SetBool(value);
        }

        public void OnBeforeSerialize()
        {
            StringValues["CompletionIsma"] = JsonUtility.ToJson(CompletionIsma);
            StringValues["CompletionIsma2"] = JsonUtility.ToJson(CompletionIsma2);
            StringValues["CompletionZemer"] = JsonUtility.ToJson(CompletionZemer);
            StringValues["CompletionZemer2"] = JsonUtility.ToJson(CompletionZemer);
            StringValues["CompletionDryya"] = JsonUtility.ToJson(CompletionDryya);
            StringValues["CompletionHegemol"] = JsonUtility.ToJson(CompletionHegemol);
        }

        public void OnAfterDeserialize()
        {
            StringValues.TryGetValue("CompletionIsma", out string @out1);
            if (string.IsNullOrEmpty(@out1)) return;
            CompletionIsma = JsonUtility.FromJson<BossStatue.Completion>(@out1);

            StringValues.TryGetValue("CompletionIsma2", out string @out4);
            if (string.IsNullOrEmpty(@out4)) return;
            CompletionIsma2 = JsonUtility.FromJson<BossStatue.Completion>(@out4);

            StringValues.TryGetValue("CompletionZemer", out string @out2);
            if (string.IsNullOrEmpty(@out2)) return;
            CompletionZemer = JsonUtility.FromJson<BossStatue.Completion>(@out2);
            
            StringValues.TryGetValue("CompletionZemer2", out string @out6);
            if (string.IsNullOrEmpty(@out6)) return;
            CompletionZemer2 = JsonUtility.FromJson<BossStatue.Completion>(@out6);

            StringValues.TryGetValue("CompletionDryya", out string @out3);
            if (string.IsNullOrEmpty(@out3)) return;
            CompletionDryya = JsonUtility.FromJson<BossStatue.Completion>(@out3);

            StringValues.TryGetValue("CompletionHegemol", out string @out5);
            if (string.IsNullOrEmpty(@out5)) return;
            CompletionHegemol = JsonUtility.FromJson<BossStatue.Completion>(@out5);
        }
    }
}