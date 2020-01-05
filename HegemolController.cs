using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;
using ModCommon;
using System.Collections;
using System.Reflection;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using HutongGames.Utility;
using ModCommon.Util;
using Object = UnityEngine.Object;

namespace FiveKnights
{
    public class HegemolController : MonoBehaviour
    {
        private const float LeftX = 61.0f;
        private const float RightX = 91.0f;
        private const float GroundX = 7.4f;

        public GameObject ogrim;
        
        private HealthManager _hm;
        private EnemyHitEffectsUninfected _hitEffects;
        private PlayMakerFSM _control;

        private void Awake()
        {
            Log("Hegemol Awake");;
            
            _control = gameObject.LocateMyFSM("FalseyControl");
            _hm = GetComponent<HealthManager>();
            _hitEffects = gameObject.AddComponent<EnemyHitEffectsUninfected>();
            _hitEffects.enabled = true;

            On.EnemyHitEffectsArmoured.RecieveHitEffect += OnReceiveHitEffect;
        }

        private IEnumerator Start()
        {
            _hm.hp = 1600;

            AssignFields();
            
            //Stuff for getting hegemol to work
            GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = FiveKnights.SPRITES[0].texture;
            
            _control.RemoveAction<SpawnObjectFromGlobalPool>("S Attack Recover");
            _control.InsertCoroutine("S Attack Recover", 0, DungWave);
            _control.RemoveAction<AudioPlayerOneShot>("Voice?");
            _control.RemoveAction<AudioPlayerOneShot>("Voice? 2");
            
            Log("Setting State");
            _control.SetState("Init");
            yield return new WaitWhile(() => _control.ActiveStateName != "Dormant");
            _control.SendEvent("BATTLE START");
            Log("while true");
            while (true)
            {
                Log("hhh");
                if (_control.ActiveStateName.Contains("Hero Pos"))//JA Check Hero Pos
                {
                    float diff = HeroController.instance.transform.position.x - transform.position.x;
                    if (diff > 0) _control.SendEvent("RIGHT");
                    else _control.SendEvent("LEFT");
                }
                yield return new WaitForEndOfFrame();
            }
        }

        private void FixedUpdate()
        {
            
        }

        private void OnReceiveHitEffect(On.EnemyHitEffectsArmoured.orig_RecieveHitEffect orig, EnemyHitEffectsArmoured self, float attackDirection)
        {
            AudioSource audioPlayerPrefab = ogrim.GetComponent<EnemyHitEffectsUninfected>()
                .GetAttr<EnemyHitEffectsUninfected, AudioSource>("audioPlayerPrefab");
            AudioEvent enemyDamage = ogrim.GetComponent<EnemyHitEffectsUninfected>()
                .GetAttr<EnemyHitEffectsUninfected, AudioEvent>("enemyDamage");
            enemyDamage.SpawnAndPlayOneShot(audioPlayerPrefab, transform.position);

            orig(self, attackDirection);
        }
        
        private void AssignFields()
        {
            HealthManager ogrimHealth = ogrim.GetComponent<HealthManager>();
            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name.Contains("Prefab")))
            {
                fi.SetValue(_hm, fi.GetValue(ogrimHealth));
            }

            EnemyHitEffectsUninfected ogrimHitEffects = ogrim.GetComponent<EnemyHitEffectsUninfected>();
            foreach (FieldInfo fi in typeof(EnemyHitEffectsUninfected).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                fi.SetValue(_hitEffects, fi.GetValue(ogrimHitEffects));
            }
        }
        
        private IEnumerator DungWave()
        {
            Transform trans = transform;
            Vector2 pos = trans.position;
            float scaleX = trans.localScale.x;
            float xLeft = pos.x + scaleX - 2;
            float xRight = pos.x + scaleX + 2;
            while (xLeft >= LeftX || xRight <= RightX)
            {
                GameObject dungPillarR = Instantiate(FiveKnights.preloadedGO["pillar"], new Vector2(xRight, 12.0f), Quaternion.identity);
                dungPillarR.SetActive(true);
                dungPillarR.AddComponent<DungPillar>();
                
                GameObject dungPillarL = Instantiate(FiveKnights.preloadedGO["pillar"], new Vector2(xLeft, 12.0f), Quaternion.identity);
                dungPillarL.SetActive(true);
                Vector3 pillarRScale = dungPillarR.transform.localScale;
                dungPillarL.transform.localScale = new Vector3(-pillarRScale.x, pillarRScale.y, pillarRScale.z);
                dungPillarL.AddComponent<DungPillar>();

                xLeft -= 2;
                xRight += 2;
                
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void OnDestroy()
        {
            On.EnemyHitEffectsArmoured.RecieveHitEffect -= OnReceiveHitEffect;
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[Hegemol] " + o);
        }
    }
}
