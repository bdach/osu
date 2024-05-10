// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Online.API.Requests.Responses;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Screens.Menu
{
    public partial class BeatmapOfTheDayButton : MainMenuButton
    {
        private APIBeatmap? beatmap;

        public APIBeatmap? Beatmap
        {
            get => beatmap;
            set
            {
                beatmap = value;
                cover.OnlineInfo = beatmap?.BeatmapSet;
            }
        }

        private readonly UpdateableOnlineBeatmapSetCover cover;

        public BeatmapOfTheDayButton(string sampleName, Color4 colour, Action? clickAction = null, float extraWidth = 0, params Key[] triggerKeys)
            : base("beatmap of\nthe day", sampleName, colour, clickAction, extraWidth, triggerKeys)
        {
            Title.Margin = new MarginPadding { Left = -12, Bottom = 7 };

            Background.Add(cover = new UpdateableOnlineBeatmapSetCover
            {
                RelativeSizeAxes = Axes.Y,
                Margin = new MarginPadding { Left = -ButtonSystem.WEDGE_WIDTH },
                Colour = ColourInfo.GradientVertical(Colour4.White, Colour4.White.Opacity(0)),
                Shear = -Background.Shear,
            });
        }

        protected override void Update()
        {
            base.Update();

            cover.Width = Background.Width + ButtonSystem.WEDGE_WIDTH;
        }
    }
}
