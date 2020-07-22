using UnityEngine;
using System.Collections;
using Modding;

namespace FiveKnights
{
    public class Pogoable : MonoBehaviour
    {
        private static bool ParryFlag;

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (ParryFlag || col.gameObject.layer != 16) return;
            ParryFlag = true;
            GameObject slash = col.transform.parent.gameObject;
            float degrees = 0f;
            PlayMakerFSM damagesEnemy = PlayMakerFSM.FindFsmOnGameObject(slash, "damages_enemy");
            if (damagesEnemy == null) return;
            degrees = damagesEnemy.FsmVariables.FindFsmFloat("direction").Value;
            if (degrees < 45f)
            {
                HeroController.instance.RecoilLeft();
            }
            else if (degrees < 135f)
            {
                HeroController.instance.RecoilDown();
            }
            else if (degrees < 225f)
            {
                HeroController.instance.RecoilRight();
            }
            else
            {
                HeroController.instance.Bounce();
            }
            Log("Trying to end parry1");
            StartCoroutine(DoNextFrame());
        }

        IEnumerator DoNextFrame()
        {
            yield return null;
            Log("Time");
            yield return new WaitForSecondsRealtime(0.2f);
            Log("Time2");
            ParryFlag = false;
        }

        private void Log(object ob)
        {
            Modding.Logger.Log("[Parryable] " + ob);
        }
    }
}