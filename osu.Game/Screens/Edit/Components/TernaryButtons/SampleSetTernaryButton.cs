// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;

namespace osu.Game.Screens.Edit.Components.TernaryButtons
{
    public partial class SampleSetTernaryButton : DrawableTernaryButton
    {
        public EditorBeatmapSkin.SampleSet SampleSet { get; }

        public SampleSetTernaryButton(EditorBeatmapSkin.SampleSet sampleSet)
        {
            SampleSet = sampleSet;
            CreateIcon = () => sampleSet.SampleSetIndex == 0 ? new SpriteIcon { Icon = OsuIcon.SkinA } : new Circle();
        }
    }
}
