// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;

namespace osu.Game.Screens.Select.Leaderboards
{
    public class OnlineLeaderboardScoreProvider : Component, ILeaderboardScoreProvider
    {
        public IBindableList<ScoreInfo> Scores => scores;
        private readonly BindableList<ScoreInfo> scores = new BindableList<ScoreInfo>();

        public IBindable<bool> Loading => loading;
        private readonly BindableBool loading = new BindableBool();

        public event Action<ScoreInfo[], ScoreInfo?>? Success;
        public event Action? Failure;

        public Bindable<BeatmapInfo> Beatmap { get; } = new Bindable<BeatmapInfo>();
        public Bindable<RulesetInfo> Ruleset { get; } = new Bindable<RulesetInfo>();
        public Bindable<bool> ModFilterActive { get; } = new BindableBool();
        public Bindable<IReadOnlyList<Mod>> Mods { get; } = new Bindable<IReadOnlyList<Mod>>();

        private readonly BeatmapLeaderboardScope scope;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private RulesetStore rulesets { get; set; } = null!;

        private GetScoresRequest? scoreRetrievalRequest;

        public OnlineLeaderboardScoreProvider(BeatmapLeaderboardScope scope)
        {
            this.scope = scope;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Beatmap.BindValueChanged(_ => Scheduler.AddOnce(RefetchScores));
            Ruleset.BindValueChanged(_ => Scheduler.AddOnce(RefetchScores));
            ModFilterActive.BindValueChanged(_ => Scheduler.AddOnce(RefetchScores));
            Mods.BindValueChanged(_ => Scheduler.AddOnce(RefetchScores));
            RefetchScores();
        }

        public void RefetchScores()
        {
            scoreRetrievalRequest?.Cancel();
            scoreRetrievalRequest = null;

            loading.Value = true;

            IReadOnlyList<Mod>? requestMods = null;

            if (ModFilterActive.Value && !Mods.Value.Any())
                // add nomod for the request
                requestMods = new Mod[] { new ModNoMod() };
            else if (ModFilterActive.Value)
                requestMods = Mods.Value;

            var fetchBeatmapInfo = Beatmap.Value;

            var newRequest = new GetScoresRequest(fetchBeatmapInfo, Ruleset.Value, scope, requestMods);
            newRequest.Success += response => Schedule(() =>
            {
                // Request may have changed since fetch request.
                // Can't rely on request cancellation due to Schedule inside SetScores so let's play it safe.
                if (!newRequest.Equals(scoreRetrievalRequest))
                    return;

                var newScores = response.Scores.Select(s => s.ToScoreInfo(rulesets, fetchBeatmapInfo)).OrderByTotalScore().ToArray();
                var userScore = response.UserScore?.CreateScoreInfo(rulesets, fetchBeatmapInfo);

                var allScores = newScores;
                if (userScore != null)
                    allScores = newScores.Append(userScore).ToArray();

                scores.Clear();
                scores.AddRange(allScores);
                Success?.Invoke(newScores, userScore);
            });
            newRequest.Failure += _ => Schedule(() =>
            {
                scores.Clear();
                Failure?.Invoke();
                loading.Value = false;
            });
            api.Queue(newRequest);

            scoreRetrievalRequest = newRequest;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            scoreRetrievalRequest?.Cancel();
        }
    }
}
