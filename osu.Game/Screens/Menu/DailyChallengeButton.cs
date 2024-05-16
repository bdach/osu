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
using osu.Framework.Graphics.Shapes;
using osu.Framework.Threading;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Beatmaps.Drawables.Cards;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
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

        private readonly OsuSpriteText countdown;
        private ScheduledDelegate? scheduledCountdownUpdate;

        private UpdateableOnlineBeatmapSetCover cover = null!;
        private IBindable<DailyChallengeInfo?> info = null!;
        private BufferedContainer background = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        public DailyChallengeButton(string sampleName, Color4 colour, Action<MainMenuButton>? clickAction = null, params Key[] triggerKeys)
            : base(ButtonSystemStrings.DailyChallenge, sampleName, OsuIcon.DailyChallenge, colour, clickAction, triggerKeys)
        {
            BaseSize = new Vector2(ButtonSystem.BUTTON_WIDTH * 1.3f, ButtonArea.BUTTON_AREA_HEIGHT);

            Content.Add(countdown = new OsuSpriteText
            {
                Shadow = true,
                AllowMultiline = false,
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                Margin = new MarginPadding
                {
                    Left = -3,
                    Bottom = 22,
                },
                Font = OsuFont.Default.With(size: 12),
                Alpha = 0,
            });
        }

        protected override Drawable CreateBackground(Colour4 accentColour) => background = new BufferedContainer
        {
            Children = new Drawable[]
            {
                cover = new UpdateableOnlineBeatmapSetCover
                {
                    RelativeSizeAxes = Axes.Y,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativePositionAxes = Axes.X,
                    X = -0.5f,
                },
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = ColourInfo.GradientVertical(accentColour.Opacity(0), accentColour),
                    Blending = BlendingParameters.Additive,
                },
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = accentColour.Opacity(0.7f)
                },
            },
        };

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

            cover.MoveToX(-0.5f, 10000, Easing.InOutSine)
                 .Then().MoveToX(0.5f, 10000, Easing.InOutSine)
                 .Loop();
        }

        protected override void Update()
        {
            base.Update();

            cover.Width = 2 * background.DrawWidth;
        }

        private void updateDisplay(ValueChangedEvent<DailyChallengeInfo?> info)
        {
            UpdateState();

            scheduledCountdownUpdate?.Cancel();
            scheduledCountdownUpdate = null;

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

                    updateCountdown();
                    Scheduler.AddDelayed(updateCountdown, 1000, true);
                };
                api.Queue(roomRequest);
            }
        }

        private void updateCountdown()
        {
            if (Room == null)
                return;

            var remaining = (Room.EndDate.Value - DateTimeOffset.Now) ?? TimeSpan.Zero;

            if (remaining <= TimeSpan.Zero)
            {
                countdown.FadeOut(250, Easing.OutQuint);
            }
            else
            {
                if (countdown.Alpha == 0)
                    countdown.FadeIn(250, Easing.OutQuint);

                countdown.Text = remaining.ToString(@"hh\:mm\:ss");
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
