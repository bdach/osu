// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass.Fx;
using osu.Framework.Bindables;
using osu.Framework.Graphics;

namespace osu.Game.Audio.Effects
{
    public class AudioFilter : Component, ITransformableFilter
    {
        /// <summary>
        /// The maximum cutoff frequency that can be used with a low-pass filter.
        /// This is equal to nyquist - 1hz.
        /// </summary>
        public const int MAX_LOWPASS_CUTOFF = 22049; // nyquist - 1hz

        public BQFType Type { get; }

        /// <summary>
        /// The current cutoff of this filter.
        /// </summary>
        public BindableNumber<int> Cutoff { get; }

        /// <summary>
        /// A Component that implements a BASS FX BiQuad Filter Effect.
        /// </summary>
        /// <param name="type">The type of filter (e.g. LowPass, HighPass, etc)</param>
        public AudioFilter(BQFType type = BQFType.LowPass)
        {
            Type = type;

            int initialCutoff;

            switch (type)
            {
                case BQFType.HighPass:
                    initialCutoff = 1;
                    break;

                case BQFType.LowPass:
                    initialCutoff = MAX_LOWPASS_CUTOFF;
                    break;

                default:
                    initialCutoff = 500; // A default that should ensure audio remains audible for other filters.
                    break;
            }

            Cutoff = new BindableNumber<int>(initialCutoff)
            {
                MinValue = 1,
                MaxValue = MAX_LOWPASS_CUTOFF
            };
        }
    }
}
