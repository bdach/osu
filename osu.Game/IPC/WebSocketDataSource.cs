// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.IPC
{
    public partial class WebSocketDataSource : IDisposable
    {
        private readonly IWebSocketProvider provider;

        public WebSocketDataSource(IWebSocketProvider provider)
        {
            this.provider = provider;
            provider.Register(this);
        }

        public event Action<object>? MessageReceived;

        public void BroadcastMessage<T>(T message)
            where T : notnull
        {
            MessageReceived?.Invoke(message);
        }

        public void Dispose()
        {
            provider.Unregister(this);
        }
    }
}
