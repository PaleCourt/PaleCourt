using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = Modding.Logger;

namespace FiveKnights.Misc
{
    public class ThroneFlash : MonoBehaviour
    {
        public GameObject throne;
        private GameObject appearEffect;
        private Animator animator;

        private void Awake()
        {
            appearEffect = transform.Find("Throne First Appear").gameObject;
            animator = appearEffect.GetComponent<Animator>();

            BoxCollider2D trigger = gameObject.AddComponent<BoxCollider2D>();
            trigger.offset = Vector2.zero;
            trigger.size = new Vector2(9f, 15f);
            trigger.isTrigger = true;
            appearEffect.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if(FiveKnights.Instance.SaveSettings.UnlockedChampionsCall && !FiveKnights.Instance.SaveSettings.SeenChampionsCall)
            {
                FiveKnights.Instance.SaveSettings.SeenChampionsCall = true;
                appearEffect.SetActive(true);
                throne.SetActive(false);
                StartCoroutine(FlashRoutine());
            }
        }

        private IEnumerator FlashRoutine()
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            yield return new WaitForSeconds(1.5f);
            FiveKnights.preloadedGO["ThroneCovered"].SetActive(false);
            throne.SetActive(true);
            GameObject.Find("throne").transform.Find("core_extras_0029_wp").gameObject.SetActive(FiveKnights.Instance.SaveSettings.UnlockedChampionsCall && FiveKnights.Instance.SaveSettings.SeenChampionsCall);
            yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
            animator.enabled = false;
        }
    }
}
