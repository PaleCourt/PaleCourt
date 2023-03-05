using System;
using System.Collections;
using System.Collections.Generic;
using FrogCore;
using HutongGames.PlayMaker.Actions;
using SFCore.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace FiveKnights.Misc
{
    public class Artists : MonoBehaviour
    {
        private GameObject _sheo;
        private GameObject _smith;
        private MusicPlayer _sheoSnd;
        private MusicPlayer _smithSnd;
        private Dictionary<string, AudioClip> _clips;

        private void Awake()
        {
            USceneManager.activeSceneChanged += OnSceneChange;
        }

        private void OnSceneChange(Scene prev, Scene curr)
        {
            if (prev.name == "Room_nailmaster_02")
            {
                ABManager.ResetBundle(ABManager.Bundle.Artist);
                Destroy(_sheo);
                Destroy(_smith);
                Destroy(this);
            }
        }

        private void Start()
        {
            _clips = new Dictionary<string, AudioClip>();
            LoadArtist();
            Log("Instantiate artist");
            var parent = Instantiate(FiveKnights.preloadedGO["Artist"]);
            parent.transform.position = FiveKnights.preloadedGO["Artist"].transform.position;
            parent.SetActive(true);
            _sheo = parent.transform.Find("CuteScene").Find("Sheo").gameObject;
            _smith = parent.transform.Find("CuteScene").Find("Smith").gameObject;
            
            PlayMakerFSM spellControl = HeroController.instance.gameObject.LocateMyFSM("Spell Control");
            GameObject fireballParent = spellControl.GetAction<SpawnObjectFromGlobalPool>("Fireball 2", 3).gameObject.Value;
            PlayMakerFSM fireballCast = fireballParent.LocateMyFSM("Fireball Cast");
            GameObject actor = fireballCast.GetAction<AudioPlayerOneShotSingle>("Cast Right", 3).audioPlayer.Value;
            _sheoSnd = new MusicPlayer
            {
                Volume = 1f,
                Player = actor,
                Loop = false,
                MaxPitch = 1f,
                MinPitch = 1f,
                Spawn = _sheo
            };
            _smithSnd = new MusicPlayer
            {
                Volume = 1f,
                Player = actor,
                Loop = false,
                MaxPitch = 1f,
                MinPitch = 1f,
                Spawn = _smith
            };
            
            foreach (var remName in new[] { "Painting Parent", "NM Parent" })
            {
                GameObject.Find(remName).SetActive(false);
            }

            _smith.transform.Find("Hands").GetComponent<Animator>().enabled = true;
            CreateDialogue(_sheo,  new Vector2(42f, 11f),"SHEO_TITLE", "SHEO_DREAM", GetSheoDialogue, false, true, 3f);
            CreateDialogue(_smith, new Vector2(30.5f, 9.4f), "SMITH_TITLE", "SMITH_DREAM", GetSmithDialogue, false, true, 3f);
            
            Log("Instantiate artist 2");
        }

        private void CreateDialogue(GameObject target, Vector2 promptPos, string title, string dream,
            Func<DialogueCallbackOptions, DialogueOptions> dialogueSelecter, bool alwaysLeft, bool alwaysRight,
            float offset)
        {
            DialogueNPC dialogue = DialogueNPC.CreateInstance();
            dialogue.transform.position = target.transform.position; //new Vector3(31.78f, 7.5f);
            dialogue.transform.Find("Prompt Marker").position = promptPos;
            dialogue.DialogueSelector = dialogueSelecter;
            dialogue.GetComponent<MeshRenderer>().enabled = false;
            dialogue.SetTitle(title);
            dialogue.SetDreamKey(dream);
            dialogue.SetUp();
            PlayMakerFSM npcControl = dialogue.gameObject.LocateMyFSM("npc_control");
            npcControl.GetBoolVariable("Hero Always Right").Value = alwaysRight;
            npcControl.GetBoolVariable("Hero Always Left").Value = alwaysLeft;
            npcControl.GetFloatVariable("Move To Offset").Value = offset;
            BoxCollider2D bcSheo = target.GetComponent<BoxCollider2D>();
            BoxCollider2D dialogueBC = dialogue.GetComponent<BoxCollider2D>();
            BoxCollider2D dreamBC = dialogue.transform.Find("Dream Dialogue").GetComponent<BoxCollider2D>();
            dialogueBC.size = dreamBC.size = bcSheo.size;
            dialogueBC.offset = dreamBC.offset = bcSheo.offset;
        }

        private DialogueOptions GetSheoDialogue(DialogueCallbackOptions prev)
        {
            if (!prev.Continue)
            {
                return new ()
                    { Key = "SHEO_PC_1", Sheet = "Minor NPC", Type = DialogueType.Normal, Wait=PlayAnimSheo(), Continue = true };
            }
            switch (prev.Key)
            {
                case "SHEO_PC_1":
                    return new() { Key = "SHEO_PC_2", Sheet = "Minor NPC", Type = DialogueType.Normal, Continue = true };
                case "SHEO_PC_2":
                    return new () { Key = "SHEO_PC_3", Sheet = "Minor NPC", Type = DialogueType.Normal, Continue = true };
                default:
                    return new() { Continue = false, Wait = StopAnimSheo()};
            }
            
            IEnumerator PlayAnimSheo()
            {
                Animator anim = _sheo.GetComponent<Animator>();
                anim.enabled = true;
                _sheoSnd.Clip = _clips["sheo_1"];
                _sheoSnd.DoPlayRandomClip();
                anim.Play("SheoTalk", -1, 0f);
                yield return anim.WaitToFrame(3);
                anim.enabled = false;
            }
            
            IEnumerator StopAnimSheo()
            {
                Animator anim = _sheo.GetComponent<Animator>();
                anim.enabled = true;
                anim.Play("SheoStopTalk", -1, 0f);
                yield return anim.PlayToEnd();
            }
        }
        
        private DialogueOptions GetSmithDialogue(DialogueCallbackOptions prev)
        {
            if (!prev.Continue)
            {
                return new ()
                    { Key = "SMITH_PC_1", Sheet = "Minor NPC", Type = DialogueType.Normal, Wait=PlayAnimSmith(), Continue = true };
            }
            switch (prev.Key)
            {
                case "SMITH_PC_1":
                    return new() { Key = "SMITH_PC_2", Sheet = "Minor NPC", Type = DialogueType.Normal, Continue = true };
                case "SMITH_PC_2":
                    return new () { Key = "SMITH_PC_3", Sheet = "Minor NPC", Type = DialogueType.Normal, Continue = true };
                default:
                    return new() { Continue = false , Wait = StopAnimSmith()};
            }
            
            IEnumerator PlayAnimSmith()
            {
                yield return null;
                _smithSnd.Clip = _clips["smith_1"];
                _smithSnd.DoPlayRandomClip();
                _smith.transform.Find("Hands").GetComponent<Animator>().enabled = false;
                Animator anim = _smith.GetComponent<Animator>();
                anim.enabled = true;
            }
            
            IEnumerator StopAnimSmith()
            {
                yield return null;
                _smith.transform.Find("Hands").GetComponent<Animator>().enabled = true;
                Animator anim = _smith.GetComponent<Animator>();
                anim.enabled = false;
            }
        }
        
        private void LoadArtist()
        {
            Log("Loading Artist Bundle");
            if (FiveKnights.preloadedGO.TryGetValue("Artist", out var go) && go != null)
            {
                Log("Already Loaded Artist");
                return;
            }

            AssetBundle ab = ABManager.AssetBundles[ABManager.Bundle.Artist];
            foreach (var c in ab.LoadAllAssets<AnimationClip>())
            {
                Log($"Name of anim adding is {c.name}");
                FiveKnights.AnimClips[c.name] = c;
            }

            foreach (var c in ab.LoadAllAssets<AudioClip>())
            {
                Log($"Name of audio adding is {c.name}");
                _clips[c.name] = c;
            }

            FiveKnights.preloadedGO["Artist"] = ab.LoadAsset<GameObject>("Artists");

            Log("Finished Loading Artist Bundle");
        }

        private void OnDestroy()
        {
            USceneManager.activeSceneChanged -= OnSceneChange;
        }

        private static void Log(object o)
        {
            Modding.Logger.Log($"[Artist] {o}");
        }
    }
}