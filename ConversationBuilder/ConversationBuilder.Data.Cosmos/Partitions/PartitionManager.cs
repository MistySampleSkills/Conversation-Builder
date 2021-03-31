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
	public class PartitionManager
	{
		private RootPartition _rootPartition;
		protected string _defaultTimeStampItem;
		protected string _defaultOrderItem;

		protected Container _container;
		public PartitionManager(Container container, RootPartition rootPartition, string defaultTimeStampItem, string defaultOrderItem)
		{	
			_rootPartition = rootPartition;
			_container = container;
			_defaultTimeStampItem = string.IsNullOrWhiteSpace(defaultTimeStampItem) ? "timestamp" : defaultTimeStampItem;
			_defaultOrderItem = string.IsNullOrWhiteSpace(defaultOrderItem) ? "timestamp" : defaultOrderItem;
		}

		protected async Task<ItemResponse<TDataItem>> AddAsync<TDataItem>(TDataItem data)
			where TDataItem : IDataItem
		{
			return await _container.CreateItemAsync(data, new PartitionKey(_rootPartition.ToString()));
		}

		protected async Task DeleteAsync<TDataItem>(string id)
			where TDataItem : IDataItem
		{
			await _container.DeleteItemAsync<IDataItem>(id, new PartitionKey(_rootPartition.ToString()));
		}

		protected async Task<TDataItem> GetAsync<TDataItem>(string id)
			where TDataItem : IDataItem
		{
			try
			{
				if (string.IsNullOrWhiteSpace(id))
				{
					return default(TDataItem);
				}
				ItemResponse<TDataItem> response = await _container.ReadItemAsync<TDataItem>(id, new PartitionKey(_rootPartition.ToString()));
				return response.Resource;
			}
			catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				return default(TDataItem);
			}
		}

		protected async Task UpdateAsync<TDataItem>(TDataItem data)
			where TDataItem : IDataItem
		{
			_ = await _container.UpsertItemAsync(data, new PartitionKey(_rootPartition.ToString()));
		}

		protected async Task<IEnumerable<TDataItem>> GetListAsync<TDataItem>(int startItem, int totalItems = 100, string conversationId = null)
		{
			startItem = startItem < 1 ? 1 : startItem;
			totalItems = totalItems < 1 ? 1 : totalItems;

			FeedIterator<TDataItem> query = null;
			if(conversationId == null)
			{
				query = _container.GetItemQueryIterator<TDataItem>(new QueryDefinition($"SELECT * FROM t WHERE t.itemType = @itemType ORDER BY t.{_defaultOrderItem} DESC OFFSET @start LIMIT @total")
					.WithParameter("@itemType", _rootPartition.ToString())				
					.WithParameter("@start", startItem-1)
					.WithParameter("@total", totalItems));		
			}
			else
			{
				query = _container.GetItemQueryIterator<TDataItem>(new QueryDefinition($"SELECT * FROM t WHERE t.itemType = @itemType AND t.conversationId = @conversationId ORDER BY t.{_defaultOrderItem} DESC OFFSET @start LIMIT @total")
					.WithParameter("@itemType", _rootPartition.ToString())				
					.WithParameter("@conversationId", conversationId)				
					.WithParameter("@start", startItem-1)
					.WithParameter("@total", totalItems));		
			}
			
			List<TDataItem> results = new List<TDataItem>();
			while (query.HasMoreResults)
			{
				var response = await query.ReadNextAsync();

				results.AddRange(response);
			}

			return results;
		}

		protected async Task<IEnumerable<TDataItem>> GetListByDateAsync<TDataItem>(DateTimeOffset startDate, DateTimeOffset? endDate = null, string conversationId = null)
		{
			FeedIterator<TDataItem> query = null;
			if (endDate == null)
			{
				endDate = DateTimeOffset.UtcNow;
			}
			
			//Assumes looking up in utc and data is saved in 24 hour format
			//https://docs.microsoft.com/en-us/azure/cosmos-db/working-with-dates
			string endDtAs24 = ((DateTimeOffset)endDate).ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
			string startDtAs24 = startDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");

			if(conversationId == null)
			{
				query = _container.GetItemQueryIterator<TDataItem>(new QueryDefinition($"SELECT * FROM t WHERE t.itemType = @itemType AND t.{_defaultTimeStampItem} >= @startDate AND t.{_defaultTimeStampItem} <= @endDate ORDER BY t.{_defaultTimeStampItem} DESC")
					.WithParameter("@itemType", _rootPartition.ToString())		
					.WithParameter("@startDate", startDtAs24)
					.WithParameter("@endDate", endDtAs24));
			}
			else
			{
				query = _container.GetItemQueryIterator<TDataItem>(new QueryDefinition($"SELECT * FROM t WHERE t.itemType = @itemType AND t.conversationId = @conversationId AND t.{_defaultTimeStampItem} >= @startDate AND t.{_defaultTimeStampItem} <= @endDate ORDER BY t.{_defaultTimeStampItem} DESC")
					.WithParameter("@itemType", _rootPartition.ToString())			
					.WithParameter("@conversationId", conversationId)		
					.WithParameter("@startDate", startDtAs24)
					.WithParameter("@endDate", endDtAs24));	
			}

			List<TDataItem> results = new List<TDataItem>();
			while (query.HasMoreResults)
			{
				var response = await query.ReadNextAsync();

				results.AddRange(response);
			}

			return results;
		}

		public async Task<int> GetCountAsync(string conversationId)
		{
			FeedIterator<int> screenIdIterator = null;
			if(conversationId == null)
			{
				screenIdIterator = _container.GetItemQueryIterator<int>(new QueryDefinition(@"SELECT VALUE COUNT(1) FROM c WHERE c.itemType = @itemType") 
					.WithParameter("@itemType", _rootPartition.ToString()));
			}
			else
			{
				screenIdIterator = _container.GetItemQueryIterator<int>(new QueryDefinition(@"SELECT VALUE COUNT(1) FROM c WHERE c.itemType = @itemType AND c.conversationId = @conversationId") 
					.WithParameter("@itemType", _rootPartition.ToString())
					.WithParameter("@conversationId", conversationId));					
			}

			while (screenIdIterator.HasMoreResults)
			{
				var response = await screenIdIterator.ReadNextAsync();
				return response.First();
			}
			return 0;
		}

		public async Task<int> GetCountAsync()
		{
			FeedIterator<int> screenIdIterator = null;
			screenIdIterator = _container.GetItemQueryIterator<int>(new QueryDefinition(@"SELECT VALUE COUNT(1) FROM c WHERE c.itemType = @itemType") 
					.WithParameter("@itemType", _rootPartition.ToString()));
					
			while (screenIdIterator.HasMoreResults)
			{
				var response = await screenIdIterator.ReadNextAsync();
				return response.First();
			}
			return 0;
		}
	}
}