using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using DebugColliders = ModCommon.DebugColliders;

namespace FiveKnights.Isma
{
    public class PlantCtrl : MonoBehaviour
    {
        private Animator _anim;
        private HealthManager _hm;
        public List<float> PlantX;
        private const int PLANTHP = 35;
        public bool IsmaFight;

        private void Awake()
        {
            _anim = gameObject.GetComponent<Animator>();

            transform.position -= new Vector3(0f, 0.2f, 0f);
        }

        private IEnumerator Start()
        {
            yield return null;
            if (IsmaFight)
            {
                _hm = gameObject.AddComponent<HealthManager>();
                SetupHM();
                gameObject.AddComponent<Flash>();
                _hm.hp = PLANTHP;
            }
            var bc = GetComponent<BoxCollider2D>();
            gameObject.layer = 25;
            gameObject.SetActive(true);
            _anim.enabled = true;
            _anim.Play("PlantGrow");
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
            _anim.enabled = false;
            yield return new WaitForSeconds(0.9f);
            _anim.enabled = true;
            yield return new WaitWhile(() => _anim.IsPlaying());
            bc.isTrigger = false;
            bc.enabled = true;
            gameObject.AddComponent<ShadeOnlyPass>().disableCollider = bc;

            if (IsmaFight)
            {
                GameObject bnc = new GameObject("PillarPogo")
                {
                    layer = 11,
                    transform =
                    {
                        position = gameObject.transform.position,
                        rotation = gameObject.transform.rotation,
                        localScale = gameObject.transform.localScale,
                        parent = gameObject.transform
                    }
                };
                var col = bnc.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = bc.size;
                col.offset = bc.offset;
                bnc.SetActive(true);
                bnc.transform.parent = gameObject.transform;
            }
            
            yield return new WaitForSeconds(0.55f);
            if (!IsmaFight) StartCoroutine(Death());
        }

        private IEnumerator Death()
        {
            _anim.Play("PlantDie");
            yield return null;
            yield return new WaitWhile(() => _anim.IsPlaying());
            PlantX.Remove(gameObject.transform.GetPositionX());
            Destroy(gameObject);
        }

        private void SetupHM()
        {
            HealthManager wdHM = FiveKnights.preloadedGO["WD"].GetComponent<HealthManager>();
            HealthManager hm2 = gameObject.GetComponent<HealthManager>();
            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name.Contains("Prefab")))
            {
                fi.SetValue(hm2, fi.GetValue(wdHM));
            }
        }
    }
}
