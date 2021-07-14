using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ConversationBuilder.Data.Cosmos;
using ConversationBuilder.DataModels;
using ConversationBuilder.ViewModels;
using ConversationBuilder.Extensions;
using Newtonsoft.Json;

namespace ConversationBuilder.Controllers
{
	[AuthorizeRoles(Roles.SiteAdministrator, Roles.Customer)]
	public class RecipesController : AdminToolController
	{
		public RecipesController(ICosmosDbService cosmosDbService, UserManager<ApplicationUser> userManager)
			: base(cosmosDbService, userManager) { }

		public async Task<ActionResult> Index()
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				IEnumerable<Recipe> recipes = await _cosmosDbService.ContainerManager.RecipeData.GetListAsync(1, 10000, userInfo.AccessId);
				await SetViewBagData();
				@ViewBag.Message = Convert.ToString(TempData["Message"]);
				TempData["Message"] = "";
				if (recipes == null || recipes.Count() == 0)
				{
					return View();
				}
				else
				{
					return View(recipes.OrderBy(x => x.Name));
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing recipe list.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> Details(string id)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				await SetViewBagData();
				Recipe recipe = await _cosmosDbService.ContainerManager.RecipeData.GetAsync(id);				
				if (recipe == null)
				{
					return RedirectToAction("Error", "Home", new { message = "Exception accessing recipe details.", exception = "Cannot find details for recipe." });
				}
				else
				{
					return View(recipe);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing recipe details.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> Create()
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				await SetViewBagData();
				Recipe recipe = new Recipe();
				return View(recipe);
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating recipe.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Create(Recipe model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				await SetViewBagData();
				if (ModelState.IsValid)
				{			
					model.Id = Guid.NewGuid().ToString();
					DateTimeOffset dt = DateTimeOffset.UtcNow;
					model.Created = dt;
					model.Updated = dt;
					model.CreatedBy = userInfo.AccessId;

					await _cosmosDbService.ContainerManager.RecipeData.AddAsync(model);

					return RedirectToAction(nameof(Index), new { model.Id });
				}
				else
				{
					return RedirectToAction(nameof(Create), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating recipe.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> Edit(string id)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}
				
				Recipe recipe = await _cosmosDbService.ContainerManager.RecipeData.GetAsync(id);				
				await SetViewBagData();
				if (recipe == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{			
					return View(recipe);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing recipe.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Edit(Recipe recipe)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				await SetViewBagData();
				if (ModelState.IsValid)
				{

					Recipe loadedRecipe = await _cosmosDbService.ContainerManager.RecipeData.GetAsync(recipe.Id);

					loadedRecipe.Script = recipe.Script;
					loadedRecipe.Name = recipe.Name;
					loadedRecipe.Updated = DateTimeOffset.UtcNow;
					loadedRecipe.Notes = recipe.Notes;
					loadedRecipe.Description = recipe.Description;
					await _cosmosDbService.ContainerManager.RecipeData.UpdateAsync(loadedRecipe);

					return RedirectToAction(nameof(Index));
				}
				else
				{
					return RedirectToAction(nameof(Edit), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing recipe.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> Delete(string id)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}
			
				Recipe recipe = await _cosmosDbService.ContainerManager.RecipeData.GetAsync(id);
				if (recipe == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ViewBag.CanBeDeleted = true;
					await SetViewBagData();
					return View(recipe);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting recipe.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Delete(string id, IFormCollection collection)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				 await _cosmosDbService.ContainerManager.RecipeData.DeleteAsync(id);
				return RedirectToAction(nameof(Index));				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting recipe.", exception = ex.Message });
			}
		}

	}
}