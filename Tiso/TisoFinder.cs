using System.Collections;
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
        }

        private IEnumerator BossStatueOnSwapStatues(On.BossStatue.orig_SwapStatues orig, BossStatue self, bool doanim)
        {
            Log($"Doing swap for {self.name}");
            if (self.name == "GG_Statue_Mawlek" && doanim)
            {
                Log($"Doing swap for {self.name} got in");
                FiveKnights.Instance.SaveSettings.AltStatueMawlek = !FiveKnights.Instance.SaveSettings.AltStatueMawlek;
            }
            yield return orig(self, doanim);
        }

        private void OnSceneChange(Scene prev, Scene curr)
        {
            if (prev.name == TisoScene)
            {
                ABManager.ResetBundle(ABManager.Bundle.TisoBund);
                Destroy(FiveKnights.preloadedGO["Tiso"]);
                Destroy(this);
            }
            else if (curr.name is TisoScene)
            {
                ClearOldContent();
                LoadTiso();
                GameObject tiso = Instantiate(FiveKnights.preloadedGO["Tiso"]);
                tiso.SetActive(true);
                tiso.transform.position = HeroController.instance.transform.position;
                AssetBundle misc = ABManager.AssetBundles[ABManager.Bundle.Misc];
                FiveKnights.Materials["flash"] = misc.LoadAsset<Material>("UnlitFlashMat");
                tiso.AddComponent<TisoController>();
            }
            if (curr.name is StatueScene)
            {
                SetStatue();
            }
        }

        private void ClearOldContent()
        {
            var battle = GameObject.Find("Battle Scene");
            Destroy(battle.LocateMyFSM("Activate Boss"));
        }
        
        private void LoadTiso()
        {
            Log("Loading Tiso Bundle");
            if (FiveKnights.preloadedGO.TryGetValue("Tiso", out var go) && go != null)
            {
                Log("Already Loaded Tiso");
                return;
            }

            AssetBundle ab = ABManager.AssetBundles[ABManager.Bundle.TisoBund];
            

            FiveKnights.preloadedGO["Tiso"] = ab.LoadAsset<GameObject>("Tiso");

            Log("Finished Loading Tiso Bundle");
        }

        private void SetStatue()
        {
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
            Destroy(statue.FindGameObjectInChildren("StatueAlt"));
            GameObject displayStatue = bs.statueDisplay;
            GameObject alt = Instantiate
            (
                displayStatue,
                displayStatue.transform.parent,
                true
            );
            alt.SetActive(bs.UsingDreamVersion);
            var spr = alt.GetComponentInChildren<SpriteRenderer>(true);
            spr.sprite = tisoSprite;
            spr.gameObject.transform.position = new Vector3(45.54f, 8.16f,1.94f);
            spr.gameObject.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
            alt.name = "StatueAlt";
            bs.statueDisplayAlt = alt; //
            /* } 56's code */
            BossStatue.BossUIDetails details = new BossStatue.BossUIDetails();
            details.nameKey = key;
            details.nameSheet = "Speech";
            details.descriptionKey = desc;
            details.descriptionSheet = "Speech";
            bs.dreamBossDetails = details;

            GameObject altLever = statue.FindGameObjectInChildren("alt_lever");
            altLever.SetActive(true);
            GameObject switchBracket = altLever.FindGameObjectInChildren("GG_statue_switch_bracket");
            switchBracket.SetActive(true);

            GameObject switchLever = altLever.FindGameObjectInChildren("GG_statue_switch_lever");
            switchLever.SetActive(true);

            BossStatueLever toggle = statue.GetComponentInChildren<BossStatueLever>();
            toggle.SetOwner(bs);
            toggle.SetState(true);

            FiveKnights.Instance.SaveSettings.CompletionMawlek2.isUnlocked = true;
            bs.DreamStatueState = FiveKnights.Instance.SaveSettings.CompletionMawlek2;
            Log($"Alt stat is at {FiveKnights.Instance.SaveSettings.AltStatueMawlek}");
            bs.SetDreamVersion(FiveKnights.Instance.SaveSettings.AltStatueMawlek, FiveKnights.Instance.SaveSettings.AltStatueMawlek, false);



            /*if(FiveKnights.Instance.SaveSettings.CompletionZemer2.isUnlocked)
            {
                
            }*/
        }

        private void OnDestroy()
        {
            USceneManager.activeSceneChanged -= OnSceneChange;
        }
        
        private static void Log(object o)
        {
            Modding.Logger.Log($"[TisoFinder] {o}");
        }
    }
}