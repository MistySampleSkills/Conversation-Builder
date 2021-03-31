/**********************************************************************
	Copyright 2021 Misty Robotics
	Licensed under the Apache License, Version 2.0 (the "License");
	you may not use this file except in compliance with the License.
	You may obtain a copy of the License at
		http://www.apache.org/licenses/LICENSE-2.0
	Unless required by applicable law or agreed to in writing, software
	distributed under the License is distributed on an "AS IS" BASIS,
	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	See the License for the specific language governing permissions and
	limitations under the License.
	**WARRANTY DISCLAIMER.**
	* General. TO THE MAXIMUM EXTENT PERMITTED BY APPLICABLE LAW, MISTY
	ROBOTICS PROVIDES THIS SAMPLE SOFTWARE "AS-IS" AND DISCLAIMS ALL
	WARRANTIES AND CONDITIONS, WHETHER EXPRESS, IMPLIED, OR STATUTORY,
	INCLUDING THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
	PURPOSE, TITLE, QUIET ENJOYMENT, ACCURACY, AND NON-INFRINGEMENT OF
	THIRD-PARTY RIGHTS. MISTY ROBOTICS DOES NOT GUARANTEE ANY SPECIFIC
	RESULTS FROM THE USE OF THIS SAMPLE SOFTWARE. MISTY ROBOTICS MAKES NO
	WARRANTY THAT THIS SAMPLE SOFTWARE WILL BE UNINTERRUPTED, FREE OF VIRUSES
	OR OTHER HARMFUL CODE, TIMELY, SECURE, OR ERROR-FREE.
	* Use at Your Own Risk. YOU USE THIS SAMPLE SOFTWARE AND THE PRODUCT AT
	YOUR OWN DISCRETION AND RISK. YOU WILL BE SOLELY RESPONSIBLE FOR (AND MISTY
	ROBOTICS DISCLAIMS) ANY AND ALL LOSS, LIABILITY, OR DAMAGES, INCLUDING TO
	ANY HOME, PERSONAL ITEMS, PRODUCT, OTHER PERIPHERALS CONNECTED TO THE PRODUCT,
	COMPUTER, AND MOBILE DEVICE, RESULTING FROM YOUR USE OF THIS SAMPLE SOFTWARE
	OR PRODUCT.
	Please refer to the Misty Robotics End User License Agreement for further
	information and full details:
		https://www.mistyrobotics.com/legal/end-user-license-agreement/
**********************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConversationBuilder
{
    public interface IWebSocketManager
    {
        Task<String> SendMessageAsync(string robotSerialNumber, string msg);
    }

    /// <summary>
    /// Not in use in the Conversation Builder at this time. Used to communicate directly with authorized robots.
    /// </summary>
    public class WebSocketManager : IWebSocketManager, IDisposable
    {
        private readonly ConcurrentDictionary<string, WebSocketConnection> _webSocketConnections;
        private readonly Timer _connectionCheckTimer;
        
        public WebSocketManager()
        {
            _webSocketConnections = new ConcurrentDictionary<string, WebSocketConnection>();
            _connectionCheckTimer = new Timer(ConnectionsCheck, null, 60000, 60000);
        }

        private async void ConnectionsCheck(object timerData)
        {
            try
            {
                // If a client disconnects we don't know it. So perodically check them so that we don't leak resources.
                var remove = new List<KeyValuePair<string, WebSocketConnection>>();
                foreach (KeyValuePair<string, WebSocketConnection> wsc in _webSocketConnections)
                {
                    string result = await wsc.Value.SendMessageAsync("ping");
                    if (result == null)
                    {
                        remove.Add(wsc);
                    }
                }

                foreach (KeyValuePair<string, WebSocketConnection> wsc in remove)
                {
                    _webSocketConnections.TryRemove(wsc.Key, out WebSocketConnection whocares);
                    wsc.Value.Dispose();
                }
            }
            catch (Exception) { } // extra defensive primarily for websocket dispose
        }

        public void Connection(WebSocket webSocket, string robotSerialNumber, TaskCompletionSource<object> socketFinishedTcs)
        {
            var wsc = new WebSocketConnection(webSocket, robotSerialNumber, socketFinishedTcs);
            _webSocketConnections.AddOrUpdate(robotSerialNumber, wsc, (key, value) => wsc);
        }

        public async Task<string> SendMessageAsync(string robotSerialNumber, string msg)
        {
            WebSocketConnection wsc = null;
            if (_webSocketConnections.ContainsKey(robotSerialNumber))
            {
                wsc = _webSocketConnections[robotSerialNumber];
            }

            if (wsc == null)
            {
                throw new InvalidOperationException("No connection to this robot.");
            }
            else
            {
                string result = await wsc.SendMessageAsync(msg);
                if (result == null)
                {
                    try
                    {
                        _webSocketConnections.TryRemove(wsc.RobotSerialNumber, out WebSocketConnection whocares);
                        wsc.Dispose();
                    }
                    catch (Exception) { } // extra defensive primarily for websocket dispose
                    throw new InvalidOperationException("Failed to send message.");
                }
                
                return result;
            }
        }

        #region IDisposable

        private bool _disposed = false;
        
        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _connectionCheckTimer.Dispose();

                try
                {
                    foreach (KeyValuePair<string, WebSocketConnection> wsc in _webSocketConnections.ToList())
                    {
                        wsc.Value.Dispose();
                    }
                }
                catch (Exception) { }
            }

            _disposed = true;
        }

        #endregion
    }

    public class WebSocketConnection : IDisposable
    {
        private readonly WebSocket _webSocket;
        private readonly TaskCompletionSource<object> _socketFinishedTcs;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private const int SendReceiveTimeoutMs = 20000;
        private const int ReceiveBufferSize = 4096;

        public string RobotSerialNumber { get; private set; }

        public WebSocketConnection(WebSocket webSocket, string robotSerialNumber, TaskCompletionSource<object> socketFinishedTcs)
        {
            _webSocket = webSocket;
            RobotSerialNumber = robotSerialNumber;
            _socketFinishedTcs = socketFinishedTcs;
        }

        public async Task<string> SendMessageAsync(string msg)
        {
            // Don't want overlapping send-receive pairs.
            await _semaphore.WaitAsync();

            string response = null;
            try
            {
                CancellationTokenSource source = new CancellationTokenSource(SendReceiveTimeoutMs);
                CancellationToken cancellationToken = source.Token;

                var bytes = Encoding.UTF8.GetBytes(msg);
                await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);

                source = new CancellationTokenSource(SendReceiveTimeoutMs);
                cancellationToken = source.Token;

                var buffer = new byte[ReceiveBufferSize];
                WebSocketReceiveResult result = null;
                response = "";
                do
                {
                    result = await _webSocket.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        response += Encoding.UTF8.GetString(buffer.Take(result.Count).ToArray());
                    }
                } while (!result.EndOfMessage);
            }
            catch (Exception)
            {
                response = null;
            }
            finally
            {
                _semaphore.Release();
            }

            return response;
        }

        private bool _disposed = false;
        
        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _socketFinishedTcs.TrySetResult(null);
            }

            _disposed = true;
        }
    }
}