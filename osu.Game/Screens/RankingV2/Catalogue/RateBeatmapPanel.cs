// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Screens.RankingV2.Statistics;
using osuTK;

namespace osu.Game.Screens.RankingV2.Catalogue
{
    public partial class RateBeatmapPanel : StatisticItemPanel
    {
        private RatingToggleButton likeButton = null!;
        private RatingToggleButton dislikeButton = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        public RateBeatmapPanel()
            : base("Rate the beatmap")
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(5),
                Children =
                [
                    likeButton = new RatingToggleButton
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = "Like",
                        Icon = FontAwesome.Solid.ThumbsUp,
                        ActiveLighterColour = colours.Lime1,
                        ActiveDarkerColour = colours.Lime3,
                        Width = 120,
                        Height = 40,
                    },
                    dislikeButton = new RatingToggleButton
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = "Dislike",
                        Icon = FontAwesome.Solid.ThumbsDown,
                        ActiveLighterColour = colours.Orange1,
                        ActiveDarkerColour = colours.Orange3,
                        Width = 120,
                        Height = 40,
                    },
                    new RatingToggleButton
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = "Favourite",
                        Icon = OsuIcon.Heart,
                        ActiveLighterColour = colours.Pink1,
                        ActiveDarkerColour = colours.Pink3,
                        Width = 140,
                        Height = 40,
                    },
                ]
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            likeButton.Active.BindValueChanged(_ => likeToggled());
            dislikeButton.Active.BindValueChanged(_ => dislikeToggled());
        }

        private bool adjustingStates;

        private void likeToggled()
        {
            if (adjustingStates)
                return;

            adjustingStates = true;

            if (dislikeButton.Active.Value)
                dislikeButton.Active.Value = false;

            // either vote 10 if like active, or remove vote if like inactive (is that even possible?)

            adjustingStates = false;
        }

        private void dislikeToggled()
        {
            if (adjustingStates)
                return;

            adjustingStates = true;

            if (likeButton.Active.Value)
                likeButton.Active.Value = false;

            // either vote 1 if dislike active, or remove vote if dislike inactive (is that even possible?)

            adjustingStates = false;
        }

        public partial class RatingToggleButton : ShearedButtonWithIcon
        {
            private Sample? sampleOff;
            private Sample? sampleOn;

            /// <summary>
            /// Whether this button is currently toggled to an active state.
            /// </summary>
            public BindableBool Active { get; } = new BindableBool();

            [BackgroundDependencyLoader]
            private void load(AudioManager audio)
            {
                sampleOn = audio.Samples.Get(@"UI/check-on");
                sampleOff = audio.Samples.Get(@"UI/check-off");
            }

            public required Colour4 ActiveDarkerColour { get; init; }
            public required Colour4 ActiveLighterColour { get; init; }

            protected override HoverSounds CreateHoverSounds(HoverSampleSet sampleSet) => new HoverSounds(sampleSet);

            protected override void LoadComplete()
            {
                Active.BindDisabledChanged(disabled => Action = disabled ? null : Active.Toggle, true);
                Active.BindValueChanged(_ =>
                {
                    UpdateActiveState();
                    playSample();
                });

                UpdateActiveState();
                base.LoadComplete();
            }

            protected virtual void UpdateActiveState()
            {
                DarkerColour = Active.Value ? ActiveDarkerColour : ColourProvider.Background3;
                LighterColour = Active.Value ? ActiveLighterColour : ColourProvider.Background1;
                TextColour = Active.Value ? ColourProvider.Background6 : ColourProvider.Content1;
            }

            private void playSample()
            {
                if (Active.Value)
                    sampleOn?.Play();
                else
                    sampleOff?.Play();
            }
        }
    }
}
