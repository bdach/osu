// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
using ManagedBass.Fx;
using osu.Framework.Audio.Mixing;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Game.Audio.Effects
{
    public class AudioEffectManager : CompositeDrawable
    {
        private readonly AudioMixer mixer;
        private readonly Dictionary<AudioFilter, BQFParameters> parametersMap = new Dictionary<AudioFilter, BQFParameters>();

        public AudioEffectManager(AudioMixer mixer)
        {
            this.mixer = mixer;
        }

        public AudioFilter Get(BQFType type)
        {
            var filter = new AudioFilter(type);

            var parameters = new BQFParameters
            {
                lFilter = type,
                fCenter = filter.Cutoff.Value,
                fBandwidth = 0,
                fQ = 0.7f // This allows fCenter to go up to 22049hz (nyquist - 1hz) without overflowing and causing weird filter behaviour (see: https://www.un4seen.com/forum/?topic=19542.0)
            };

            AddInternal(filter);
            parametersMap[filter] = parameters;

            // Don't start attached if this is low-pass or high-pass filter (as they have special auto-attach/detach logic)
            if (type != BQFType.LowPass && type != BQFType.HighPass)
                attachFilter(filter);

            filter.Cutoff.BindValueChanged(cutoff => updateFilter(filter, cutoff));

            return filter;
        }

        protected override bool RemoveInternal(Drawable drawable)
        {
            if (!base.RemoveInternal(drawable))
                return false;

            var filter = (AudioFilter)drawable;
            filter.Cutoff.UnbindEvents();
            detachFilter(filter);
            parametersMap.Remove(filter);
            return true;
        }

        private void attachFilter(AudioFilter filter)
        {
            Debug.Assert(parametersMap.TryGetValue(filter, out var parameters) && !mixer.Effects.Contains(parameters));
            mixer.Effects.Add(parameters);
        }

        private void detachFilter(AudioFilter filter)
        {
            Debug.Assert(parametersMap.TryGetValue(filter, out var parameters));

            if (!mixer.Effects.Contains(parameters))
                return;

            mixer.Effects.Remove(parameters);
        }

        private void updateFilter(AudioFilter filter, ValueChangedEvent<int> cutoff)
        {
            // Workaround for weird behaviour when rapidly setting fCenter of a low-pass filter to nyquist - 1hz.
            if (filter.Type == BQFType.LowPass)
            {
                if (cutoff.NewValue >= AudioFilter.MAX_LOWPASS_CUTOFF)
                {
                    detachFilter(filter);
                    return;
                }

                if (cutoff.OldValue >= AudioFilter.MAX_LOWPASS_CUTOFF && cutoff.NewValue < AudioFilter.MAX_LOWPASS_CUTOFF)
                    attachFilter(filter);
            }

            // Workaround for weird behaviour when rapidly setting fCenter of a high-pass filter to 1hz.
            if (filter.Type == BQFType.HighPass)
            {
                if (cutoff.NewValue <= 1)
                {
                    detachFilter(filter);
                    return;
                }

                if (cutoff.OldValue <= 1 && cutoff.NewValue > 1)
                    attachFilter(filter);
            }

            bool found = parametersMap.TryGetValue(filter, out var parameters);
            if (!found) return;

            var filterIndex = mixer.Effects.IndexOf(parameters);
            if (filterIndex < 0) return;

            if (mixer.Effects[filterIndex] is BQFParameters existingFilter)
            {
                existingFilter.fCenter = cutoff.NewValue;

                // required to update effect with new parameters.
                mixer.Effects[filterIndex] = existingFilter;
            }
        }
    }
}
