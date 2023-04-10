using System.Collections;
using UnityEngine;

namespace FiveKnights.Tiso
{
    public class TisoAttacks
    {
        private Transform transform;
        private Rigidbody2D _rb;
        private BoxCollider2D _bc;
        private Animator _anim;
        private GameObject _target;
        private GameObject _shield;

        private const float RunSpeed = 12f;
        private const float DodgeSpeed = 20f;
        private const int NumShots = 3;

        public TisoAttacks(Transform transform, Rigidbody2D rb, BoxCollider2D bc, Animator anim)
        {
            this.transform = transform;
            _rb = rb;
            _bc = bc;
            _anim = anim;
            _shield = transform.Find("Shields").Find("ShieldHalf").gameObject;
            _target = HeroController.instance.gameObject;
        }

        public IEnumerator Shoot()
        {
            float dir = FaceHero();
            yield return _anim.PlayToEnd("TisoShootAntic");
            for (int i = 0; i < NumShots; i++)
            {
                yield return _anim.PlayToEnd("TisoShoot");
                yield return new WaitForSeconds(0.1f);
            }

            yield return _anim.PlayToEnd("TisoShootEnd");
        }

        public IEnumerator ThrowShield()
        {
            float dir = FaceHero();
            yield return _anim.PlayToEnd("TisoThrow");

            GameObject shield = null;
            
            foreach (float i in new [] {0.3f, -0.3f})
            {
                shield = Object.Instantiate(_shield);
                shield.transform.position = _shield.transform.position;
                shield.transform.localScale *= 2f;
                shield.SetActive(true);
                Shield shieldCtrl = shield.AddComponent<Shield>();
                shieldCtrl.horizDir = dir;
                shieldCtrl.vertDir = i;
            }

            yield return new WaitWhile(() => dir > 0
                ? shield.transform.position.x < transform.position.x - 5f
                : shield.transform.position.x >= transform.position.x + 5f);

            yield return _anim.PlayToFrame("TisoThrowCatch", 4);
            Object.Destroy(shield);
            yield return _anim.PlayToEnd();
        }

        public IEnumerator Walk(float towardsX)
        {
            towardsX = Mathf.Max(towardsX, TisoController.LeftX);
            towardsX = Mathf.Min(towardsX, TisoController.RightX);
            float dir = FacePos(new Vector2(towardsX, 0f));
            _anim.speed = 2f;
            yield return _anim.PlayToEnd("TisoRunStart");
            _anim.Play("TisoRun");
            _rb.velocity = new Vector2(RunSpeed * -dir, 0f);
            yield return new WaitWhile(() =>
                dir < 0 ? transform.position.x < towardsX : transform.position.x > towardsX);
            yield return _anim.PlayToEnd("TisoRunEnd");
            _anim.speed = 1f;
            _rb.velocity = Vector2.zero;
        }

        public IEnumerator Dodge()
        {
            float dir = FaceHero();
            _rb.velocity = new Vector2(DodgeSpeed * dir, 0f);
            yield return _anim.PlayToEnd("TisoDodge");
            _rb.velocity = Vector2.zero;
        }

        public float FaceHero()
        {
            return FacePos(_target.transform.position);
        }

        // If pos is on left, returns positive
        private float FacePos(Vector2 pos)
        {
            float sign = Mathf.Sign(transform.position.x - pos.x);
            Vector2 oldScale = transform.localScale;
            transform.localScale = new Vector3(Mathf.Abs(oldScale.x) * sign, oldScale.y);
            return sign;
        }
    }
}