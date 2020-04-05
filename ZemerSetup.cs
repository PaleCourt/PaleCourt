using System.Linq;
using System.Reflection;
using ModCommon;
using UnityEngine;

namespace FiveKnights
{
    public class ZemerSetup : MonoBehaviour
    {
        private GameObject _ogrim;
        
        private const int AttunedHP = 800;
        private const int AscendedHP = 1000;
        
        private void Awake()
        {
            gameObject.name = "Zemer";

            _ogrim = FiveKnights.preloadedGO["WD"];
            
            Log("Adding Components");
            AddComponents();
            Log("Getting Components");
            GetComponents();
            Log("Getting GameObjects");
            GetGameObjects();
            Log("Assigning Fields");
            AssignFields();
            Log("Modifying FSMs");
            ModifyFSMs();

            gameObject.PrintSceneHierarchyTree();
        }

        private AudioSource _as;
        private EnemyDeathEffectsUninfected _de;
        private EnemyDreamnailReaction _dn;
        private EnemyHitEffectsUninfected _he;
        private HealthManager _hm;
        private SpriteFlash _sf;
        private void AddComponents()
        {
            _as = gameObject.AddComponent<AudioSource>();
            _de = gameObject.AddComponent<EnemyDeathEffectsUninfected>();
            _dn = gameObject.AddComponent<EnemyDreamnailReaction>();
            _he = gameObject.AddComponent<EnemyHitEffectsUninfected>();
            _hm = gameObject.AddComponent<HealthManager>();
            _sf = gameObject.AddComponent<SpriteFlash>();

            int bossLevel = BossSceneController.Instance.BossLevel;
            _hm.hp = bossLevel > 0 ? AscendedHP : AttunedHP;
            _hm.hasSpecialDeath = true;
        }

        private tk2dSprite _sprite;
        private void GetComponents()
        {
            _sprite = GetComponent<tk2dSprite>();

            Shader shader = _ogrim.GetComponent<tk2dSprite>().Collection.spriteDefinitions[0].material.shader;

            foreach (tk2dSpriteDefinition spriteDef in _sprite.Collection.spriteDefinitions)
                spriteDef.material.shader = shader;
        }

        private void GetGameObjects()
        {
            GameObject.Find("Burrow Effect").SetActive(false);

            PlayMakerFSM nailClashTink = FiveKnights.preloadedGO["Slash"].LocateMyFSM("nail_clash_tink");
            foreach (PolygonCollider2D poly in GetComponentsInChildren<PolygonCollider2D>(true))
            {
                PlayMakerFSM pfsm = poly.gameObject.AddComponent<PlayMakerFSM>();
                foreach (FieldInfo fi in typeof(PlayMakerFSM).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                    fi.SetValue(pfsm, fi.GetValue(nailClashTink));
            }
        }

        private void AssignFields()
        {
            EnemyDeathEffectsUninfected ogrimDeathEffects = _ogrim.GetComponent<EnemyDeathEffectsUninfected>();
            foreach (FieldInfo fi in typeof(EnemyDeathEffectsUninfected).GetFields(BindingFlags.Instance | BindingFlags.Public))
                fi.SetValue(_de, fi.GetValue(ogrimDeathEffects));
            
            EnemyHitEffectsUninfected ogrimHitEffects = _ogrim.GetComponent<EnemyHitEffectsUninfected>();
            foreach (FieldInfo fi in typeof(EnemyHitEffectsUninfected).GetFields(BindingFlags.Instance | BindingFlags.Public))
                fi.SetValue(_he, fi.GetValue(ogrimHitEffects));

            HealthManager ogrimHealth = _ogrim.GetComponent<HealthManager>();
            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(x => x.Name.Contains("Prefab")))
                fi.SetValue(_hm, fi.GetValue(ogrimHealth));
        }
        
        private PlayMakerFSM _control;
        private void ModifyFSMs()
        {
            GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false; 
            
            _control = gameObject.LocateMyFSM("Control");

            _control.SetState("Init");

            _control.Fsm.GetFsmGameObject("Hero").Value = HeroController.instance.gameObject;
        }
        
        private void Log(object message) => Modding.Logger.Log("[Zemer Setup] " + message);
    }
}