// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Skinning;

namespace osu.Game.Screens.RankingV2.Legacy
{
    public partial class LegacyRankingBackgroundOverlay : CompositeDrawable, ISerialisableDrawable
    {
        private Sprite sprite = null!;

        [BackgroundDependencyLoader]
        private void load(ISkinSource skin)
        {
            AutoSizeAxes = Axes.Both;

            InternalChild = sprite = new Sprite
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Texture = skin.GetTexture(@"ranking-background-overlay"),
                Blending = BlendingParameters.Additive,
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            sprite.RotateTo(0).Then().RotateTo(360, 20000).Loop();
        }

        public bool UsesFixedAnchor { get; set; }
    }
}
