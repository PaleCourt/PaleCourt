using System.Collections;
using UnityEngine;

namespace FiveKnights
{
    public class ElegyBeam : MonoBehaviour
    {
        private const float AnimFPS = 0.125f;

        private Animator _anim;
        private BoxCollider2D _collider;
        private SpriteRenderer _sr;

        private void Awake()
        {
            GameObject go = gameObject;
            go.SetActive(true);
            go.layer = 22;
            go.transform.localScale = new Vector3(1, 0.5f, 1);
            _anim = GetComponent<Animator>();
            _collider = GetComponent<BoxCollider2D>();
            _sr = GetComponent<SpriteRenderer>();
            _sr.material = new Material(Shader.Find("Sprites/Default"));
        }

        private IEnumerator Start()
        {
            _anim.Play("Beam On");
            yield return new WaitForSeconds(4 * AnimFPS);
            DamageHero damageHero = gameObject.AddComponent<DamageHero>();
            damageHero.enabled = true;
            yield return new WaitForSeconds(0.25f);
            _anim.Play("Beam Off 1");
            yield return new WaitForSeconds(AnimFPS);
            _anim.Play("Beam Off 2");
            yield return new WaitForSeconds(AnimFPS);
            Destroy(gameObject);
        }
    }
}