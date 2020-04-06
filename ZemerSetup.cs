using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;

namespace FiveKnights
{
    public class ZemerSetup : MonoBehaviour
    {
        private GameObject _ogrim;
        private GameObject _pv;
        
        private const int AttunedHP = 1800;
        private const int AscendedHP = 2100;
        
        private void Awake()
        {
            gameObject.name = "Zemer";

            _ogrim = FiveKnights.preloadedGO["WD"];
            _pv = FiveKnights.preloadedGO["PV"];
            
            AddComponents();
            GetComponents();
            GetGameObjects();
            AssignFields();
            ModifyFSMs();

            gameObject.PrintSceneHierarchyTree();
        }
        
        private EnemyDeathEffectsUninfected _de;
        private EnemyDreamnailReaction _dn;
        private EnemyHitEffectsUninfected _he;
        private HealthManager _hm;
        private SpriteFlash _sf;
        private PlayMakerFSM _stun;
        private void AddComponents()
        {
            _de = gameObject.AddComponent<EnemyDeathEffectsUninfected>();
            _dn = gameObject.AddComponent<EnemyDreamnailReaction>();
            _he = gameObject.AddComponent<EnemyHitEffectsUninfected>();
            _hm = gameObject.AddComponent<HealthManager>();
            _sf = gameObject.AddComponent<SpriteFlash>();
            _stun = gameObject.AddComponent<PlayMakerFSM>();

            PlayMakerFSM stunCtrl = _pv.LocateMyFSM("Stun Control");
            foreach (FieldInfo fi in typeof(PlayMakerFSM).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                fi.SetValue(_stun, fi.GetValue(stunCtrl));
            
            int bossLevel = BossSceneController.Instance.BossLevel;
            _hm.hp = bossLevel > 0 ? AscendedHP : AttunedHP;
            _hm.hasSpecialDeath = true;
            _hm.SetAttr("stunControlFSM", _stun);
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
            GameObject counterFlash = FiveKnights.preloadedGO["CounterFX"];
            counterFlash.transform.localPosition = Vector3.right * 1.3f + Vector3.up * 0.1f;
            counterFlash.transform.localRotation = Quaternion.Euler(0, 0, transform.localScale.x * 100);
            _control.Fsm.GetFsmGameObject("CameraParent").Value = GameCameras.instance.gameObject;
            _control.Fsm.GetFsmGameObject("Counter Flash").Value = counterFlash;
            GameObject stunEffect = _pv.LocateMyFSM("Control").GetAction<SpawnObjectFromGlobalPool>("Stun Start", 17).gameObject.Value;
            stunEffect.PrintSceneHierarchyTree();
            _control.Fsm.GetFsmGameObject("Stun Effect").Value = stunEffect;

            GameObject audioPlayerActor = GameObject.Find("Audio Player Actor");
            AudioReverbFilter filter = audioPlayerActor.AddComponent<AudioReverbFilter>();
            filter.reverbPreset = AudioReverbPreset.User;
            filter.reverbLevel = 25;
            filter.reflectionsLevel = 25;
            
            _control.InsertMethod("Counter Stance", 0, () =>
            {
                _hm.IsInvincible = true;
                if (transform.localScale.x == 1)
                    _hm.InvincibleFromDirection = 8;
                else if (transform.localScale.x == -1)
                    _hm.InvincibleFromDirection = 9;
                
                _sf.flashFocusHeal();
            });
            
            _control.InsertCoroutine("Countered", 0, () => GameManager.instance.FreezeMoment(0.04f, 0.35f, 0.04f, 0f));

            _control.InsertMethod("Counter End", 0, () => _hm.IsInvincible = false);
            _control.InsertMethod("CS Antic", 0, () => _hm.IsInvincible = false);

            _stun.SetState("Init");
            
            _stun.Fsm.GetFsmInt("Stun Combo").Value = 7;
            _stun.Fsm.GetFsmInt("Stun Hit Max").Value = 13;
        }

        private void Log(object message) => Modding.Logger.Log("[Zemer Setup] " + message);
    }
}