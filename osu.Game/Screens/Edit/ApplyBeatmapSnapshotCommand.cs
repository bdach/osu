// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;

namespace osu.Game.Screens.Edit
{
    public class ApplyBeatmapSnapshotCommand
    {
        private readonly LegacyEditorBeatmapPatcher patcher;

        private readonly byte[] initialState;
        private byte[]? finalState;

        public ApplyBeatmapSnapshotCommand(EditorBeatmap editorBeatmap, byte[] initialState)
        {
            patcher = new LegacyEditorBeatmapPatcher(editorBeatmap);
            this.initialState = initialState;
        }

        public void Finish(byte[] finalState)
        {
            this.finalState = finalState;
        }

        public bool? IsRedundant => finalState?.SequenceEqual(initialState);

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
