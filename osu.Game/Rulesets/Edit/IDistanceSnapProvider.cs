// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Rulesets.Edit
{
    /// <summary>
    /// A snap provider which given a reference hit object and proposed distance from it, offers a more correct duration or distance value.
    /// </summary>
    [Cached]
    public interface IDistanceSnapProvider
    {
        /// <summary>
        /// A multiplier which changes the ratio of distance travelled per time unit.
        /// Importantly, this is provided for manual usage, and not multiplied into any of the methods exposed by this interface.
        /// </summary>
        /// <seealso cref="IBeatmap.DistanceSpacing"/>
        Bindable<double> DistanceSpacingMultiplier { get; }

        float GetBeatSnapDistance(IHasSliderVelocity? withVelocity = null);

        float DurationToDistance(double duration, double timingReference, IHasSliderVelocity? withVelocity = null);

        double DistanceToDuration(float distance, double timingReference, IHasSliderVelocity? withVelocity = null);

        double FindSnappedDuration(float distance, double snapReferenceTime, IHasSliderVelocity? withVelocity = null);

        float FindSnappedDistance(float distance, double snapReferenceTime, IHasSliderVelocity? withVelocity = null);
    }
}
