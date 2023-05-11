using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FiveKnights.Tiso
{
    public static class TisoRandAudio
    {
        private const string BaseName = "AudTiso";

        private static Dictionary<string, int> _counter = new()
        {
            ["AudTiso2"] = 0,
            ["AudTiso3"] = 0,
            ["AudTiso4"] = 0,
            ["AudTiso5"] = 0,
            ["AudTiso6"] = 0,
        };

        public static string PickRandomTisoAud(int min, int max)
        {
            int[] values = { 2, 3, 4, 5, 6 };
            List<string> choices = values.Where(x => x >= min && x <= max).Select(x => BaseName + x).ToList();
            string choice = choices[Random.Range(0, choices.Count)];
                
            while (choices.Count > 1 && _counter[choice] > 0)
            {
                choices.Remove(choice);
                choice = choices[Random.Range(0, choices.Count)];
            }

            if (choices.Count <= 1)
            {
                _counter = _counter.ToDictionary(kvp => kvp.Key, _ => 0);
                choice = choices[Random.Range(0, choices.Count)];
            }

            _counter[choice]++;

            return choice;
        }
    }
}