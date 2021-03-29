using UnityEngine;

namespace FiveKnights.Zemer
{
    public class WaveIncrease : MonoBehaviour
    {
        private void Update ()
        {
            transform.localScale += new Vector3(0.02f, 0.02f, 0f);

            if (!(transform.localScale.x > 5f)) return;
            
            Modding.Logger.Log("KILL");
            Destroy(gameObject);

        }
    }
}