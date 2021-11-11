using UnityEngine;

namespace FiveKnights
{
    public class SmallShot : MonoBehaviour
    {
        private int _damage = 20;

        private void OnTriggerEnter2D(Collider2D collider)
        {
            if (collider.gameObject.layer == 11)
            {
                HitInstance smallShotHit = new HitInstance();
                smallShotHit.DamageDealt = _damage;
                smallShotHit.AttackType = AttackTypes.NailBeam;
                smallShotHit.IgnoreInvulnerable = true;
                smallShotHit.Source = gameObject;
                smallShotHit.Multiplier = 1.0f;
                HealthManager hm = collider.gameObject.GetComponent<HealthManager>();
                hm.Hit(smallShotHit);
                Destroy(gameObject);
            }
        }

        private void Log(object message) => Modding.Logger.Log("[Small Shot] " + message);
    }
}
