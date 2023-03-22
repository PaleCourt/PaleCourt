using UnityEngine;
using SFCore.Utils;
using System.Collections;
using FiveKnights.BossManagement;
using GlobalEnums;
using Random = UnityEngine.Random;
using HutongGames.PlayMaker.Actions;

namespace FiveKnights.Dryya
{
    public class ElegyBeam : MonoBehaviour
    {
        public GameObject parent;
        public Vector2 offset;
        public bool activate = false;

        private Animator _anim;
        private float PosY = OWArenaFinder.IsInOverWorld ? 100.5f : 10f;

        private void Awake()
        {
            gameObject.layer = (int)PhysLayers.ENEMY_ATTACK;
            DamageHero dh = gameObject.AddComponent<DamageHero>();
            dh.damageDealt = 1;
            dh.hazardType = (int)HazardType.SPIKES;
            dh.shadowDashHazard = false;

            _anim = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            StartCoroutine(Beam());
        }

        private IEnumerator Beam()
        {
            Vector2 beamPos = new Vector2(HeroController.instance.transform.position.x, PosY);
            Quaternion randomRot = Quaternion.Euler(0, 0, Mathf.Sign(transform.localScale.x) * Random.Range(15, 105));
            gameObject.transform.SetPositionAndRotation(beamPos + offset, randomRot);
            
            _anim.Play("Beams");

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
            _anim.enabled = false;

            yield return new WaitUntil(() => activate);
            yield return new WaitForSeconds(0.1f);
            _anim.enabled = true;

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);
            _anim.enabled = false;

            yield return new WaitForSeconds(0.17f);
            _anim.enabled = true;
            Destroy(GetComponent<PolygonCollider2D>());
            Destroy(GetComponent<DamageHero>());

            yield return new WaitWhile(() => _anim.IsPlaying("Beams"));

            Destroy(gameObject);
        }
    }
}