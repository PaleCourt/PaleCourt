using UnityEngine;

namespace FiveKnights
{
    public class Pogoable : MonoBehaviour
    {
        public GameObject tar;

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.gameObject.layer != 16) return;
            
            GameObject slash = tar;
            if (col.transform.parent != null)
            {
                slash = col.transform.parent.gameObject;
            }
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
        }

        private void Log(object ob)
        {
            Modding.Logger.Log("[Parryable] " + ob);
        }
    }
}