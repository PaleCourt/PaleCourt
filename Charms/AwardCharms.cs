using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using SFCore.Utils;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vasi;
using Random = UnityEngine.Random;

namespace FiveKnights
{

    internal partial class AwardCharms : MonoBehaviour
    {
        private HeroController _hc;
        private PlayerData _pd;
        private GameObject _charmGet;
        private AssetBundle _charmUnlock;
        private SaveModSettings _settings = FiveKnights.Instance.SaveSettings;
        private bool pauseShroom = false;
        public bool[] bossWin = new bool[4];

        //public static BindingFlags all = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.CreateInstance | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding | BindingFlags.FlattenHierarchy | BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.IgnoreCase | BindingFlags.IgnoreReturn | BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.SetProperty | BindingFlags.OptionalParamBinding | BindingFlags.PutDispProperty | BindingFlags.SuppressChangeType | BindingFlags.PutRefDispProperty;
        
        public void Awake()
        {
            ModHooks.LanguageGetHook += CutsceneDialogue;
            ModHooks.BeforeSceneLoadHook += SceneCheck;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += BloomPlacement;
          
        }

        private void BloomPlacement(Scene From, Scene To)
        {

            if (To.name == "White_Palace_08")
            {
                if (!FiveKnights.Instance.SaveSettings.gotCharms[3])
                {
                    var saveSettings = FiveKnights.Instance.SaveSettings;

                    GameObject bloomShiny = Instantiate(FiveKnights.preloadedGO["Shiny"]);
                    Destroy(bloomShiny.transform.GetChild(0).gameObject.GetComponent<PersistentBoolItem>());

                    bloomShiny.SetActive(false);
                    bloomShiny.transform.GetChild(0).gameObject.SetActive(true);
                    bloomShiny.transform.position = new Vector3(110.8f, 20.6f, 0.05f);

                    var shinyFsm = bloomShiny.transform.GetChild(0).gameObject.LocateMyFSM("Shiny Control");
                    var shinyFsmVars = shinyFsm.FsmVariables;

                    shinyFsm.ChangeFsmTransition("Init", "FINISHED", "Idle");
                    shinyFsm.GetState("Type").InsertMethod(() => CharmCutscene("bloom"), 0);
                    shinyFsm.ChangeFsmTransition("Type", "QUEEN", "Msg");
                    shinyFsm.GetState("Hero Up").RemoveAction<CallMethodProper>();


                    shinyFsmVars.FindFsmBool("Activated").Value = false;
                    shinyFsmVars.FindFsmBool("Queen Charm").Value = true;

                    bloomShiny.SetActive(true);

                }
            }
        }

        private string SceneCheck(string sceneName)
        {
            var settings = FiveKnights.Instance.SaveSettings;
            var scene = GameManager.instance.sceneName;
            if((scene == "dryya overworld" && !settings.gotCharms[0] && bossWin[0]) ||
                (scene == "zemer overworld arena" && !settings.gotCharms[1] && bossWin[1]) ||
                (scene == "hegemol overworld arena" && !settings.gotCharms[2] && bossWin[2]) || 
                (scene == "isma overworld" && !settings.upgradedCharm_10 && bossWin[3]))

            {
                bossWin[0] = false;
                bossWin[1] = false;
                bossWin[2] = false;
                bossWin[3] = false;

                var boss = scene.Split(' ');
                StartCoroutine(AwardCharm(boss[0]));        
            }
            return sceneName;
        }

        private IEnumerator AwardCharm(string boss)
        {
            yield return new WaitUntil(() => HeroController.instance != null);

            // Award charms now so people can skip the cutscenes if they really want
            var settings = FiveKnights.Instance.SaveSettings;
            if(boss == "isma")
            {
                PlayerData.instance.gotCharm_10 = true;
                settings.upgradedCharm_10 = true;
                PlayerData.instance.newCharm_10 = true;
            }
            else if(boss == "dryya")
            {
                settings.gotCharms[0] = true;
                settings.newCharms[0] = true;
            }
            else if(boss == "zemer")
            {
                settings.gotCharms[1] = true;
                settings.newCharms[1] = true;
            }
            else if(boss == "hegemol")
            {
                settings.gotCharms[2] = true;
                settings.newCharms[2] = true;
            }

            yield return new WaitUntil(() => HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name == "Prostrate Rise");
            yield return new WaitUntil(() => HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name != "Prostrate Rise");
            if (!pauseShroom)
            {
                _hc.IgnoreInput();
                CharmCutscene(boss);
            }
            
        }

