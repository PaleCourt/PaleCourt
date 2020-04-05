using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FiveKnights
{
    public class DryyaSetup : MonoBehaviour
    {
        private int _hp = 1650;
        
        private PlayMakerFSM _mageLord;
        private PlayMakerFSM _control;
        
        private GameObject _diveShockwave;
        private GameObject _elegyBeam1;
        private GameObject _elegyBeam2;
        private GameObject _ogrim;
        private GameObject _slash1Collider1;
        private GameObject _slash1Collider2;
        private GameObject _slash2Collider1;
        private GameObject _slash2Collider2;
        private GameObject _slash3Collider1;
        private GameObject _slash3Collider2;
        private GameObject _cheekySlashCollider1;
        private GameObject _cheekySlashCollider2;
        private GameObject _cheekySlashCollider3;
        private List<GameObject> _slashes;
        private GameObject _stabFlash;

        private string[] _dreamNailDialogue =
        {
            "DRYYA_DIALOG_1",
            "DRYYA_DIALOG_2",
            "DRYYA_DIALOG_3",
            "DRYYA_DIALOG_4",
            "DRYYA_DIALOG_5",
        };

        private void Awake()
        {
            GameObject go = gameObject;
            go.SetActive(true);
            go.layer = 11;

            _corpse = gameObject.FindGameObjectInChildren("Corpse Dryya");
            _diveShockwave = gameObject.FindGameObjectInChildren("Dive Shockwave");
            _elegyBeam1 = gameObject.FindGameObjectInChildren("Elegy Beam 1");
            _elegyBeam2 = gameObject.FindGameObjectInChildren("Elegy Beam 2");
            _slash1Collider1 = gameObject.FindGameObjectInChildren("Slash 1 Collider 1");
            _slash1Collider2 = gameObject.FindGameObjectInChildren("Slash 1 Collider 2");
            _slash2Collider1 = gameObject.FindGameObjectInChildren("Slash 2 Collider 1");
            _slash2Collider2 = gameObject.FindGameObjectInChildren("Slash 2 Collider 2");
            _slash3Collider1 = gameObject.FindGameObjectInChildren("Slash 3 Collider 1");
            _slash3Collider2 = gameObject.FindGameObjectInChildren("Slash 3 Collider 2");
            _cheekySlashCollider1 = gameObject.FindGameObjectInChildren("Slash Collider 1");
            _cheekySlashCollider2 = gameObject.FindGameObjectInChildren("Slash Collider 2");
            _cheekySlashCollider3 = gameObject.FindGameObjectInChildren("Slash Collider 3");
            _slashes = new List<GameObject>
            {
                _slash1Collider1,
                _slash1Collider2,
                _slash2Collider1,
                _slash2Collider2,
                _slash3Collider1,
                _slash3Collider2,
                _cheekySlashCollider1,
                _cheekySlashCollider2,
                _cheekySlashCollider3,
            };
            
            _stabFlash = gameObject.FindGameObjectInChildren("Stab Flash");
            
            _ogrim = FiveKnights.preloadedGO["WD"];
            
            _dreamImpactPrefab = _ogrim.GetComponent<EnemyDreamnailReaction>().GetAttr<EnemyDreamnailReaction, GameObject>("dreamImpactPrefab");
            
            AddComponents();
            GetComponents();
            
            _mageLord = FiveKnights.preloadedGO["Mage"].LocateMyFSM("Mage Lord");
            _control = gameObject.LocateMyFSM("Control");

            _control.SetState("Init");
            _control.Fsm.GetFsmGameObject("Hero").Value = HeroController.instance.gameObject;

            _control.InsertMethod("Activate", 0, () => _hm.enabled = true);
            
            _control.InsertMethod("Phase Check", 0, () => _control.Fsm.GetFsmInt("HP").Value = _hm.hp);

            _control.InsertMethod("Counter Stance", 0, () =>
            {
                _hm.IsInvincible = true;
                if (transform.localScale.x == 1)
                    _hm.InvincibleFromDirection = 8;
                else if (transform.localScale.x == -1)
                    _hm.InvincibleFromDirection = 9;
                
                _spriteFlash.flashFocusHeal();

                Vector2 fxPos = transform.position + Vector3.right * 1.3f * -transform.localScale.x + Vector3.up * 0.1f;
                Quaternion fxRot = Quaternion.Euler(0, 0, -transform.localScale.x * -60);
                GameObject counterFX = Instantiate(FiveKnights.preloadedGO["CounterFX"], fxPos, fxRot);
                counterFX.SetActive(true);
            });
            _control.InsertMethod("Counter End", 0, () => _hm.IsInvincible = false);
            _control.InsertMethod("Counter Slash Antic", 0, () => _hm.IsInvincible = false);

            _control.InsertCoroutine("Countered", 0, () => GameManager.instance.FreezeMoment(0.04f, 0.35f, 0.04f, 0f));
            
            _control.InsertMethod("Dive Land Heavy", 0, () => SpawnShockwaves(1.5f, 50, 1));

            GameObject.Find("Burrow Effect").SetActive(false);
            GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;
            
            AssignFields();
            
            gameObject.PrintSceneHierarchyTree();
            
            _hm.OnDeath += DeathHandler;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
            On.HealthManager.TakeDamage += OnTakeDamage;
        }

        private void DeathHandler()
        {
            CustomWP.Instance.wonLastFight = true;
        }

        private GameObject _dreamImpactPrefab;
        private void OnReceiveDreamImpact(On.EnemyDreamnailReaction.orig_RecieveDreamImpact orig, EnemyDreamnailReaction self)
        {
            if (self.name.Contains("Dryya"))
            {
                _dreamNailReaction.SetConvoTitle(_dreamNailDialogue[Random.Range(0, _dreamNailDialogue.Length)]);
                _dreamImpactPrefab.Spawn(transform.position);
            }
            
            orig(self);
        }
        
        private void OnTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.gameObject.name.Contains("Dryya"))
                _spriteFlash.flashFocusHeal();

            orig(self, hitInstance);
        }
        
        private GameObject _corpse;
        private tk2dSprite _sprite;
        private void GetComponents()
        {
            _sprite = GetComponent<tk2dSprite>();
            EnemyDeathEffects deathEffects = GetComponent<EnemyDeathEffects>();
            deathEffects.corpseSpawnPoint = transform.position + Vector3.up * 2;
            
            deathEffects.SetAttr("corpsePrefab", _corpse);
            deathEffects.SetAttr("corpseFlingSpeed", 25.0f);

            Shader shader = _ogrim.GetComponent<tk2dSprite>().Collection.spriteDefinitions[0].material.shader;
            
            foreach (tk2dSpriteDefinition spriteDef in _sprite.Collection.spriteDefinitions)
                spriteDef.material.shader = shader;
            
            tk2dSprite shockwaveSprite = _diveShockwave.GetComponent<tk2dSprite>();
            foreach (tk2dSpriteDefinition spriteDef in shockwaveSprite.Collection.spriteDefinitions)
                spriteDef.material.shader = Shader.Find("tk2d/BlendVertexColor");

            tk2dSprite beam1Sprite = _elegyBeam1.GetComponent<tk2dSprite>();
            foreach (tk2dSpriteDefinition spriteDef in beam1Sprite.Collection.spriteDefinitions)
                spriteDef.material.shader = Shader.Find("tk2d/BlendVertexColor");
            
            tk2dSprite beam2Sprite = _elegyBeam2.GetComponent<tk2dSprite>();
            foreach (tk2dSpriteDefinition spriteDef in beam2Sprite.Collection.spriteDefinitions)
                spriteDef.material.shader = Shader.Find("tk2d/BlendVertexColor");
            
            tk2dSprite flashSprite = _stabFlash.GetComponent<tk2dSprite>();
            foreach (tk2dSpriteDefinition spriteDef in flashSprite.Collection.spriteDefinitions)
                spriteDef.material.shader = Shader.Find("tk2d/BlendVertexColor");
        }
        
        private EnemyDeathEffectsUninfected _deathEffects;
        private EnemyDreamnailReaction _dreamNailReaction;
        private EnemyHitEffectsUninfected _hitEffects;
        private HealthManager _hm;
        private SpriteFlash _spriteFlash;
        private void AddComponents()
        {
            _deathEffects = gameObject.AddComponent<EnemyDeathEffectsUninfected>();

            _dreamNailReaction = gameObject.AddComponent<EnemyDreamnailReaction>();
            _dreamNailReaction.enabled = true;
            _dreamNailReaction.SetConvoTitle(_dreamNailDialogue[Random.Range(0, _dreamNailDialogue.Length)]);
            
            _hitEffects = gameObject.AddComponent<EnemyHitEffectsUninfected>();
            _hitEffects.enabled = true;

            _hm = gameObject.AddComponent<HealthManager>();
            _hm.enabled = false;
            _hm.hp = _hp;

            _spriteFlash = gameObject.AddComponent<SpriteFlash>();

            _elegyBeam1.AddComponent<ElegyBeam>().parent = gameObject;

            _elegyBeam2.AddComponent<ElegyBeam>().parent = gameObject;

            PlayMakerFSM nailClashTink = FiveKnights.preloadedGO["Slash"].LocateMyFSM("nail_clash_tink");
            
            foreach (GameObject slash in _slashes)
            {
                PlayMakerFSM pfsm = slash.AddComponent<PlayMakerFSM>();
                foreach (FieldInfo fi in typeof(PlayMakerFSM).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                    fi.SetValue(pfsm, fi.GetValue(nailClashTink));
            }

            _stabFlash.AddComponent<DeactivateAfter2dtkAnimation>();
        }
        
        private void AssignFields()
        {
            EnemyDeathEffectsUninfected ogrimDeathEffects = _ogrim.GetComponent<EnemyDeathEffectsUninfected>();
            foreach (FieldInfo fi in typeof(EnemyDeathEffectsUninfected).GetFields(BindingFlags.Instance | BindingFlags.Public))
                fi.SetValue(_deathEffects, fi.GetValue(ogrimDeathEffects));
            
            EnemyHitEffectsUninfected ogrimHitEffects = _ogrim.GetComponent<EnemyHitEffectsUninfected>();
            foreach (FieldInfo fi in typeof(EnemyHitEffectsUninfected).GetFields(BindingFlags.Instance | BindingFlags.Public))
                fi.SetValue(_hitEffects, fi.GetValue(ogrimHitEffects));

            HealthManager ogrimHealth = _ogrim.GetComponent<HealthManager>();
            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(x => x.Name.Contains("Prefab")))
                fi.SetValue(_hm, fi.GetValue(ogrimHealth));
        }

        private void SpawnShockwaves(float vertScale, float speed, int damage)
        {
            bool[] facingRightBools = {false, true};
            Vector2 pos = transform.position;
            foreach (bool facingRight in facingRightBools)
            {
                GameObject shockwave =
                    Instantiate(_mageLord.GetAction<SpawnObjectFromGlobalPool>("Quake Waves", 0).gameObject.Value);
                PlayMakerFSM shockFSM = shockwave.LocateMyFSM("shockwave");
                shockFSM.FsmVariables.FindFsmBool("Facing Right").Value = facingRight;
                shockFSM.FsmVariables.FindFsmFloat("Speed").Value = speed;
                shockwave.AddComponent<DamageHero>().damageDealt = damage;
                shockwave.SetActive(true);
                shockwave.transform.SetPosition2D(new Vector2(pos.x + (facingRight ? 0.5f : -0.5f), 6f));
                shockwave.transform.SetScaleX(vertScale);
            }
        }
        
        private void OnDestroy()
        {
            _hm.OnDeath += DeathHandler;
            On.EnemyDreamnailReaction.RecieveDreamImpact -= OnReceiveDreamImpact;
            On.HealthManager.TakeDamage -= OnTakeDamage;
        }
        
        private void Log(object message) => Modding.Logger.Log("[Dryya Setup] " + message);
    }
}