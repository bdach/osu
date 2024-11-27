// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using osu.Framework.Bindables;
using osu.Framework.Localisation;
using osu.Game.Localisation;

namespace osu.Game.Screens.Edit.Submission
{
    public class BeatmapSubmissionSettings
    {
        public Bindable<BeatmapSubmissionTarget> Target { get; } = new Bindable<BeatmapSubmissionTarget>();
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum BeatmapSubmissionTarget
    {
        [LocalisableDescription(typeof(BeatmapSubmissionStrings), nameof(BeatmapSubmissionStrings.BeatmapSubmissionTargetWIP))]
        WIP,

        [LocalisableDescription(typeof(BeatmapSubmissionStrings), nameof(BeatmapSubmissionStrings.BeatmapSubmissionTargetPending))]
        Pending,
    }
}
