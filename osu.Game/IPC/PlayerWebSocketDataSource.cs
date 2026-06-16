// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text.Json.Serialization;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play;

namespace osu.Game.IPC
{
    public class PlayerWebSocketDataSource : WebSocketDataSource
    {
        private readonly GameplayState gameplayState;

        public PlayerWebSocketDataSource(IWebSocketProvider provider, GameplayState gameplayState)
            : base(provider)
        {
            this.gameplayState = gameplayState;

            gameplayState.LastJudgementResult.BindValueChanged(onJudgementResultChange);
        }

        private void onJudgementResultChange(ValueChangedEvent<JudgementResult> change)
        {
            var result = change.NewValue;
            var msg = new WebSocketJudgementMessage(result.Type, result.TimeOffset);
            BroadcastMessage(msg);
        }

        private record WebSocketJudgementMessage(
            [property: JsonPropertyName("result")]
            [property: JsonConverter(typeof(JsonStringEnumConverter<HitResult>))]
            [property: UsedImplicitly]
            HitResult HitResult,
            [property: JsonPropertyName("offset")]
            [property: UsedImplicitly]
            double TimeOffset);
    }
}
