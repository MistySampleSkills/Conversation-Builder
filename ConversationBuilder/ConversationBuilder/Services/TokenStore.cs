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
using System.Threading;
using ConversationBuilder.Models;

namespace ConversationBuilder
{
    public interface ITokenStore
    {
        void AddToken(JwtTokenModel jwtTokenModel, string robotSerialNumber);

        string GetRobotSerialNumber(string token);
    }

    public class TokenStore : ITokenStore
    {
        // <Token, TokenMap>
        private readonly ConcurrentDictionary<string, TokenMap> _tokens;
        private readonly Timer _cleanupTimer;

        public TokenStore()
        {
            _tokens = new ConcurrentDictionary<string, TokenMap>();
            _cleanupTimer = new Timer(CleanTokens, null, 60000, 60000);
        }

        public void AddToken(JwtTokenModel jwtTokenModel, string robotSerialNumber)
        {
            var tokenMap = new TokenMap()
            {
                JwtTokenModel = jwtTokenModel,
                RobotSerialNumber = robotSerialNumber
            };

            _tokens.AddOrUpdate(jwtTokenModel.Token, tokenMap, (key, value) => tokenMap );
        }

        public string GetRobotSerialNumber(string token)
        {
            if (_tokens.ContainsKey(token))
            {
                return _tokens[token].RobotSerialNumber;
            }
            else
            {
                return null;
            }
        }

        private void CleanTokens(object timerData)
        {
            var remove = new List<string>();

            foreach (KeyValuePair<string, TokenMap> entry in _tokens)
            {
                if (DateTime.UtcNow >= entry.Value.JwtTokenModel.Expires)
                {
                    remove.Add(entry.Key);
                }
            }

            foreach (string key in remove)
            {
                _tokens.TryRemove(key, out TokenMap whocares);
            }
        }

        private class TokenMap
        {
            public JwtTokenModel JwtTokenModel { get; set; }

            public string RobotSerialNumber { get; set; }
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
                _cleanupTimer.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}