// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;

namespace osu.Game.Online.Multiplayer
{
    /// <summary>
    /// Common interface for servers that keep user state.
    /// Such servers commonly disallow concurrent connections from a single user.
    /// </summary>
    public interface IStatefulServer
    {
        public const string TOKEN_HEADER = @"Token";

        /// <summary>
        /// Sends an arbitrary key/value pair to the server.
        /// Mostly intended to be used analogously to HTTP headers sent when establishing the initial connection to a server.
        /// The reason that this exists as an operation is that after the initial handshake and protocol switch to websockets, HTTP headers are not sent anymore.
        /// </summary>
        Task SendHeader(string key, string value);
    }
}
