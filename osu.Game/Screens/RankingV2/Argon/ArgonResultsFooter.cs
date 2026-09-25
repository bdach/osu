// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Overlays.SkinEditor;
using osu.Game.Screens.Footer;
using osu.Game.Skinning;

namespace osu.Game.Screens.RankingV2.Argon
{
    public partial class ArgonResultsFooter : CompositeDrawable, ISerialisableDrawable
    {
        public bool CanBePlaced(SkinnableContainer skinnableContainer) => !skinnableContainer.Components.OfType<ArgonResultsFooter>().Any();
        public bool CanBeMoved => false;
        public bool CanBeRotated => false;
        public bool CanBeScaled => false;

        private LeasedBindable<ScreenFooterContent?>? leasedFooterContent;

        public ArgonResultsFooter()
        {
            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;
            RelativeSizeAxes = Axes.X;
            Height = ScreenFooterButton.HEIGHT;
        }

        [BackgroundDependencyLoader]
        private void load(ResultsScreenV2? results, SkinEditor? skinEditor, OsuColour colours)
        {
            if (skinEditor == null)
            {
                leasedFooterContent = results?.GlobalFooterContent.BeginLease(true);
                leasedFooterContent?.Value = new ScreenFooterContent(
                    BackButton: true,
                    // TODO: replace fontawesome, localise strings, actually hook up actions
                    LeftButtons: () =>
                    [
                        new ScreenFooterButton
                        {
                            Icon = FontAwesome.Solid.ChartBar,
                            Text = "Ranking",
                            Action = () => { },
                            AccentColour = colours.Green1, // to match web ranking pages
                        },
                        new ScreenFooterButton
                        {
                            Icon = FontAwesome.Solid.Search,
                            Text = "More statistics",
                            Action = () => { },
                            AccentColour = colours.Blue1,
                        },
                        new ScreenFooterButton
                        {
                            Icon = FontAwesome.Solid.Inbox,
                            Text = "Catalogue",
                            Action = () => { },
                            AccentColour = colours.Blue1, // to match beatmap pages
                        },
                    ]); // TODO: main buttons on the right
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            leasedFooterContent?.Return();

            base.Dispose(isDisposing);
        }

        public bool UsesFixedAnchor { get; set; } = true;
    }
}
