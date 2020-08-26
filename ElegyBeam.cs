using System.Collections;
using UnityEngine;

namespace FiveKnights
{
    public class ElegyBeam : MonoBehaviour
    {
        public GameObject parent;
        
        private tk2dSpriteAnimator _anim;
        private BoxCollider2D _collider;

        private void Awake()
        {
            GameObject go = gameObject;
            go.SetActive(true);
            go.layer = 22;
            go.transform.localScale = new Vector3(1, 0.5f, 1);
            _anim = GetComponent<tk2dSpriteAnimator>();
            _collider = GetComponent<BoxCollider2D>();
        }

        private void OnEnable()
        {
            StartCoroutine(Beam());
        }

        private IEnumerator Beam()
        {
            Vector2 beamPos = new Vector2(HeroController.instance.transform.position.x, 10);
            Quaternion randomRot = Quaternion.Euler(0, 0, Random.Range(60, 120));
            gameObject.transform.SetPositionAndRotation(beamPos, randomRot);
            
            _anim.Play("Beam Antic");
            _collider.enabled = false;
            yield return new WaitWhile(() => _anim.IsPlaying("Beam Antic"));
            _anim.Play("Beam");
            _collider.enabled = true;
            yield return new WaitForSeconds(0.25f);
            _anim.Play("Beam Off");
            _collider.enabled = false;
            yield return new WaitWhile(() => _anim.IsPlaying("Beam Off"));

            gameObject.SetActive(false);
            gameObject.transform.SetParent(parent.transform);
        }
    }
}