// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.ComponentModel;
using System.Diagnostics;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Logging;
using osu.Game.Configuration;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Mods
{
    public class TaikoModAlternate : ModAlternate<TaikoHitObject, TaikoAction>
    {
        [SettingSource("Playstyle", "Preferred alternate playstyle")]
        public Bindable<Playstyle> Style { get; } = new Bindable<Playstyle>();

        [SettingSource("Allow any key after drum roll")]
        public BindableBool ResetAfterDrumRoll { get; } = new BindableBool();

        [CanBeNull]
        private Hit pendingStrongHit;

        protected override bool ShouldBlock(TaikoAction action)
        {
            bool shouldBlock = false;

            switch (CurrentHitObject)
            {
                case Hit hit:
                    shouldBlock = hit.IsStrong ? shouldBlockStrongHit(action) : shouldBlockInCurrentPlaystyle(action);
                    break;

                case Swell _:
                    // swells can be mashed through.
                    break;

                case DrumRoll _:
                    shouldBlock = action == LastAction;
                    break;
            }

            Logger.Log($"prev: {LastAction}, curr: {action}, block: {shouldBlock}, cho: {CurrentHitObject?.StartTime}");
            ActionHistory.Push(action);
            return shouldBlock;
        }

        private bool shouldBlockInCurrentPlaystyle(TaikoAction action)
        {
            switch (Style.Value)
            {
                case Playstyle.AlternateFingers:
                    return action == LastAction;

                case Playstyle.AlternateHands:
                    return handForAction(action) == handForAction(LastAction);

                default:
                    throw new NotSupportedException();
            }
        }

        private Hand? handForAction(TaikoAction? action)
        {
            switch (action)
            {
                case TaikoAction.LeftCentre:
                case TaikoAction.LeftRim:
                    return Hand.Left;

                case TaikoAction.RightCentre:
                case TaikoAction.RightRim:
                    return Hand.Right;

                case null:
                    return null;

                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        private bool shouldBlockStrongHit(TaikoAction action)
        {
            // make sure we've seen a strong hit before and it's the same one we're about to consider.
            if (pendingStrongHit == null || pendingStrongHit != CurrentHitObject)
                return shouldBlockInCurrentPlaystyle(action);

            Debug.Assert(LastAction.HasValue);
            return action != secondStrongHitActionFor(LastAction.Value);
        }

        private TaikoAction secondStrongHitActionFor(TaikoAction action)
        {
            switch (action)
            {
                case TaikoAction.LeftCentre:
                    return TaikoAction.LeftRim;

                case TaikoAction.LeftRim:
                    return TaikoAction.LeftCentre;

                case TaikoAction.RightCentre:
                    return TaikoAction.RightRim;

                case TaikoAction.RightRim:
                    return TaikoAction.RightCentre;

                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        protected override void OnNewJudgement(JudgementResult result)
        {
            base.OnNewJudgement(result);

            switch (result.HitObject)
            {
                case StrongHitObject _:
                    // a strong judgement marks the definite end of a strong hit.
                    pendingStrongHit = null;

                    if (result.IsHit)
                        // allow restart with any button after a successful strong hit.
                        ActionHistory.Clear();

                    break;

                case Hit hit:
                    if (hit.IsStrong && result.IsHit)
                        pendingStrongHit = hit;

                    break;

                case Swell _:
                    ActionHistory.Clear();
                    break;

                case DrumRoll _:
                    if (ResetAfterDrumRoll.Value)
                        ActionHistory.Clear();
                    break;
            }
        }

        public enum Playstyle
        {
            /// <summary>
            /// Each hand has a rim and a centre button, so alternating is done by changing hands.
            /// Also known in community vernacular as "kddk".
            /// </summary>
            [Description(@"Alternate hands (""kddk"")")]
            AlternateHands,

            /// <summary>
            /// One hand has both rim buttons and the other both centre buttons.
            /// Alternating is done using fingers only.
            /// Also known in community vernacular as "kkdd".
            /// </summary>
            [Description(@"Alternate fingers (""kkdd"")")]
            AlternateFingers
        }

        private enum Hand
        {
            Left,
            Right
        }
    }
}