        private string CutsceneDialogue(string key, string sheetTitle, string orig)
        {
            switch (key)
            {
                case "BLOOM_NAME":
                    return "Abyssal Bloom";
                case "LAMENT_NAME":
                    return "Vessel's Lament";
                case "PURITY_NAME":
                    return "Mark of Purity";
                case "BOON_NAME":
                    return "Boon of Hallownest";
                case "CREST_NAME":
                    return "King's Honour";
                case "CUSTOM_ITEM_INTROS":
                    return "Received the";
            }
            
            return orig; 
            
        }

        private void CharmCutscene(string boss)
        {
            pauseShroom = true;
            _charmUnlock = ABManager.AssetBundles[ABManager.Bundle.CharmUnlock];
            _charmGet = Instantiate(FiveKnights.preloadedGO["CharmGet"]);

            string charm = null;
            string charmName = null;
            string audioName = null;
            float upDelay = 2.4f;
            int charmNumber = 0; 
            switch (boss)
            {
                case "dryya":
                    charm = "PurityAppear";
                    charmName = "PURITY_NAME";
                    upDelay = 2.4f;
                    audioName = "purity_charm_get";
                    charmNumber = 0;
                    break;
                case "zemer":
                    charm = "LamentAppear";
                    charmName = "LAMENT_NAME";
                    upDelay = 2.4f;
                    audioName = "spell_information_merged";
                    charmNumber = 1;
                    break;
                case "isma":
                    charm = "CrestUpgrade";
                    charmName = "CREST_NAME";
                    audioName = "kings_brand_get";
                    upDelay = 2.8f;
            
                    break;
                case "hegemol":
                    charm = "BoonAppear";
                    charmName = "BOON_NAME";
                    upDelay = 2.4f;
                    audioName = "spell_information_merged";
                    charmNumber = 2;
                    break;
                case "bloom" :
                    charm = "BloomGrow";
                    charmName = "BLOOM_NAME";
                    audioName = "abyss_bloom";
                    upDelay = 2.4f;
                    charmNumber = 3;
                    break;
            }

            SetupUI(_charmGet, charmName, upDelay);
            HeroController.instance.RelinquishControl();
            _charmGet.SetActive(true);
            
            GameObject CharmAnim = Instantiate(_charmUnlock.LoadAsset<GameObject>(charm));
            CharmAnim.SetActive(false);
            CharmAnim.transform.parent = _charmGet.transform;
            CharmAnim.transform.position = new Vector3(.19f, 2.5f, -3.1f);
            CharmAnim.transform.localScale = new Vector3(1.25f, 1.25f, 1);
            CharmAnim.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            CharmAnim.GetComponent<SpriteRenderer>().sortingLayerID = 629535577;
            //BloomAnim.AddComponent<PlayMakerFSM>();
            CharmAnim.AddComponent<FadeOut>().enabled = false;
            CharmAnim.GetComponent<FadeOut>().fadeTime = 1;
            //PlayMakerFSM colorFader = _charmGet.Find("Single Frag").GetComponent<PlayMakerFSM>();
            //Semi-functional code for cloning an FSM, commented due to error, just created the effect I wanted with script
            /*foreach (FieldInfo fi in typeof(PlayMakerFSM).GetFields(all))
            {
                fi.SetValue(BloomAnim.GetComponent<PlayMakerFSM>(), fi.GetValue(colorFader));
            }
            foreach (PropertyInfo fi in typeof(PlayMakerFSM).GetProperties(all))
            {
                fi.SetValue(BloomAnim.GetComponent<PlayMakerFSM>(), fi.GetValue(colorFader));
            }*/
            CharmAnim.layer = (int)PhysLayers.UI;


            StartCoroutine(AnimCoroutine(CharmAnim, charmNumber, boss, audioName));
        }

