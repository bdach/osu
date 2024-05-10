// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;
using osu.Framework.Extensions.Color4Extensions;
using osu.Game.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// Button designed specifically for the osu!next main menu.
    /// In order to correctly flow, we have to use a negative margin on the parent container (due to the parallelogram shape).
    /// </summary>
    public partial class MainMenuButton : BeatSyncedContainer, IStateful<ButtonState>
    {
        public const float BOUNCE_COMPRESSION = 0.9f;
        public const float HOVER_SCALE = 1.2f;
        public const float BOUNCE_ROTATION = 8;
        public event Action<ButtonState>? StateChanged;

        public readonly Key[] TriggerKeys;

        protected override Container<Drawable> Content => content;
        private readonly Container content;

        private readonly Container box;
        private readonly Box boxHoverLayer;
        private readonly string sampleName;

        /// <summary>
        /// The menu state for which we are visible for (assuming only one).
        /// </summary>
        public ButtonSystemState VisibleState
        {
            set
            {
                VisibleStateMin = value;
                VisibleStateMax = value;
            }
        }

        public ButtonSystemState VisibleStateMin = ButtonSystemState.TopLevel;
        public ButtonSystemState VisibleStateMax = ButtonSystemState.TopLevel;

        private readonly Action? clickAction;
        private Sample? sampleClick;
        private Sample? sampleHover;
        private SampleChannel? sampleChannel;

        public override bool IsPresent => base.IsPresent
                                          // Allow keyboard interaction based on state rather than waiting for delayed animations.
                                          || state == ButtonState.Expanded;

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => box.ReceivePositionalInputAt(screenSpacePos);

        public MainMenuButton(LocalisableString text, string sampleName, Color4 colour, Action? clickAction = null, float extraWidth = 0, params Key[] triggerKeys)
        {
            this.sampleName = sampleName;
            this.clickAction = clickAction;
            TriggerKeys = triggerKeys;

            AutoSizeAxes = Axes.Both;
            Alpha = 0;

            Vector2 boxSize = new Vector2(ButtonSystem.BUTTON_WIDTH + Math.Abs(extraWidth), ButtonArea.BUTTON_AREA_HEIGHT);

            AddRangeInternal(new Drawable[]
            {
                box = new Container
                {
                    // box needs to be always present to ensure the button is always sized correctly for flow
                    AlwaysPresent = true,
                    Masking = true,
                    MaskingSmoothness = 2,
                    EdgeEffect = new EdgeEffectParameters
                    {
                        Type = EdgeEffectType.Shadow,
                        Colour = Color4.Black.Opacity(0.2f),
                        Roundness = 5,
                        Radius = 8,
                    },
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Scale = new Vector2(0, 1),
                    Size = boxSize,
                    Shear = new Vector2(ButtonSystem.WEDGE_WIDTH / boxSize.Y, 0),
                    Children = new[]
                    {
                        new Box
                        {
                            EdgeSmoothness = new Vector2(1.5f, 0),
                            RelativeSizeAxes = Axes.Both,
                            Colour = colour,
                        },
                        boxHoverLayer = new Box
                        {
                            EdgeSmoothness = new Vector2(1.5f, 0),
                            RelativeSizeAxes = Axes.Both,
                            Blending = BlendingParameters.Additive,
                            Colour = Color4.White,
                            Alpha = 0,
                        },
                    }
                },
                content = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Margin = new MarginPadding { Left = extraWidth / 2 },
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Shadow = true,
                            AllowMultiline = false,
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            Margin = new MarginPadding
                            {
                                Left = -3,
                                Bottom = 7,
                            },
                            Text = text
                        }
                    }
                }
            });
        }

        protected override bool OnHover(HoverEvent e)
        {
            if (State != ButtonState.Expanded) return true;

            sampleHover?.Play();
            box.ScaleTo(new Vector2(1.5f, 1), 500, Easing.OutElastic);

            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            if (State == ButtonState.Expanded)
                box.ScaleTo(new Vector2(1, 1), 500, Easing.OutElastic);
        }

        [BackgroundDependencyLoader]
        private void load(AudioManager audio)
        {
            sampleHover = audio.Samples.Get(@"Menu/button-hover");
            sampleClick = audio.Samples.Get(!string.IsNullOrEmpty(sampleName) ? $@"Menu/{sampleName}" : @"UI/button-select");
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            boxHoverLayer.FadeTo(0.1f, 1000, Easing.OutQuint);
            return base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            boxHoverLayer.FadeTo(0, 1000, Easing.OutQuint);
            base.OnMouseUp(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            trigger();
            return true;
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Repeat || e.ControlPressed || e.ShiftPressed || e.AltPressed || e.SuperPressed)
                return false;

            if (TriggerKeys.Contains(e.Key))
            {
                trigger();
                return true;
            }

            return false;
        }

        private void trigger()
        {
            sampleChannel = sampleClick?.GetChannel();
            sampleChannel?.Play();

            clickAction?.Invoke();

            boxHoverLayer.ClearTransforms();
            boxHoverLayer.Alpha = 0.9f;
            boxHoverLayer.FadeOut(800, Easing.OutExpo);
        }

        public override bool HandleNonPositionalInput => state == ButtonState.Expanded;
        public override bool HandlePositionalInput => state != ButtonState.Exploded && box.Scale.X >= 0.8f;

        public void StopSamplePlayback() => sampleChannel?.Stop();

        protected override void Update()
        {
            content.Alpha = Math.Clamp((box.Scale.X - 0.5f) / 0.3f, 0, 1);
            base.Update();
        }

        public int ContractStyle;

        private ButtonState state;

        public ButtonState State
        {
            get => state;

            set
            {
                if (state == value)
                    return;

                state = value;

                switch (state)
                {
                    case ButtonState.Contracted:
                        switch (ContractStyle)
                        {
                            default:
                                box.ScaleTo(new Vector2(0, 1), 500, Easing.OutExpo);
                                this.FadeOut(500);
                                break;

                            case 1:
                                box.ScaleTo(new Vector2(0, 1), 400, Easing.InSine);
                                this.FadeOut(800);
                                break;
                        }

                        break;

                    case ButtonState.Expanded:
                        const int expand_duration = 500;
                        box.ScaleTo(new Vector2(1, 1), expand_duration, Easing.OutExpo);
                        this.FadeIn(expand_duration / 6f);
                        break;

                    case ButtonState.Exploded:
                        const int explode_duration = 200;
                        box.ScaleTo(new Vector2(2, 1), explode_duration, Easing.OutExpo);
                        this.FadeOut(explode_duration / 4f * 3);
                        break;
                }

                StateChanged?.Invoke(State);
            }
        }

        public ButtonSystemState ButtonSystemState
        {
            set
            {
                ContractStyle = 0;

                switch (value)
                {
                    case ButtonSystemState.Initial:
                        State = ButtonState.Contracted;
                        break;

                    case ButtonSystemState.EnteringMode:
                        ContractStyle = 1;
                        State = ButtonState.Contracted;
                        break;

                    default:
                        if (value <= VisibleStateMax && value >= VisibleStateMin)
                            State = ButtonState.Expanded;
                        else if (value < VisibleStateMin)
                            State = ButtonState.Contracted;
                        else
                            State = ButtonState.Exploded;
                        break;
                }
            }
        }
    }

    public enum ButtonState
    {
        Contracted,
        Expanded,
        Exploded
    }
}
