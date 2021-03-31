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
	public interface ITriggerDetailData
	{
		Task<int> GetCountAsync();
		Task<TriggerDetail> GetAsync(string id);
		Task UpdateAsync(TriggerDetail data);
		Task<ItemResponse<TriggerDetail>> AddAsync(TriggerDetail data);		
		Task DeleteAsync(string id);
		Task<IList<TriggerDetail>> GetListAsync(int startItem = 1, int totalItems = 100);
		Task<IList<TriggerDetail>> GetListByDateAsync(DateTimeOffset startDate, DateTimeOffset? endDate = null);
	}

	public class TriggerDetailData : PartitionManager, ITriggerDetailData
	{
		public TriggerDetailData(Container container) : base(container, RootPartition.TriggerDetail, "created", "name") { }

		public async Task<TriggerDetail> GetAsync(string id)
		{
			return await base.GetAsync<TriggerDetail>(id);
		}
		
		public async Task UpdateAsync(TriggerDetail data)
		{
			await base.UpdateAsync<TriggerDetail>(data);
		}

		public async Task<ItemResponse<TriggerDetail>> AddAsync(TriggerDetail data)
		{
			return await base.AddAsync<TriggerDetail>(data);
		}

		public async Task DeleteAsync(string id)
		{
			await base.DeleteAsync<TriggerDetail>(id);
		}

		public async Task<IList<TriggerDetail>> GetListAsync(int startItem = 1, int totalItems = 100)
		{
			return (await base.GetListAsync<TriggerDetail>(startItem, totalItems)).ToList();
		}

		public async Task<IList<TriggerDetail>> GetListByDateAsync(DateTimeOffset startDate, DateTimeOffset? endDate = null)
		{
			return (await base.GetListByDateAsync<TriggerDetail>(startDate, endDate)).ToList();
		}
	}
}