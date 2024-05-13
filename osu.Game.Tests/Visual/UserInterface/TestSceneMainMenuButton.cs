// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Metadata;
using osu.Game.Screens.Menu;
using osuTK.Input;
using Color4 = osuTK.Graphics.Color4;

namespace osu.Game.Tests.Visual.UserInterface
{
    [TestFixture]
    public partial class TestSceneMainMenuButton : OsuTestScene
    {
        [Resolved]
        private MetadataClient metadataClient { get; set; } = null!;

        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        [Test]
        public void TestStandardButton()
        {
            AddStep("add button", () => Child = new MainMenuIconButton(
                ButtonSystemStrings.Solo, @"button-default-select", OsuIcon.Player, new Color4(102, 68, 204, 255), () => { }, 0, Key.P)
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                ButtonSystemState = ButtonSystemState.TopLevel,
            });
        }

        [Test]
        public void TestBeatmapOfTheDayButton()
        {
            BeatmapLookupCache lookupCache = null!;
            AddStep("add lookup cache", () =>
            {
                Clear();
                Add(lookupCache = new BeatmapLookupCache());
            });
            AddStep("add button", () => Add(new DependencyProvidingContainer
            {
                CachedDependencies = [(typeof(BeatmapLookupCache), lookupCache)],
                RelativeSizeAxes = Axes.Both,
                Child = new BeatmapOfTheDayButton(
                    @"button-default-select", new Color4(102, 68, 204, 255), () => { }, 0, Key.D)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    ButtonSystemState = ButtonSystemState.TopLevel,
                }
            }));

            AddStep("set up API", () => dummyAPI.HandleRequest = req =>
            {
                switch (req)
                {
                    case GetBeatmapsRequest getBeatmapsRequest:
                        if (getBeatmapsRequest.BeatmapIds.Count != 1 || getBeatmapsRequest.BeatmapIds.SingleOrDefault() != 1001)
                            return false;

                        var beatmap = CreateAPIBeatmap();
                        beatmap.OnlineID = 1001;
                        getBeatmapsRequest.TriggerSuccess(new GetBeatmapsResponse
                        {
                            Beatmaps = new List<APIBeatmap> { beatmap }
                        });
                        return true;

                    default:
                        return false;
                }
            });

            AddStep("beatmap of the day active", () => metadataClient.BeatmapOfTheDayUpdated(new BeatmapOfTheDayInfo
            {
                RoomID = 1234,
                BeatmapID = 1001,
            }));

            AddStep("beatmap of the day not active", () => metadataClient.BeatmapOfTheDayUpdated(null));
        }
    }
}
