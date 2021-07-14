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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using ConversationBuilder.DataModels;

namespace ConversationBuilder.Data.Cosmos
{
	public interface IRobotData
	{
		Task<int> GetCountAsync(string creatorFilter);
		Task<Robot> GetAsync(string id);
		Task UpdateAsync(Robot data);
		Task<ItemResponse<Robot>> AddAsync(Robot data);		
		Task DeleteAsync(string id);
		Task<IList<Robot>> GetListAsync(int startItem = 1, int totalItems = 1000, string creatorFilter = null);
		Task<IList<Robot>> GetListByDateAsync(DateTimeOffset startDate, DateTimeOffset? endDate = null, string creatorFilter = null);
		Task<Robot> GetBySerialNumberAsync(string serialNumber, bool? isProvisioned = true);
	}

	public class RobotData : PartitionManager, IRobotData
	{
		public RobotData(Container container) : base(container, RootPartition.Robot, "created", "robotName") { }

		public async Task<Robot> GetAsync(string id)
		{
			return await base.GetAsync<Robot>(id);
		}
		
		public async Task UpdateAsync(Robot data)
		{
			await base.UpdateAsync<Robot>(data);
		}

		public async Task<ItemResponse<Robot>> AddAsync(Robot data)
		{
			return await base.AddAsync<Robot>(data);
		}

		public async Task DeleteAsync(string id)
		{
			await base.DeleteAsync<Robot>(id);
		}

		public async Task<IList<Robot>> GetListAsync(int startItem = 1, int totalItems = 1000, string creatorFilter = null)
		{
			return (await base.GetListAsync<Robot>(startItem, totalItems, creatorFilter)).ToList();
		}

		public async Task<IList<Robot>> GetListByDateAsync(DateTimeOffset startDate, DateTimeOffset? endDate = null, string creatorFilter = null)
		{
			return (await base.GetListByDateAsync<Robot>(startDate, endDate, creatorFilter)).ToList();
		}

		public async Task<Robot> GetBySerialNumberAsync(string serialNumber, bool? isProvisioned = true)
		{
			FeedIterator<Robot> query = null;		
			if (isProvisioned == null)
			{
				query = _container.GetItemQueryIterator<Robot>(new QueryDefinition($"SELECT * FROM t WHERE t.itemType = @itemType AND t.serialNumber = @serialNumber")
					.WithParameter("@itemType", "Robot")
					.WithParameter("@serialNumber", serialNumber));
			}
			else
			{
				query = _container.GetItemQueryIterator<Robot>(new QueryDefinition($"SELECT * FROM t WHERE t.itemType = @itemType AND t.serialNumber = @serialNumber AND t.isProvisioned = @isProvisioned")
					.WithParameter("@itemType", "Robot")
					.WithParameter("@serialNumber", serialNumber)
					.WithParameter("@isProvisioned", isProvisioned));
			}
		
			while (query.HasMoreResults)
			{
				var response = await query.ReadNextAsync();
				return response.FirstOrDefault();
			}

			return default(Robot);
		}
	}
}