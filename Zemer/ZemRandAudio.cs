using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FiveKnights.Zemer
{
    public static class ZemRandAudio
    {
        private const string BaseName = "ZAudAtt";

        private static Dictionary<string, int> _counter = new()
        {
            ["ZAudAtt1"] = 0,
            ["ZAudAtt2"] = 0,
            ["ZAudAtt3"] = 0,
            ["ZAudAtt4"] = 0,
            ["ZAudAtt5"] = 0,
            ["ZAudAtt6"] = 0,
        };

        public static string PickRandomZemAud(int min, int max)
        {
            int[] values = { 1, 2, 3, 4, 5, 6 };
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