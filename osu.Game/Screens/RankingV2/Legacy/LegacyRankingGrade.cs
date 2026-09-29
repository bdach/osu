// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Scoring;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Screens.RankingV2.Legacy
{
    public partial class LegacyRankingGrade : CompositeDrawable, ISerialisableDrawable
    {
        private Sprite gradeSprite = null!;
        private Sprite gradeAdditiveSprite = null!;

        [Resolved]
        private IBindable<IScoreInfo> score { get; set; } = null!;

        [Resolved]
        private ISkinSource skin { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;

            InternalChildren =
            [
                gradeSprite = new Sprite
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
                gradeAdditiveSprite = new Sprite
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Blending = BlendingParameters.Additive,
                    Alpha = 0,
                },
            ];
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            score.BindValueChanged(_ => updateState(), true);
        }

        private void updateState()
        {
            gradeAdditiveSprite.Size = gradeSprite.Size = Vector2.Zero;
            gradeAdditiveSprite.Texture = gradeSprite.Texture = skin.GetTexture($@"ranking-{score.Value.Rank.ToString()}");
        }

        public void StartAnimating(double startTime)
        {
            gradeSprite.ScaleTo(new Vector2(2));
            gradeSprite.FadeOut();
            gradeAdditiveSprite.ScaleTo(Vector2.One);
            gradeAdditiveSprite.FadeOut();

            const double basic_transition_duration = 1000;
            const double flash_duration = 3400 - 1000;

            using (BeginAbsoluteSequence(startTime))
            {
                gradeSprite.ScaleTo(Vector2.One, basic_transition_duration, Easing.In);
                gradeSprite.FadeIn(basic_transition_duration, Easing.In);

                if (score.Value.Rank > ScoreRank.C)
                {
                    using (BeginDelayedSequence(basic_transition_duration))
                    {
                        gradeAdditiveSprite.ScaleTo(new Vector2(1.05f), flash_duration, Easing.Out);
                        gradeAdditiveSprite.FadeOutFromOne(flash_duration, Easing.Out);
                    }
                }
            }
        }

        public void FinishAnimating()
        {
            gradeSprite.FinishTransforms();
            gradeAdditiveSprite.FinishTransforms();
        }

        public bool UsesFixedAnchor { get; set; }
    }
}
