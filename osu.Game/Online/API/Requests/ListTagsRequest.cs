// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Game.Online.API.Requests
{
    public class ListTagsRequest : APIRequest<List<APITag>>
    {
        protected override string Target => "tags";
    }
}
