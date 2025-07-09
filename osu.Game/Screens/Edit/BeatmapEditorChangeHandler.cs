// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Game.Beatmaps.Formats;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Screens.Edit
{
    public partial class BeatmapEditorChangeHandler : Component, IEditorChangeHandler
    {
        /// <summary>
        /// Fires whenever a transaction begins. Will not fire on nested transactions.
        /// </summary>
        public event Action? TransactionBegan;

        /// <summary>
        /// Fires when the last transaction completes.
        /// </summary>
        public event Action? TransactionEnded;

        /// <summary>
        /// Fires when <see cref="SaveState"/> is called and results in a non-transactional state save.
        /// </summary>
        public event Action? SaveStateTriggered;

        public bool TransactionActive => bulkChangesStarted > 0;

        private int bulkChangesStarted;
        public readonly Bindable<bool> CanUndo = new Bindable<bool>();
        public readonly Bindable<bool> CanRedo = new Bindable<bool>();

        public event Action? OnStateChange;

        private readonly List<ApplyBeatmapSnapshotCommand> commandHistory = new List<ApplyBeatmapSnapshotCommand>();
        private ApplyBeatmapSnapshotCommand? currentSnapshotCommand;

        private int lastExecutedCommand = -1;

        /// <summary>
        /// A SHA-2 hash representing the current visible editor state.
        /// </summary>
        public string CurrentStateHash
        {
            get
            {
                using var stream = new MemoryStream();
                using var sw = new StreamWriter(stream, Encoding.UTF8, 1024, true);
                new LegacyBeatmapEncoder(editorBeatmap, editorBeatmap.BeatmapSkin).Encode(sw);
                return stream.ComputeSHA2Hash();
            }
        }

        private bool isRestoring;

        public const int MAX_SAVED_STATES = 50;

        private readonly EditorBeatmap editorBeatmap;

        /// <summary>
        /// Creates a new <see cref="EditorChangeHandler"/>.
        /// </summary>
        /// <param name="editorBeatmap">The <see cref="EditorBeatmap"/> to track the <see cref="HitObject"/>s of.</param>
        public BeatmapEditorChangeHandler(EditorBeatmap editorBeatmap)
        {
            this.editorBeatmap = editorBeatmap;

            editorBeatmap.TransactionBegan += BeginChange;
            editorBeatmap.TransactionEnded += EndChange;
            editorBeatmap.SaveStateTriggered += SaveState;
        }

        public void BeginChange()
        {
            currentSnapshotCommand ??= new ApplyBeatmapSnapshotCommand(editorBeatmap);

            if (bulkChangesStarted++ == 0)
                TransactionBegan?.Invoke();
        }

        /// <summary>
        /// Signal the end of a change.
        /// </summary>
        /// <exception cref="InvalidOperationException">Throws if <see cref="BeginChange"/> was not first called.</exception>
        public void EndChange()
        {
            if (bulkChangesStarted == 0 || currentSnapshotCommand == null)
                throw new InvalidOperationException($"Cannot call {nameof(EndChange)} without a previous call to {nameof(BeginChange)}.");

            if (--bulkChangesStarted == 0)
            {
                SaveState();
                TransactionEnded?.Invoke();
            }
        }

        /// <summary>
        /// Force an update of the state with no attached transaction.
        /// This is a no-op if a transaction is already active. Should generally be used as a safety measure to ensure granular changes are not left outside a transaction.
        /// </summary>
        public void SaveState()
        {
            if (bulkChangesStarted > 0)
                return;

            SaveStateTriggered?.Invoke();

            if (isRestoring)
                return;

            Debug.Assert(currentSnapshotCommand != null);

            currentSnapshotCommand.Finish();

            if (currentSnapshotCommand.IsRedundant == true)
                return;

            if (lastExecutedCommand < commandHistory.Count - 1)
                commandHistory.RemoveRange(lastExecutedCommand + 1, commandHistory.Count - lastExecutedCommand - 1);

            if (commandHistory.Count > MAX_SAVED_STATES)
                commandHistory.RemoveAt(0);

            commandHistory.Add(currentSnapshotCommand);
            currentSnapshotCommand = null;

            lastExecutedCommand = commandHistory.Count - 1;

            OnStateChange?.Invoke();
            updateBindables();
        }

        public void RestoreState(int direction)
        {
            if (Math.Abs(direction) != 1)
                throw new ArgumentException();

            if (TransactionActive)
                return;

            if (commandHistory.Count == 0)
                return;

            if (direction < 0 && !CanUndo.Value) return;
            if (direction > 0 && !CanRedo.Value) return;

            isRestoring = true;

            if (direction > 0)
                commandHistory[++lastExecutedCommand].Apply();
            else
                commandHistory[lastExecutedCommand--].Rollback();

            isRestoring = false;

            OnStateChange?.Invoke();
            updateBindables();
        }

        private void updateBindables()
        {
            CanUndo.Value = commandHistory.Count > 0 && lastExecutedCommand >= 0;
            CanRedo.Value = lastExecutedCommand < commandHistory.Count - 1;
        }
    }
}
