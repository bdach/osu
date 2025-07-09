// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Text;
using osu.Game.Beatmaps.Formats;

namespace osu.Game.Screens.Edit
{
    public class ApplyBeatmapSnapshotCommand
    {
        private readonly EditorBeatmap editorBeatmap;
        private readonly LegacyEditorBeatmapPatcher patcher;

        private readonly byte[] initialState;
        private byte[]? finalState;

        public ApplyBeatmapSnapshotCommand(EditorBeatmap editorBeatmap)
        {
            this.editorBeatmap = editorBeatmap;
            patcher = new LegacyEditorBeatmapPatcher(editorBeatmap);
            initialState = getBeatmapSnapshot();
        }

        public void Finish()
        {
            finalState = getBeatmapSnapshot();
        }

        public bool? IsRedundant => finalState?.SequenceEqual(initialState);

        private byte[] getBeatmapSnapshot()
        {
            using var stream = new MemoryStream();
            using var sw = new StreamWriter(stream, Encoding.UTF8, 1024, true);
            new LegacyBeatmapEncoder(editorBeatmap, editorBeatmap.BeatmapSkin).Encode(sw);
            return stream.ToArray();
        }

        public void Apply()
        {
            if (finalState == null)
                throw new InvalidOperationException();

            patcher.Patch(initialState, finalState);
        }

        public void Rollback()
        {
            if (finalState == null)
                throw new InvalidOperationException();

            patcher.Patch(finalState, initialState);
        }
    }
}
