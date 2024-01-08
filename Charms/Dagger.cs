using UnityEngine;

namespace FiveKnights
{
    public class Dagger : MonoBehaviour
    {
        private const int DaggerDamage = 13;
        private const int DaggerDamageUpgraded = 25;
        public bool upgraded;

        private void Start()
		{
            DamageEnemies damageEnemies = gameObject.AddComponent<DamageEnemies>();
            damageEnemies.ignoreInvuln = true;
            damageEnemies.attackType = AttackTypes.Spell;
            damageEnemies.direction = 2;
            damageEnemies.damageDealt = upgraded ? DaggerDamageUpgraded : DaggerDamage;
        }

        private void OnTriggerEnter2D(Collider2D collider)
        {
            if(collider.gameObject.layer == 11)
            {
                HitInstance smallShotHit = new HitInstance();
                smallShotHit.DamageDealt = upgraded ? DaggerDamageUpgraded : DaggerDamage;
                smallShotHit.AttackType = AttackTypes.Spell;
                smallShotHit.IgnoreInvulnerable = true;
                smallShotHit.Source = gameObject;
                smallShotHit.Multiplier = 1f;
                HealthManager hm = collider.gameObject.GetComponent<HealthManager>();
                if(hm != null) hm.Hit(smallShotHit);
            }
        }

        private void Log(object message) => Modding.Logger.Log("[Dagger] " + message);
    }
}