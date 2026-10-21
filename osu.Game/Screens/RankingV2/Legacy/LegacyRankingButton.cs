// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Audio;
using osu.Game.Scoring;
using osu.Game.Skinning;

namespace osu.Game.Screens.RankingV2.Legacy
{
    public abstract partial class LegacyRankingButton : CompositeDrawable, ISerialisableDrawable
    {
        public Action? Action { get; set; }

        private readonly string[] spriteNames;

        private Sprite buttonSprite = null!;
        private PoolableSkinnableSample sample = null!;

        [Resolved]
        protected IBindable<IScoreInfo> Score { get; private set; } = null!;

        protected LegacyRankingButton(params string[] spriteNames)
        {
            this.spriteNames = spriteNames;
        }

        [BackgroundDependencyLoader]
        private void load(ISkinSource skin)
        {
            AutoSizeAxes = Axes.Both;
            InternalChildren =
            [
                buttonSprite = new Sprite(),
                sample = new PoolableSkinnableSample(new SampleInfo(@"menuhit")),
            ];

            foreach (string spriteName in spriteNames)
                buttonSprite.Texture ??= skin.GetTexture(spriteName);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            updateState();
            buttonSprite.FinishTransforms();
        }

        protected override bool OnHover(HoverEvent e)
        {
            updateState();
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            updateState();
            base.OnHoverLost(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            Action?.Invoke();
            sample.Play();
            return true;
        }

        private void updateState()
        {
            buttonSprite.FadeTo(IsHovered ? 1f : 0.7f, 200);
        }

        #region ISerialisableDrawable

        public bool UsesFixedAnchor { get; set; }

        // TODO: temporary to avoid `SkinDeserialisationTest` failures.
        // Remove when the argon skin supplies a default layout of results with this component included.
        public bool IsEditable => false;

        #endregion
    }

    public partial class LegacyRankingRetryButton : LegacyRankingButton
    {
        public LegacyRankingRetryButton()
            : base(@"ranking-retry", @"pause-retry")
        {
        }
    }

    public partial class LegacyRankingWatchReplayButton : LegacyRankingButton
    {
        public LegacyRankingWatchReplayButton()
            : base(@"ranking-replay", @"pause-replay")
        {
        }
    }
}
