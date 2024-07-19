// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Game.Beatmaps;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Edit
{
    public class SliderCompositionTool : HitObjectCompositionTool
    {
        public SliderCompositionTool()
            : base(nameof(Slider))
        {
            Hotkeys =
            [
                (new Hotkey(new KeyCombination(InputKey.MouseLeft)), "to place new point."),
                (new Hotkey(new KeyCombination(InputKey.MouseLeft)), "twice for new segment."),
                (new Hotkey(new KeyCombination(InputKey.S)), "for new segment."),
                (new Hotkey(new KeyCombination(InputKey.Tab), new KeyCombination(InputKey.Shift, InputKey.Tab), new KeyCombination(InputKey.Alt, InputKey.Number1, InputKey.Number2, InputKey.Number3, InputKey.Number4)), "to change current segment type."),
                (new Hotkey(new KeyCombination(InputKey.MouseRight)), "to finish."),
                (new Hotkey(new KeyCombination(InputKey.MouseLeft)), "and drag for drawing mode.")
            ];
        }

        public override Drawable CreateIcon() => new BeatmapStatisticIcon(BeatmapStatisticsIconType.Sliders);

        public override PlacementBlueprint CreatePlacementBlueprint() => new SliderPlacementBlueprint();
    }
}
