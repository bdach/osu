// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Framework.Logging;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Resources.Localisation.Web;

namespace osu.Game.Beatmaps.Drawables.Cards.Buttons
{
    public partial class FavouriteButton : BeatmapCardIconButton
    {
        public Bindable<APIBeatmapSet> BeatmapSet { get; } = new Bindable<APIBeatmapSet>();

        private PostBeatmapFavouriteRequest? favouriteRequest;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Action = toggleFavouriteStatus;
            BeatmapSet.BindValueChanged(_ => updateState(), true);
        }

        private void toggleFavouriteStatus()
        {
            var actionType = BeatmapSet.Value.HasFavourited ? BeatmapFavouriteAction.UnFavourite : BeatmapFavouriteAction.Favourite;

            favouriteRequest?.Cancel();
            favouriteRequest = new PostBeatmapFavouriteRequest(BeatmapSet.Value.OnlineID, actionType);

            Enabled.Value = false;
            favouriteRequest.Success += () =>
            {
                bool favourited = actionType == BeatmapFavouriteAction.Favourite;

                BeatmapSet.Value.HasFavourited = favourited;
                BeatmapSet.Value.FavouriteCount += favourited ? 1 : -1;
                BeatmapSet.TriggerChange();

                Enabled.Value = true;
            };
            favouriteRequest.Failure += e =>
            {
                Logger.Error(e, $"Failed to {actionType.ToString().ToLowerInvariant()} beatmap: {e.Message}");
                Enabled.Value = true;
            };

            api.Queue(favouriteRequest);
        }

        private void updateState()
        {
            if (BeatmapSet.Value.HasFavourited)
            {
                Icon.Icon = FontAwesome.Solid.Heart;
                TooltipText = BeatmapsetsStrings.ShowDetailsUnfavourite;
            }
            else
            {
                Icon.Icon = FontAwesome.Regular.Heart;
                TooltipText = BeatmapsetsStrings.ShowDetailsFavourite;
            }
        }
    }
}
