// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Beatmaps.Drawables.Cards;
using osu.Game.Graphics;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Metadata;
using osu.Game.Online.Rooms;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Screens.Menu
{
    public partial class DailyChallengeButton : MainMenuButton, IHasCustomTooltip<APIBeatmapSet?>
    {
        public Room? Room { get; private set; }

        private readonly UpdateableOnlineBeatmapSetCover cover;
        private IBindable<DailyChallengeInfo?> info = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        public DailyChallengeButton(string sampleName, Color4 colour, Action<MainMenuButton>? clickAction = null, params Key[] triggerKeys)
            : base(ButtonSystemStrings.DailyChallenge, sampleName, OsuIcon.DailyChallenge, colour, clickAction, triggerKeys)
        {
            BaseSize = new Vector2(ButtonSystem.BUTTON_WIDTH * 1.3f, ButtonArea.BUTTON_AREA_HEIGHT);

            Background.Add(cover = new UpdateableOnlineBeatmapSetCover
            {
                RelativeSizeAxes = Axes.Y,
                Colour = ColourInfo.GradientVertical(Colour4.White, Colour4.White.Opacity(0)),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativePositionAxes = Axes.X,
                X = -0.5f,
            });
        }

        [BackgroundDependencyLoader]
        private void load(MetadataClient metadataClient)
        {
            info = metadataClient.DailyChallengeInfo.GetBoundCopy();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            info.BindValueChanged(updateDisplay, true);
            FinishTransforms(true);

            cover.Shear = -Background.Shear;

            cover.MoveToX(-0.5f, 10000, Easing.InOutSine)
                 .Then().MoveToX(0.5f, 10000, Easing.InOutSine)
                 .Loop();
        }

        protected override void Update()
        {
            base.Update();

            cover.Width = 2 * Background.Width + ButtonSystem.WEDGE_WIDTH;
        }

        private void updateDisplay(ValueChangedEvent<DailyChallengeInfo?> info)
        {
            UpdateState();

            if (info.NewValue == null)
            {
                Room = null;
                cover.OnlineInfo = TooltipContent = null;
            }
            else
            {
                var roomRequest = new GetRoomRequest(info.NewValue.Value.RoomID);

                roomRequest.Success += room =>
                {
                    Room = room;
                    cover.OnlineInfo = TooltipContent = room.Playlist.FirstOrDefault()?.Beatmap.BeatmapSet as APIBeatmapSet;
                };
                api.Queue(roomRequest);
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

        public ITooltip<APIBeatmapSet?> GetCustomTooltip() => new DailyChallengeTooltip();

        public APIBeatmapSet? TooltipContent { get; private set; }

        internal partial class DailyChallengeTooltip : CompositeDrawable, ITooltip<APIBeatmapSet?>
        {
            [Cached]
            private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Purple);

            private APIBeatmapSet? lastContent;

            [BackgroundDependencyLoader]
            private void load()
            {
                AutoSizeAxes = Axes.Both;
            }

            public void Move(Vector2 pos) => Position = pos;

            public void SetContent(APIBeatmapSet? content)
            {
                if (content == lastContent)
                    return;

                lastContent = content;

                ClearInternal();
                if (content != null)
                    AddInternal(new BeatmapCardNano(content));
            }
        }
    }
}
