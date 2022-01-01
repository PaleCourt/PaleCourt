using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiveKnights.Hegemol
{
    public class Carriage : MonoBehaviour
    {
        public float speed = 5f;
        public float distance = 90f;
        public float direction = 0f;
        private float startx = 0f;
        private float newStartx = 0f;
        private float xLimit = 402f;
        public float Zvalue = 0f;
        public float new_Zvalue = 0f;
        
        void Start()
        {
            Zvalue = gameObject.transform.position.z;
            direction = gameObject.transform.localScale.x * -1;
            startx = gameObject.transform.position.x;
            newStartx = Random.Range(startx, startx + distance * direction);
            gameObject.transform.position = new Vector3(newStartx, gameObject.transform.position.y,
                gameObject.transform.position.z);
        }

        // Update is called once per frame
        void Update()
        {
            List<float> list = new List<float>() { Zvalue, Zvalue + 0.5f, Zvalue + 1f };
            new_Zvalue = list[Random.Range(0, list.Count)];

            if (gameObject.transform.position.x * direction - startx * direction < distance 
                && gameObject.transform.position.x >= xLimit)
                gameObject.transform.position =
                    new Vector3(gameObject.transform.position.x + direction * speed * Time.deltaTime,
                        gameObject.transform.position.y, gameObject.transform.position.z);
            else
                gameObject.transform.position = new Vector3(startx, gameObject.transform.position.y, new_Zvalue);
        }
    }
}