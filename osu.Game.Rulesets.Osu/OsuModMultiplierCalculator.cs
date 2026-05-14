// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;

namespace osu.Game.Rulesets.Osu
{
    public class OsuModMultiplierCalculator
    {
        private record ModCombination(Type[] ModTypes, Func<Mod[], double> CalculateMultiplier);

        private readonly List<ModCombination> modCombinations = [];

        public void Register<TMod>(Func<TMod, double> multiplier)
            where TMod : Mod
        {
            modCombinations.Add(new ModCombination([typeof(TMod)], mods => multiplier((TMod)mods[0])));
        }

        public void Register<T1, T2>(T1 first, T2 second, Func<T1, T2, double> multiplier)
            where T1 : Mod
            where T2 : Mod
        {
            modCombinations.Add(new ModCombination([typeof(T1), typeof(T2)], mods => multiplier((T1)mods[0], (T2)mods[1])));
        }

        public OsuModMultiplierCalculator()
        {
            #region Difficulty Reduction

            Register<OsuModEasy>(static _ => 0.5);
            Register<OsuModNoFail>(static _ => 0.5);
            Register<OsuModHalfTime>(static ht => GetMultiplierForRateAdjustment(ht.SpeedChange.Value));
            Register<OsuModDaycore>(static dc => GetMultiplierForRateAdjustment(dc.SpeedChange.Value));

            #endregion

            #region Difficulty Increase

            Register<OsuModHardRock>(static hr => hr.UsesDefaultConfiguration ? 1.06 : 1);
            // Sudden Death
            // Perfect
            Register<OsuModDoubleTime>(static dt => GetMultiplierForRateAdjustment(dt.SpeedChange.Value));
            Register<OsuModNightcore>(static nc => GetMultiplierForRateAdjustment(nc.SpeedChange.Value));
            Register<OsuModHidden>(static hd => hd.UsesDefaultConfiguration ? 1.06 : 1);
            // Traceable
            Register<OsuModFlashlight>(static fl => fl.UsesDefaultConfiguration ? 1.12 : 1);
            Register<OsuModBlinds>(static bl => bl.UsesDefaultConfiguration ? 1.12 : 1);
            // Strict Tracking
            // Accuracy Challenge

            #endregion

            #region Conversion

            Register<OsuModTargetPractice>(static _ => 0.1);
            Register<OsuModDifficultyAdjust>(static _ => 0.5);
            Register<OsuModClassic>(static _ => 0.96);
            // Random
            // Mirror
            // Alternate
            // Single Tap

            #endregion

            #region Automation

            // Autoplay
            // Cinema
            Register<OsuModRelax>(static _ => 0.1);
            Register<OsuModAutopilot>(static _ => 0.1);
            Register<OsuModSpunOut>(static _ => 0.9);

            #endregion

            #region Fun

            // Transform
            // Wiggle
            // Spin In
            // Grow
            // Deflate
            Register<ModWindUp>(static _ => 0.5);
            Register<ModWindDown>(static _ => 0.5);
            // Barrel Roll
            // Approach Different
            // Muted
            // No Scope
            Register<OsuModMagnetised>(static _ => 0.5);
            // Repel
            Register<ModAdaptiveSpeed>(static _ => 0.5);
            // Freeze Frame
            // Bubbles
            Register<OsuModSynesthesia>(static _ => 0.8);
            // Depth
            // Bloom

            #endregion

            #region System

            // Touch Device
            // Score V2

            #endregion
        }

        public double CalculateFor(IEnumerable<Mod> mods)
        {
            var allModsByType = mods.ToDictionary(m => m.GetType());
            var remainingModTypes = allModsByType.Keys.ToHashSet();

            double multiplier = 1;

            foreach (var combination in modCombinations)
            {
                if (remainingModTypes.IsSupersetOf(combination.ModTypes))
                {
                    var instances = combination.ModTypes.Select(t => allModsByType[t]).ToArray();
                    multiplier *= combination.CalculateMultiplier(instances);
                    remainingModTypes.ExceptWith(combination.ModTypes);
                }
            }

            return multiplier;
        }

        public static double GetMultiplierForRateAdjustment(double speedChange)
        {
            // Round to the nearest multiple of 0.1.
            double value = (int)(speedChange * 10) / 10.0;

            // Offset back to 0.
            value -= 1;

            if (speedChange >= 1)
                return 1 + value / 5;
            else
                return 0.6 + value;
        }
    }
}
