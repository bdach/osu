// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Screens.Edit;
using osu.Game.Skinning;

namespace osu.Game.Overlays.SkinEditor
{
    public partial class SkinEditorChangeHandler : Component, IEditorChangeHandler
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

        private readonly ISerialisableDrawableContainer? firstTarget;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly BindableList<ISerialisableDrawable>? components;

        public SkinEditorChangeHandler(Drawable targetScreen)
        {
            // To keep things simple, we are currently only handling the current target screen for undo / redo.
            // In the future we'll want this to cover all changes, even to skin's `InstantiationInfo`.
            // We'll also need to consider cases where multiple targets are on screen at the same time.

            firstTarget = targetScreen.ChildrenOfType<ISerialisableDrawableContainer>().FirstOrDefault();

            if (firstTarget == null)
                return;

            components = new BindableList<ISerialisableDrawable> { BindTarget = firstTarget.Components };
            components.BindCollectionChanged((_, _) => SaveState(), true);
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
            if (firstTarget == null)
                return;

            var skinnableInfos = firstTarget.CreateSerialisedInfo().ToArray();
            string json = JsonConvert.SerializeObject(skinnableInfos, new JsonSerializerSettings { Formatting = Formatting.Indented });
            stream.Write(Encoding.UTF8.GetBytes(json));
        }

        protected void ApplyStateChange(byte[] previousState, byte[] newState)
        {
            if (firstTarget == null)
                return;

            var deserializedContent = JsonConvert.DeserializeObject<IEnumerable<SerialisedDrawableInfo>>(Encoding.UTF8.GetString(newState));

            if (deserializedContent == null)
                return;

            SerialisedDrawableInfo[] skinnableInfos = deserializedContent.ToArray();
            ISerialisableDrawable[] targetComponents = firstTarget.Components.ToArray();

            // Store components based on type for later reuse
            var componentsPerTypeLookup = new Dictionary<Type, Queue<Drawable>>();

            foreach (ISerialisableDrawable component in targetComponents)
            {
                Type lookup = component.GetType();

                if (!componentsPerTypeLookup.TryGetValue(lookup, out Queue<Drawable>? componentsOfSameType))
                    componentsPerTypeLookup.Add(lookup, componentsOfSameType = new Queue<Drawable>());

                componentsOfSameType.Enqueue((Drawable)component);
            }

            for (int i = targetComponents.Length - 1; i >= 0; i--)
                firstTarget.Remove(targetComponents[i], false);

            foreach (var skinnableInfo in skinnableInfos)
            {
                Type lookup = skinnableInfo.Type;

                if (!componentsPerTypeLookup.TryGetValue(lookup, out Queue<Drawable>? componentsOfSameType))
                {
                    firstTarget.Add((ISerialisableDrawable)skinnableInfo.CreateInstance());
                    continue;
                }

                // Wherever possible, attempt to reuse existing component instances.
                if (componentsOfSameType.TryDequeue(out Drawable? component))
                {
                    component.ApplySerialisedInfo(skinnableInfo);
                }
                else
                {
                    component = skinnableInfo.CreateInstance();
                }

                firstTarget.Add((ISerialisableDrawable)component);
            }

            // Dispose components which were not reused.
            foreach ((Type _, Queue<Drawable> typeComponents) in componentsPerTypeLookup)
            {
                foreach (var component in typeComponents)
                    component.Dispose();
            }
        }

        private void updateBindables()
        {
            CanUndo.Value = savedStates.Count > 0 && currentState > 0;
            CanRedo.Value = currentState < savedStates.Count - 1;
        }
    }
}
