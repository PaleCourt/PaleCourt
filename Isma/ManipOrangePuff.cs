using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FiveKnights.Isma
{
    public class ManipOrangePuff : MonoBehaviour
    {
        // Alright so this is stupid I know but essentially with this lazy fix, the fool eaters won't break outside the
        // Isma arenas so it's a win-win.
        private void Update()
        {
            gameObject.SetActive(false);
        }
    }
}