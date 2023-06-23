using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FiveKnights.Misc;
using HutongGames.PlayMaker.Actions;
using SFCore.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace FiveKnights.Tiso
{
    public class TisoFinder : MonoBehaviour
    {
        private const string TisoScene = "GG_Brooding_Mawlek_V";
        private const string StatueScene = "GG_Workshop";
        
        private void Awake()
        {
            USceneManager.activeSceneChanged += OnSceneChange;
            On.BossStatue.SwapStatues += BossStatueOnSwapStatues;
            /*
            On.GameManager.BeginSceneTransition += GameManagerOnBeginSceneTransition;
        */
        }

        /*private void GameManagerOnBeginSceneTransition(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
        {
            if (self.sceneName == StatueScene)
            {
                BossStatue.Completion completion = PlayerData.instance.GetVariable<BossStatue.Completion>("statueStateBroodingMawlek"); 
                completion.usingAltVersion = false;
                PlayerData.instance.SetVariable("statueStateBroodingMawlek", completion);
                GameObject.Find("GG_Statue_Mawlek").GetComponent<BossStatue>().SetDreamVersion(false, false, false);
            }

            orig(self, info);
        }*/

        private IEnumerator BossStatueOnSwapStatues(On.BossStatue.orig_SwapStatues orig, BossStatue self, bool doanim)
        {
            if (self.name == "GG_Statue_Mawlek" && doanim)
            {
                FiveKnights.Instance.SaveSettings.AltStatueMawlek = !FiveKnights.Instance.SaveSettings.AltStatueMawlek;
            }
            yield return orig(self, doanim);
        }

        private void OnSceneChange(Scene prev, Scene curr)
        {
            if (curr.name is TisoScene && FiveKnights.Instance.SaveSettings.AltStatueMawlek)
            {
                ClearOldContent(curr);
                BossLoader.LoadTisoBundle();
                BossLoader.CreateTiso();
            }
            if (curr.name is StatueScene && (PlayerData.instance.GetBool(nameof(PlayerData.tisoEncounteredColosseum)) || 
                FiveKnights.Instance.SaveSettings.CompletionMawlek2.isUnlocked))
            {
                SetStatue();
            }
        }

        private void ClearOldContent(Scene curr)
        {
            var battle = curr.GetRootGameObjects().First(go => go.name == "Battle Scene");
            battle.LocateMyFSM("Activate Boss").enabled = false;
        }

        private void SetStatue()
        {
            Log("Setting Statue For Tiso.");
            
            GameObject statue = GameObject.Find("GG_Statue_Mawlek");
            Sprite tisoSprite = ABManager.AssetBundles[ABManager.Bundle.WSArena].LoadAsset<Sprite>("Tiso_Statue");
            
            if (statue == null)
            {
                Log("Warning: Did not find Mawlek Statue");
                return;
            }
            
            BossStatue bs = statue.GetComponent<BossStatue>();
            
            string sceneN = TisoScene;
            string stateN = "statueStateMawlek2";
            string key = "TISO_NAME";
            string desc = "TISO_DESC";
            
            BossScene scene = ScriptableObject.CreateInstance<BossScene>();
            scene.sceneName = sceneN;
            bs.dreamBossScene = scene;
            bs.dreamStatueStatePD = stateN;

            /* 56's code { */
            Destroy(USceneUtil.FindGameObjectInChildren(statue, "StatueAlt"));
            GameObject displayStatue = bs.statueDisplay;
            GameObject alt = Instantiate
            (
                displayStatue,
                displayStatue.transform.parent,
                true
            );

            alt.SetActive(true);
            var spr = alt.GetComponentInChildren<SpriteRenderer>(true);
            spr.sprite = tisoSprite;
            spr.gameObject.transform.position = new Vector3(45.54f, 8.16f,1.94f);
            spr.gameObject.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
            alt.name = "StatueAlt";
            bs.statueDisplayAlt = alt;
            /* } 56's code */
            
            BossStatue.BossUIDetails details = new BossStatue.BossUIDetails();
            details.nameKey = key;
            details.nameSheet = "Speech";
            details.descriptionKey = desc;
            details.descriptionSheet = "Speech";
            bs.dreamBossDetails = details;

            GameObject altLever = USceneUtil.FindGameObjectInChildren(statue, "alt_lever");
            altLever.SetActive(true);
            GameObject switchBracket = USceneUtil.FindGameObjectInChildren(altLever, "GG_statue_switch_bracket");
            switchBracket.SetActive(true);

            GameObject switchLever = USceneUtil.FindGameObjectInChildren(altLever, "GG_statue_switch_lever");
            switchLever.SetActive(true);

            BossStatueLever toggle = statue.GetComponentInChildren<BossStatueLever>(); 
            toggle.SetOwner(bs);
            toggle.SetState(true);

            FiveKnights.Instance.SaveSettings.CompletionMawlek2.isUnlocked = true;
            bs.DreamStatueState = FiveKnights.Instance.SaveSettings.CompletionMawlek2;

            StartCoroutine(Test());

            IEnumerator Test()
            {
                yield return new WaitForSeconds(1f);
                bs.SetDreamVersion(FiveKnights.Instance.SaveSettings.AltStatueMawlek, true, false);
                bs.SetDreamVersion(!FiveKnights.Instance.SaveSettings.AltStatueMawlek, true, false);
                bs.SetDreamVersion(FiveKnights.Instance.SaveSettings.AltStatueMawlek, true, false);
            }
            
            Log("Finish tiso statue.");
        }
        
        private void OnDestroy()
        {
            Log("Destroyed TisoFinder");

            BossStatue.Completion completion = PlayerData.instance.GetVariable<BossStatue.Completion>("statueStateBroodingMawlek");
            completion.usingAltVersion = false;
            PlayerData.instance.SetVariable("statueStateBroodingMawlek", completion);

            USceneManager.activeSceneChanged -= OnSceneChange;
            On.BossStatue.SwapStatues -= BossStatueOnSwapStatues;
        }
        
        private static void Log(object o)
        {
            Modding.Logger.Log($"[TisoFinder] {o}");
        }
    }
}