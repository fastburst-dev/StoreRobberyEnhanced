using System;
using System.Collections.Generic;

namespace StoreRobberyEnhanced.Systems
{
    internal class SpeechManagerSystem
    {
        private readonly Random _rng = new Random();
        private readonly Dictionary<string, string[]> _speechPools;
        private readonly Dictionary<string, string> _lastUsed = new Dictionary<string, string>();

        public SpeechManagerSystem()
        {
            _speechPools = new Dictionary<string, string[]>
            {
                ["Idle"] = new[]
                {
                    "SHOP_GREET", "SHOP_GREET_REPEAT", "SHOP_GREET_NERVOUS",
                    "SHOP_GREET_MASKED", "SHOP_BROWSE", "SHOP_SUSPICIOUS",
                    "GENERIC_HI", "GENERIC_HOWS_IT_GOING", "GENERIC_WHATS_UP"
                },

                ["Threat"] = new[]
                {
                    "SHOP_CLERK_REACT", "SHOP_SCARED", "SHOP_SCARED_LOW",
                    "SHOP_SCARED_MED", "SHOP_SCARED_HIGH", "GENERIC_DONT_SHOOT",
                    "GENERIC_DONT_KILL_ME", "GENERIC_EASY", "GENERIC_EASY_NOW",
                    "GENERIC_BACK_OFF", "GENERIC_STAY_BACK", "GENERIC_FEAR",
                    "GENERIC_FEAR_MED", "GENERIC_FEAR_HIGH", "GENERIC_WHIMPERS"
                },

                ["Stall"] = new[]
                {
                    "SHOP_HURRY_UP", "SHOP_HURRY", "GENERIC_IM_HURRYING",
                    "GENERIC_OK_OK", "GENERIC_TAKE_MONEY", "GENERIC_FEAR_MED",
                    "GENERIC_WHIMPERS"
                },

                ["Register"] = new[]
                {
                    "GENERIC_OK_OK", "GENERIC_IM_HURRYING",
                    "GENERIC_HURRY_UP", "SHOP_TAKE_MONEY"
                },

                ["CashGrab"] = new[]
                {
                    "SHOP_TAKE_MONEY", "GENERIC_TAKE_MONEY",
                    "GENERIC_HURRY", "GENERIC_IM_HURRYING", "GENERIC_OK_OK"
                },

                ["BagToss"] = new[]
                {
                    "GENERIC_TAKE_IT", "GENERIC_TAKE_MONEY",
                    "SHOP_TAKE_MONEY", "GENERIC_OK_OK"
                },

                ["Surrender"] = new[]
                {
                    "GENERIC_HANDS_UP", "GENERIC_ARREST", "GENERIC_ARRESTED",
                    "GENERIC_FEAR", "GENERIC_FEAR_HIGH", "GENERIC_FEAR_MED",
                    "GENERIC_DONT_SHOOT", "GENERIC_DONT_KILL_ME",
                    "GENERIC_PLEASE", "GENERIC_EASY_NOW", "GENERIC_OK_OK"
                },

                ["SilentAlarm"] = new[]
                {
                    "GENERIC_SHOCKED_MED", "GENERIC_SHOCKED_HIGH",
                    "GENERIC_SCARED", "GENERIC_FRIGHTENED_HIGH",
                    "GENERIC_FRIGHTENED_MED"
                },

                ["Fight"] = new[]
                {
                    "GENERIC_INSULT_HIGH", "GENERIC_INSULT_MED",
                    "GENERIC_THREATEN", "GENERIC_COMBAT_SHOUT",
                    "GENERIC_CHALLENGE"
                }
            };
        }

        public string Get(string category)
        {
            if (!_speechPools.ContainsKey(category))
                return null;

            var pool = _speechPools[category];
            string chosen;

            do
            {
                chosen = pool[_rng.Next(pool.Length)];
            }
            while (_lastUsed.ContainsKey(category) && _lastUsed[category] == chosen);

            _lastUsed[category] = chosen;
            return chosen;
        }
    }
}
