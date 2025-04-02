// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Online
{
    public class DevelopmentEndpointConfiguration : EndpointConfiguration
    {
        public DevelopmentEndpointConfiguration()
        {
            WebsiteUrl = APIUrl = @"http://localhost:8080";
            APIClientSecret = @"GuxYNvk5yZkeIumVTjxBTFWAkEpyq8aiolwEM84f";
            APIClientID = "1";

            const string spectator_server_root_url = @"http://localhost:8081";
            SpectatorUrl = $@"{spectator_server_root_url}/spectator";
            MultiplayerUrl = $@"{spectator_server_root_url}/multiplayer";
            MetadataUrl = $@"{spectator_server_root_url}/metadata";
            BeatmapSubmissionServiceUrl = @"http://localhost:5089";
        }
    }
}
