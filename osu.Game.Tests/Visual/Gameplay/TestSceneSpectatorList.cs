// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Utils;
using osu.Game.Screens.Play;
using osu.Game.Screens.Play.HUD;

namespace osu.Game.Tests.Visual.Gameplay
{
    [TestFixture]
    public partial class TestSceneSpectatorList : OsuTestScene
    {
        private readonly BindableList<SpectatorList.Spectator> spectators = new BindableList<SpectatorList.Spectator>();
        private readonly Bindable<LocalUserPlayingState> localUserPlayingState = new Bindable<LocalUserPlayingState>();

        private int counter;

        [Test]
        public void TestBasics()
        {
            AddStep("create spectator list", () => Child = new SpectatorList
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Spectators = { BindTarget = spectators },
                UserPlayingState = { BindTarget = localUserPlayingState }
            });

            AddStep("add a user", () =>
            {
                int id = Interlocked.Increment(ref counter);
                spectators.Add(new SpectatorList.Spectator(id, $"User {id}"));
            });
            AddStep("remove random user", () => spectators.RemoveAt(RNG.Next(0, spectators.Count)));
            AddStep("start playing", () => localUserPlayingState.Value = LocalUserPlayingState.Playing);
            AddStep("stop playing", () => localUserPlayingState.Value = LocalUserPlayingState.NotPlaying);
        }
    }
}
