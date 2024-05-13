// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Database;
using osu.Game.Online.Metadata;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Screens.Menu
{
    public partial class BeatmapOfTheDayButton : MainMenuButton
    {
        private readonly UpdateableOnlineBeatmapSetCover cover;
        private IBindable<BeatmapOfTheDayInfo?> info = null!;

        [Resolved]
        private BeatmapLookupCache beatmapLookupCache { get; set; } = null!;

        public BeatmapOfTheDayButton(string sampleName, Color4 colour, Action? clickAction = null, params Key[] triggerKeys)
            : base("beatmap of the day", sampleName, colour, clickAction, triggerKeys)
        {
            Title.Margin = new MarginPadding { Left = -12, Bottom = 7 };
            BaseSize = new Vector2(ButtonSystem.BUTTON_WIDTH * 1.3f, ButtonArea.BUTTON_AREA_HEIGHT);

            Background.Add(cover = new UpdateableOnlineBeatmapSetCover
            {
                RelativeSizeAxes = Axes.Y,
                Margin = new MarginPadding { Left = -ButtonSystem.WEDGE_WIDTH },
                Colour = ColourInfo.GradientVertical(Colour4.White, Colour4.White.Opacity(0)),
            });
        }

        [BackgroundDependencyLoader]
        private void load(MetadataClient metadataClient)
        {
            info = metadataClient.BeatmapOfTheDayInfo.GetBoundCopy();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            cover.Shear = -Background.Shear;

            info.BindValueChanged(updateDisplay, true);
            FinishTransforms(true);
        }

        protected override void Update()
        {
            base.Update();

            cover.Width = Background.Width + ButtonSystem.WEDGE_WIDTH;
        }

        private void updateDisplay(ValueChangedEvent<BeatmapOfTheDayInfo?> info)
        {
            UpdateState();

            if (info.NewValue == null)
            {
                cover.OnlineInfo = null;
            }
            else
            {
                beatmapLookupCache.GetBeatmapAsync(info.NewValue.Value.BeatmapID)
                                  .ContinueWith(t =>
                                  {
                                      if (t.GetResultSafely()?.BeatmapSet is IBeatmapSetOnlineInfo onlineInfo)
                                          Schedule(() => cover.OnlineInfo = onlineInfo);
                                  });
            }
        }

        protected override void UpdateState()
        {
            if (info.IsNotNull() && info.Value == null)
            {
                ContractStyle = 0;
                State = ButtonState.Contracted;
                return;
            }

            base.UpdateState();
        }
    }
}
