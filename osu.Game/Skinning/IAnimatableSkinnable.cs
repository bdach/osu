// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Testing;

namespace osu.Game.Skinning
{
    public interface IAnimatableSkinnable
    {
        int GroupNumber { get; }

        double StartAnimating(double startTime);

        void FinishAnimating();
    }

    public static class AnimatableSkinnableExtensions
    {
        public static void StartAnimationSequence(this SkinnableContainer parent)
        {
            parent.FinishAnimationSequence();

            double latestTransformEndTime = parent.LatestTransformEndTime;

            var groups = parent.Components.OfType<IAnimatableSkinnable>()
                               .GroupBy(anim => anim.GroupNumber);

            foreach (var group in groups.OrderBy(g => g.Key))
            {
                double groupStartTime = latestTransformEndTime;
                double groupEndTime = latestTransformEndTime;

                foreach (var animatable in group)
                {
                    double animatableEndTime = animatable.StartAnimating(groupStartTime);
                    groupEndTime = Math.Max(groupEndTime, animatableEndTime);
                }

                latestTransformEndTime = groupEndTime;
            }
        }

        public static void FinishAnimationSequence(this SkinnableContainer parent)
        {
            foreach (var animatable in parent.ChildrenOfType<IAnimatableSkinnable>())
                animatable.FinishAnimating();
        }
    }
}
