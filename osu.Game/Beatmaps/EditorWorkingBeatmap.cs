// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Framework.Logging;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Skinning;

namespace osu.Game.Beatmaps
{
    public class EditorWorkingBeatmap : WorkingBeatmap
    {
        private readonly WorkingBeatmap workingBeatmap;

        public EditorWorkingBeatmap(WorkingBeatmap workingBeatmap)
            : base(workingBeatmap.BeatmapInfo, null)
        {
            this.workingBeatmap = workingBeatmap;
        }

        protected internal override IBeatmap? GetBeatmap()
        {
            if (BeatmapInfo.EditFile?.Filename == null)
                return workingBeatmap.GetBeatmap();

            try
            {
                string? fileStorePath = BeatmapSetInfo.GetPathForFile(BeatmapInfo.EditFile!.Filename);

                var stream = GetStream(fileStorePath);

                if (stream == null)
                {
                    Logger.Log($"Beatmap failed to load (file {BeatmapInfo.Path} not found on disk at expected location {fileStorePath}).", level: LogLevel.Error);
                    return null;
                }

                using (var reader = new LineBufferedReader(stream))
                    return Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Beatmap failed to load");
                return null;
            }
        }

        public override Texture GetBackground() => workingBeatmap.GetBackground();

        public override Stream? GetStream(string? storagePath) => workingBeatmap.GetStream(storagePath);

        protected internal override Track GetBeatmapTrack() => workingBeatmap.GetBeatmapTrack();

        protected internal override ISkin GetSkin() => workingBeatmap.GetSkin();
    }
}