        private void SetupUI(GameObject UI, string charmName, float upDelay)
        {
            Destroy(UI.Find("Single Frag"));
            var fsm = UI.LocateMyFSM("Msg Control");
            Destroy(UI.Find("White Form"));
            fsm.GetState("Init").ChangeFsmTransition("BLACK", "Text");
            fsm.GetState("Init").ChangeFsmTransition("KING", "Text");
            fsm.GetState("Init").ChangeFsmTransition("QUEEN", "Text");
            fsm.InsertAction("Init", new GetLanguageString() {sheetName = "Prompts", convName = charmName, storeValue = fsm.FindFsmStringVariable("Game Text") }, 0);
            // Original FSM uses this bit
            //fsm.InsertAction("Init", new GetLanguageStringProcessed() { sheetName = "Prompts", convName = "GET_ITEM_INTROS", storeValue = fsm.FindFsmStringVariable("Game Text") }, 1);
            fsm.InsertAction("Init", new SetTextMeshProText() { textString = fsm.FindFsmStringVariable("Game Text"), gameObject = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.SpecifyGameObject, GameObject = fsm.gameObject.Find("Item Name") } }, 1);
            fsm.InsertAction("Init", new GetLanguageString() {sheetName = "Prompts",  convName = "CUSTOM_ITEM_INTROS", storeValue = fsm.FindFsmStringVariable("Game Text") }, 2);
            fsm.InsertAction("Init", new SetTextMeshProText() { textString = fsm.FindFsmStringVariable("Game Text"), gameObject = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.SpecifyGameObject, GameObject = fsm.gameObject.Find("Item Name Prefix") } }, 3);
            _charmGet.Find("Item Name").LocateMyFSM("color_fader").FindFsmFloatVariable("Up Delay").Value = upDelay;
            _charmGet.Find("Item Name Prefix").LocateMyFSM("color_fader").FindFsmFloatVariable("Up Delay").Value = upDelay;
        }

        private IEnumerator AnimCoroutine(GameObject CharmAnim, int charmNumber, string boss, string audioName)
        {
            if (boss == "zemer" || boss == "hegemol")
            {
                this.PlayAudio(_charmUnlock.LoadAsset<AudioClip>(audioName));
                yield return new WaitForSeconds(1.5f);
            }
            else if (boss == "dryya")
            {
                this.PlayAudio(_charmUnlock.LoadAsset<AudioClip>("spell_information_screen"));
                yield return new WaitForSeconds(1.5f);
                this.PlayAudio(_charmUnlock.LoadAsset<AudioClip>(audioName));
            }
            else if (boss == "isma")
            {
                this.PlayAudio(_charmUnlock.LoadAsset<AudioClip>("spell_information_screen"));
                yield return new WaitForSeconds(1.5f);
                CharmAnim.SetActive(true);
                CharmAnim.GetComponent<SpriteRenderer>().enabled = true;
                yield return new WaitForSeconds(.5f);
                this.PlayAudio(_charmUnlock.LoadAsset<AudioClip>(audioName));
            }
            else if (boss == "bloom")
            {
                yield return new WaitForSeconds(1.5f);
                this.PlayAudio(_charmUnlock.LoadAsset<AudioClip>(audioName));
            }


            CharmAnim.SetActive(true);
            CharmAnim.GetComponent<SpriteRenderer>().enabled = true;


            yield return new WaitUntil(() =>_charmGet.LocateMyFSM("Msg Control").GetState("Down").GetAction<SendEventByName>().Finished);
            CharmAnim.GetComponent<FadeOut>().enabled = true;
            yield return new WaitForSeconds(1.2f);
            if (boss == "isma")
            {
                PlayerData.instance.gotCharm_10 = true;
                FiveKnights.Instance.SaveSettings.upgradedCharm_10 = true;
                PlayerData.instance.newCharm_10 = true;
            }
            else
            {
                FiveKnights.Instance.SaveSettings.gotCharms[charmNumber] = true;
                FiveKnights.Instance.SaveSettings.newCharms[charmNumber] = true;
            }
            HeroController.instance.RegainControl();
            _hc.AcceptInput();
            pauseShroom = false;
        }

        private class FadeOut : MonoBehaviour
        {
            float startTime;
            float alpha;
            public float fadeTime;
            void Start()
            {
                startTime = Time.time;
                alpha = gameObject.GetComponent<SpriteRenderer>().color.a;
            }
            void Update()
            {
                if (alpha > 0)
                {
                    alpha = ((startTime + fadeTime) - Time.time);
                    gameObject.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, alpha);
                }

            }
        }

        private void OnDestroy()
		{
            Log("Destroyed AwardCharms");
            ModHooks.LanguageGetHook -= CutsceneDialogue;
            ModHooks.BeforeSceneLoadHook -= SceneCheck;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= BloomPlacement;
        }

        private static void Log(object message) => Modding.Logger.Log("[FiveKnights][AwardCharms] " + message);
    }
}
