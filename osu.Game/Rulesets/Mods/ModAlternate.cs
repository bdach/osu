// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Logging;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osu.Game.Scoring;
using osu.Game.Screens.Play;

namespace osu.Game.Rulesets.Mods
{
    public class ModAlternate : Mod
    {
        public override string Name => "Alternate";
        public override string Acronym => "AL";
        public override ModType Type => ModType.DifficultyIncrease;
        public override IconUsage? Icon => FontAwesome.Solid.Random;
        public override double ScoreMultiplier => 1;
        public override string Description => "Never use the same key twice!";
        public override Type[] IncompatibleMods => new[] { typeof(ModAutoplay) };
    }

    public abstract class ModAlternate<THitObject, TAction> : ModAlternate, IApplicableToPlayer, IApplicableToDrawableRuleset<THitObject>, IApplicableToScoreProcessor
        where THitObject : HitObject
        where TAction : struct
    {
        protected readonly Stack<TAction> ActionHistory = new Stack<TAction>();
        private readonly Bindable<bool> isBreakTime = new BindableBool();
        private InputInterceptor interceptor;

        private List<THitObject> hitObjects;
        private int lastHitObjectIndex;
        private bool anyObjectHit;

        protected virtual void OnNewJudgement(JudgementResult result)
        {
            anyObjectHit |= result.IsHit && result.Type.IsScorable();

            if (!result.IsHit)
                ActionHistory.Clear();
        }

        protected virtual bool ShouldBlock(TAction action)
        {
            if (EqualityComparer<TAction?>.Default.Equals(action, LastAction))
                return true;

            ActionHistory.Push(action);
            return false;
        }

        protected TAction? LastAction => ActionHistory.Count > 0 ? ActionHistory.Peek() : (TAction?)null;

        [CanBeNull]
        protected THitObject CurrentHitObject
        {
            get
            {
                double currentTime = interceptor.Time.Current;

                while (lastHitObjectIndex < hitObjects.Count)
                {
                    var lastHitObject = hitObjects[lastHitObjectIndex];

                    double objectMissWindow = lastHitObject.HitWindows.WindowFor(HitResult.Miss);
                    double nestedObjectMissWindow = lastHitObject.NestedHitObjects.Count > 0
                        ? lastHitObject.NestedHitObjects.Max(hitObject => hitObject.HitWindows.WindowFor(HitResult.Miss))
                        : 0;

                    double pessimisticEndTime = lastHitObject.GetEndTime() + Math.Max(objectMissWindow, nestedObjectMissWindow);
                    if (currentTime <= pessimisticEndTime)
                        break;

                    lastHitObjectIndex += 1;
                }

                return lastHitObjectIndex < hitObjects.Count ? hitObjects[lastHitObjectIndex] : null;
            }
        }

        #region IApplicableToPlayer

        public void ApplyToPlayer(Player player)
        {
            isBreakTime.BindTo((BindableBool)player.IsBreakTime);
            isBreakTime.BindValueChanged(onBreakTimeChanged);
        }

        private void onBreakTimeChanged(ValueChangedEvent<bool> isBreakTime)
        {
            if (!isBreakTime.NewValue)
                return;

            ActionHistory.Clear();
            anyObjectHit = false; // make sure the alternating doesn't start until the first hit after break.
        }

        #endregion

        #region IApplicableToDrawableRuleset

        public void ApplyToDrawableRuleset(DrawableRuleset<THitObject> drawableRuleset)
        {
            drawableRuleset.KeyBindingInputManager.Add(interceptor = new InputInterceptor(this));
            hitObjects = drawableRuleset.Beatmap.HitObjects;
        }

        #endregion

        #region IApplicableToScoreProcessor

        public void ApplyToScoreProcessor(ScoreProcessor scoreProcessor)
        {
            scoreProcessor.NewJudgement += OnNewJudgement;
        }

        public ScoreRank AdjustRank(ScoreRank rank, double accuracy) => rank;

        #endregion

        public class InputInterceptor : Drawable, IKeyBindingHandler<TAction>
        {
            private readonly ModAlternate<THitObject, TAction> mod;

            public InputInterceptor(ModAlternate<THitObject, TAction> mod)
            {
                this.mod = mod;
            }

            public bool OnPressed(TAction action)
            {
                if (mod.isBreakTime.Value)
                    return false;

                // beware: mod.ShouldBlock() should always execute first, to make sure it can see the first object after a break
                var result = mod.ShouldBlock(action) && mod.anyObjectHit;
                Logger.Log($"{Time.Current} -> {result}");
                return result;
            }

            public void OnReleased(TAction action)
            {
            }
        }
    }
}
