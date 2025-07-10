// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;

namespace osu.Game.Screens.Edit
{
    public class ApplyBeatmapSnapshotCommand : ICommand
    {
        private readonly byte[] initialState;
        private byte[]? finalState;

        public ApplyBeatmapSnapshotCommand(byte[] initialState)
        {
            this.initialState = initialState;
        }

        public void Finish(byte[] finalState)
        {
            this.finalState = finalState;
        }

        public bool? IsRedundant => finalState?.SequenceEqual(initialState);

        public void Apply(EditorBeatmap editorBeatmap)
        {
            if (finalState == null)
                throw new InvalidOperationException();

            new LegacyEditorBeatmapPatcher(editorBeatmap).Patch(initialState, finalState);
        }

        public void Rollback(EditorBeatmap editorBeatmap)
        {
            if (finalState == null)
                throw new InvalidOperationException();

            new LegacyEditorBeatmapPatcher(editorBeatmap).Patch(finalState, initialState);
        }
    }
}
