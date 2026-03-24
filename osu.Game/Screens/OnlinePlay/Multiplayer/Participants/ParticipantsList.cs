// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.Containers;
using osu.Game.Online.Multiplayer;

namespace osu.Game.Screens.OnlinePlay.Multiplayer.Participants
{
    public partial class ParticipantsList : VirtualisedListContainer<Participant, ParticipantPanel>
    {
        private BindableList<Participant> participants => RowData;

        private Participant? currentHost;

        [Resolved]
        private MultiplayerClient client { get; set; } = null!;

        public ParticipantsList()
            : base(ParticipantPanel.HEIGHT + 1, initialPoolSize: 20)
        {
        }

        protected override ScrollContainer<Drawable> CreateScrollContainer() => new OsuScrollContainer
        {
            ScrollbarVisible = false,
        };

        protected override void LoadComplete()
        {
            base.LoadComplete();

            client.RoomUpdated += onRoomUpdated;
            updateState();
        }

        private void onRoomUpdated() => Scheduler.AddOnce(updateState);

        private void updateState()
        {
            if (client.Room == null)
            {
                participants.Clear();
                return;
            }

            if (client.Room.MatchState is StandardMatchRoomState standardMatchRoomState && standardMatchRoomState.Slots is int?[] slots)
            {
                // reset host tracking - in slots mode the host's position is decided solely by their slot
                // this has the side benefit of getting the host pinned to top of list again if slots are turned off
                currentHost = null;

                if (participants.Count > slots.Length)
                    participants.RemoveRange(slots.Length, participants.Count - slots.Length);

                for (int i = 0; i < slots.Length; ++i)
                {
                    var participant = slots[i] == null ? Participant.EmptySlot : Participant.FromUser(client.Room.Users.Single(u => u.UserID == slots[i]));

                    if (i >= participants.Count)
                        participants.Add(participant);
                    if (!participant.Equals(participants[i]))
                        participants[i] = participant;
                }

                return;
            }

            // Remove panels for empty slots & users no longer in the room.
            for (int i = participants.Count - 1; i >= 0; i--)
            {
                var participant = participants[i];

                // Note that we *must* use reference equality here, as this call is scheduled and a user may have left and joined since it was last run.
                if (participant.IsEmpty || client.Room.Users.All(u => !ReferenceEquals(participant.User, u)))
                    participants.RemoveAt(i);
            }

            Debug.Assert(participants.All(p => !p.IsEmpty));

            // Add panels for all users new to the room.
            foreach (var user in client.Room.Users.Except(participants.Select(u => u.User!)))
                participants.Add(Participant.FromUser(user));

            if (currentHost == null || !currentHost.User!.Equals(client.Room.Host))
            {
                currentHost = null;

                // Change position of new host to display above all participants.
                if (client.Room.Host != null)
                {
                    currentHost = participants.SingleOrDefault(u => u.User!.Equals(client.Room.Host));
                    int currentHostIndex = currentHost == null ? -1 : participants.IndexOf(currentHost);

                    if (currentHostIndex > 0)
                    {
                        participants.Move(currentHostIndex, 0);
                        currentHost = participants[0];
                    }
                }
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (client.IsNotNull())
                client.RoomUpdated -= onRoomUpdated;
        }
    }

    public record Participant
    {
        [MemberNotNullWhen(true, nameof(User))]
        public bool IsEmpty { get; }

        public MultiplayerRoomUser? User { get; }

        private Participant(bool isEmpty, MultiplayerRoomUser? user)
        {
            IsEmpty = isEmpty;
            User = user;
        }

        public static Participant FromUser(MultiplayerRoomUser user) => new Participant(false, user);

        public static Participant EmptySlot => new Participant(true, null);
    }
}
