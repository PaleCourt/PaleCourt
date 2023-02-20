using System.Collections;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace FiveKnights
{
    public class Plume : MonoBehaviour
    {
        // Used for raycast check
        private const float Extension = 0.01f;
        private const int CollisionMask = 1 << 8;

        private PlayMakerFSM _fsm;
        public bool upgraded;

        private void Awake()
        {
            transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);

            Destroy(GetComponent<DamageHero>());

            if (!IsGrounded()) Destroy(gameObject);

            _fsm = gameObject.LocateMyFSM("FSM");
            _fsm.GetAction<Wait>("Antic", 2).time.Value = 0.25f;
            _fsm.GetAction<Wait>("End", 0).time.Value = 0.5f;
            _fsm.InsertCoroutine("Plume 2", 0, AnimControl);
            _fsm.GetAction<FloatCompare>("Outside Arena?", 2).float2.Value = Mathf.Infinity;
            _fsm.GetAction<FloatCompare>("Outside Arena?", 3).float2.Value = -Mathf.Infinity;
        }

        private void Start()
		{
            DamageEnemies damageEnemies = gameObject.AddComponent<DamageEnemies>();
            damageEnemies.ignoreInvuln = false;
            damageEnemies.attackType = AttackTypes.Spell;
            damageEnemies.direction = 2;
            damageEnemies.damageDealt = upgraded ? 5 : 3;
        }

        private IEnumerator AnimControl()
        {
            yield return new WaitForSeconds(0.1f);
            gameObject.GetComponent<tk2dSpriteAnimator>().enabled = false;
            yield return new WaitForSeconds(PlayerData.instance.equippedCharm_19 ? 0.5f : 0.3f);
            gameObject.GetComponent<tk2dSpriteAnimator>().enabled = true;
        }

        private bool IsGrounded()
        {
            float rayLength = 1.0f + Extension;
            Vector2 pos = transform.position;
            return Physics2D.Raycast(pos + Vector2.up, Vector2.down, rayLength, CollisionMask);
        }

        private void Log(object message) => Modding.Logger.Log("[FiveKnights][Plume] " + message);
    }
}
