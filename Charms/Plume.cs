using System.Collections;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace FiveKnights
{
    public class Plume : MonoBehaviour
    {
        private PolygonCollider2D _collider;

        private PlayMakerFSM _fsm;

        private void Awake()
        {
            transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);

            Destroy(GetComponent<DamageHero>());

            _collider = GetComponent<PolygonCollider2D>();

            if (!IsGrounded()) Destroy(gameObject);

            DamageEnemies damageEnemies = gameObject.AddComponent<DamageEnemies>();
            damageEnemies.ignoreInvuln = false;
            damageEnemies.attackType = AttackTypes.NailBeam;
            damageEnemies.direction = 2;
            damageEnemies.damageDealt = PlayerData.instance.nailDamage;

            _fsm = gameObject.LocateMyFSM("FSM");
            _fsm.GetAction<Wait>("Antic", 2).time.Value = 0.25f;
            _fsm.GetAction<Wait>("End", 0).time.Value = 0.5f;
            _fsm.GetAction<FloatCompare>("Outside Arena?", 2).float2.Value = Mathf.Infinity;
            _fsm.GetAction<FloatCompare>("Outside Arena?", 3).float2.Value = -Mathf.Infinity;
        }

        private IEnumerator Start()
        {
            yield return null;
        }

        private const float Extension = 0.01f;
        private const int CollisionMask = 1 << 8;
        private bool IsGrounded()
        {
            float rayLength = 1.0f + Extension;
            Vector2 pos = transform.position;
#if DEBUG
            LineRenderer lineRend = gameObject.AddComponent<LineRenderer>();
            lineRend.startWidth = 0.1f;
            lineRend.endWidth = 0.1f;
            lineRend.SetPosition(0, pos + Vector2.up);
            lineRend.SetPosition(1, pos + Vector2.up + Vector2.down * rayLength);
#endif
            return Physics2D.Raycast(pos + Vector2.up, Vector2.down, rayLength, CollisionMask);
        }

        private void Log(object message) => Modding.Logger.Log("[Plume] " + message);
    }
}
