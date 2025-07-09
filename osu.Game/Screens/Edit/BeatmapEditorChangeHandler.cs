// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private readonly List<byte[]> savedStates = new List<byte[]>();

        private int currentState = -1;

        /// <summary>
        /// A SHA-2 hash representing the current visible editor state.
        /// </summary>
        public string CurrentStateHash
        {
            get
            {
                ensureStateSaved();

                using (var stream = new MemoryStream(savedStates[currentState]))
                    return stream.ComputeSHA2Hash();
            }
        }

        private bool isRestoring;

        public const int MAX_SAVED_STATES = 50;

        private readonly LegacyEditorBeatmapPatcher patcher;
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

            patcher = new LegacyEditorBeatmapPatcher(editorBeatmap);
        }

        public void BeginChange()
        {
            ensureStateSaved();

            if (bulkChangesStarted++ == 0)
                TransactionBegan?.Invoke();
        }

        private void ensureStateSaved()
        {
            if (savedStates.Count == 0)
                SaveState();
        }

        /// <summary>
        /// Signal the end of a change.
        /// </summary>
        /// <exception cref="InvalidOperationException">Throws if <see cref="BeginChange"/> was not first called.</exception>
        public void EndChange()
        {
            if (bulkChangesStarted == 0)
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

            using (var stream = new MemoryStream())
            {
                WriteCurrentStateToStream(stream);
                byte[] newState = stream.ToArray();

                // if the previous state is binary equal we don't need to push a new one, unless this is the initial state.
                if (savedStates.Count > 0 && newState.SequenceEqual(savedStates[currentState])) return;

                if (currentState < savedStates.Count - 1)
                    savedStates.RemoveRange(currentState + 1, savedStates.Count - currentState - 1);

                if (savedStates.Count > MAX_SAVED_STATES)
                    savedStates.RemoveAt(0);

                savedStates.Add(newState);

                currentState = savedStates.Count - 1;

                OnStateChange?.Invoke();
                updateBindables();
            }
        }

        public void RestoreState(int direction)
        {
            if (TransactionActive)
                return;

            if (savedStates.Count == 0)
                return;

            int newState = Math.Clamp(currentState + direction, 0, savedStates.Count - 1);
            if (currentState == newState)
                return;

            isRestoring = true;

            ApplyStateChange(savedStates[currentState], savedStates[newState]);

            currentState = newState;

            isRestoring = false;

            OnStateChange?.Invoke();
            updateBindables();
        }

        protected void WriteCurrentStateToStream(MemoryStream stream)
        {
            using (var sw = new StreamWriter(stream, Encoding.UTF8, 1024, true))
                new LegacyBeatmapEncoder(editorBeatmap, editorBeatmap.BeatmapSkin).Encode(sw);
        }

        protected void ApplyStateChange(byte[] previousState, byte[] newState) =>
            patcher.Patch(previousState, newState);

        private void updateBindables()
        {
            CanUndo.Value = savedStates.Count > 0 && currentState > 0;
            CanRedo.Value = currentState < savedStates.Count - 1;
        }
    }
}
