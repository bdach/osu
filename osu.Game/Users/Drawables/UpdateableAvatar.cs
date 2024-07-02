// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Logging;
using osu.Game.Database;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Users.Drawables
{
    /// <summary>
    /// An avatar which can update to a new user when needed.
    /// </summary>
    public partial class UpdateableAvatar : ModelBackedDrawable<APIUser?>
    {
        [Resolved]
        private UserLookupCache lookupCache { get; set; } = null!;

        public IUser? User
        {
            get => Model;
            set
            {
                switch (value)
                {
                    case APIUser apiUser:
                        Model = apiUser;
                        break;

                    case null:
                        Model = null;
                        break;

                    default:
                        lookupUser(value);
                        break;
                }
            }
        }

        private void lookupUser(IUser user) =>
            lookupCache.GetUserAsync(user.OnlineID).ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    Logger.Log($"Error when looking up user for {nameof(UpdateableAvatar)}: {t.Exception}", LoggingTarget.Network);
                    return;
                }

                Model = t.GetResultSafely();
            });

        public new bool Masking
        {
            get => base.Masking;
            set => base.Masking = value;
        }

        public new float CornerRadius
        {
            get => base.CornerRadius;
            set => base.CornerRadius = value;
        }

        public new float CornerExponent
        {
            get => base.CornerExponent;
            set => base.CornerExponent = value;
        }

        public new EdgeEffectParameters EdgeEffect
        {
            get => base.EdgeEffect;
            set => base.EdgeEffect = value;
        }

        protected override double LoadDelay => 200;

        private readonly bool isInteractive;
        private readonly bool showGuestOnNull;
        private readonly bool showUserPanelOnHover;

        /// <summary>
        /// Construct a new UpdateableAvatar.
        /// </summary>
        /// <param name="user">The initial user to display.</param>
        /// <param name="isInteractive">If set to true, hover/click sounds will play and clicking the avatar will open the user's profile.</param>
        /// <param name="showUserPanelOnHover">
        /// If set to true, the user status panel will be displayed in the tooltip.
        /// Only has an effect if <see cref="isInteractive"/> is true.
        /// </param>
        /// <param name="showGuestOnNull">Whether to show a default guest representation on null user (as opposed to nothing).</param>
        public UpdateableAvatar(IUser? user = null, bool isInteractive = true, bool showUserPanelOnHover = false, bool showGuestOnNull = true)
        {
            this.isInteractive = isInteractive;
            this.showGuestOnNull = showGuestOnNull;
            this.showUserPanelOnHover = showUserPanelOnHover;

            User = user;
        }

        protected override Drawable? CreateDrawable(APIUser? user)
        {
            if (user == null && !showGuestOnNull)
                return null;

            if (isInteractive)
            {
                return new ClickableAvatar(user, showUserPanelOnHover)
                {
                    RelativeSizeAxes = Axes.Both,
                };
            }

            return new DrawableAvatar(user)
            {
                RelativeSizeAxes = Axes.Both,
            };
        }
    }
}
