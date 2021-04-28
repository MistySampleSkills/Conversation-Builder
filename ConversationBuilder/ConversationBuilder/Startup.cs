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
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ConversationBuilder.Data.Cosmos;
using ConversationBuilder.DataModels;
using ConversationBuilder.Extensions;
using ConversationBuilder.Models;
using Mobsites.AspNetCore.Identity.Cosmos;
using IdentityRole = Mobsites.AspNetCore.Identity.Cosmos.IdentityRole;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace ConversationBuilder
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		private WebSocketManager _websocketManager;
		private TokenStore _tokenStore;

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddTransient<IEmailSender, EmailService>(i => 
                new EmailService(
                    Configuration.GetValue<int>("EmailSender:Port"),
                    Configuration["EmailSender:Host"],
                    Configuration["EmailSender:Email"],
                    Configuration["EmailSender:Password"]
                )
            );

			services.Configure<CookiePolicyOptions>(options =>
			{
				// This lambda determines whether user consent for non-essential cookies is needed for a given request.
				options.CheckConsentNeeded = context => false;
				options.MinimumSameSitePolicy = SameSiteMode.None;
			});

			services.AddMvc(options =>
			{
				//TODO Add auth on api and re-add
				//var policy = new AuthorizationPolicyBuilder()
				//	.RequireAuthenticatedUser()
				//	.Build();
				//options.Filters.Add(new AuthorizeFilter(policy));
			})
			.SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

			IConfigurationSection configSection = Configuration.GetSection("CosmosDb");
			Tuple<ICosmosDbService, CosmosClient> spaceBits = InitializeCosmosClientInstanceAsync(configSection).GetAwaiter().GetResult();
			services.AddSingleton(spaceBits.Item1);

			_tokenStore = new TokenStore();
			services.AddSingleton<ITokenStore>(_tokenStore);
			_websocketManager = new WebSocketManager();
			services.AddSingleton<IWebSocketManager>(_websocketManager);

			services
				.AddCosmosStorageProvider(options =>
				{
					options.ConnectionString = configSection.GetSection("ConnectionString").Value;
					options.CosmosClientOptions = new CosmosClientOptions
					{
						SerializerOptions = new CosmosSerializationOptions
						{
							IgnoreNullValues = false
						}
					};
					options.DatabaseId = configSection.GetSection("DatabaseName").Value;
					options.ContainerProperties = new ContainerProperties
					{
						Id = "Identity",
					};
				});

			services
				.AddDefaultCosmosIdentity<ApplicationUser>(options =>
				{
					// User settings
					options.User.RequireUniqueEmail = true;

					// Password settings
					options.Password.RequireDigit = true;
					options.Password.RequiredLength = 8;
					options.Password.RequireLowercase = true;
					options.Password.RequireNonAlphanumeric = false;
					options.Password.RequireUppercase = true;

					// Lockout settings
					options.Lockout.AllowedForNewUsers = true;
					options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
					options.Lockout.MaxFailedAccessAttempts = 5;
				})
				// Add other IdentityBuilder methods.
				.AddDefaultUI()
				.AddDefaultTokenProviders();

			services.AddRazorPages();			
			services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
				.AddJwtBearer(options =>
				{
					options.RequireHttpsMetadata = false;
					options.SaveToken = true;
					options.TokenValidationParameters = new TokenValidationParameters
					{
						ValidateIssuer = true,
						ValidateAudience = true,
						ValidateLifetime = true,
						ValidateIssuerSigningKey = true,
						ValidIssuer = Configuration["Jwt:Issuer"],
						ValidAudience = Configuration["Jwt:Audience"],
						IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:SecretKey"])),
						ClockSkew = TimeSpan.Zero
					};
				});

			services.AddSession();			

		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env, RoleManager<IdentityRole> roleManager, 
			UserManager<ApplicationUser> userManager, ICosmosDbService dbService)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
				app.UseHsts();
			}

			app.UseSession();
			app.UseHttpsRedirection();
			app.UseStaticFiles();/*new StaticFileOptions()
			{
				FileProvider = new PhysicalFileProvider(
					System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), @"MyStaticFiles")),
				RequestPath = new PathString("/StaticFiles")
			});*/
			app.UseCookiePolicy();
			app.UseRouting();

			app.UseAuthentication();
			app.UseAuthorization();

			app.UseWebSockets();
			app.Use(async (context, next) =>
			{
				if (context.Request.Path == "/ws" & context.WebSockets.IsWebSocketRequest & context.Request.Headers.ContainsKey("auth"))
				{
					// Authenticate connection request
					string token = context.Request.Headers["auth"];
					string robotSerialNumber = _tokenStore.GetRobotSerialNumber(token);
					if (robotSerialNumber != null)
					{
						using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync())
						{
							var socketFinishedTcs = new TaskCompletionSource<object>();
							_websocketManager.Connection(webSocket, robotSerialNumber, socketFinishedTcs);
							await socketFinishedTcs.Task;
						}
					}
					else
					{
						context.Response.StatusCode = 401;
					}
				}
				else
				{
					await next();
				}
			});

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute(
					name: "default",
					pattern: "{controller=Home}/{action=Index}/{id?}"
				);

				endpoints.MapRazorPages();
				endpoints.MapControllers();
			});

			if (!roleManager.RoleExistsAsync(Roles.SiteAdministrator).Result)
			{
				roleManager.CreateAsync(new IdentityRole
				{
					Name = Roles.SiteAdministrator
				}).Wait();
			}

			if (!roleManager.RoleExistsAsync(Roles.Customer).Result)
			{
				roleManager.CreateAsync(new IdentityRole
				{
					Name = Roles.Customer
				}).Wait();
			}

			if (!roleManager.RoleExistsAsync(Roles.MistyRobot).Result)
			{
				roleManager.CreateAsync(new IdentityRole
				{
					Name = Roles.MistyRobot
				}).Wait();
			}

			const string MistyAdminId = "7a92d740-f159-4516-8215-89efbf685499";
			ApplicationUser user = userManager.FindByIdAsync(MistyAdminId).GetAwaiter().GetResult();

			if (user == null)
			{
				string personId = Guid.NewGuid().ToString();

				//TODO Put your admin email in here for the default account!
				user = new ApplicationUser
				{
					Email = "hello-misty@mistyrobotics.com",
					Id = MistyAdminId,
					UserName = "hello-misty@mistyrobotics.com",
				};

				//TODO Set your password!
				IdentityResult identityResult = userManager.CreateAsync(user, "P@ssw0rd!").GetAwaiter().GetResult();
				IdentityResult roleResult = userManager.AddToRoleAsync(user, Roles.SiteAdministrator).GetAwaiter().GetResult();

				string code = userManager.GenerateEmailConfirmationTokenAsync(user).GetAwaiter().GetResult();
				userManager.ConfirmEmailAsync(user, code);
			}
		}

		/// <summary>
		/// Creates a Cosmos DB database and a container with the specified partition key.
		/// </summary>
		/// <returns></returns>
		private static async Task<Tuple<ICosmosDbService, CosmosClient>> InitializeCosmosClientInstanceAsync(IConfigurationSection configurationSection)
		{
			string databaseName = configurationSection.GetSection("DatabaseName").Value;
			string account = configurationSection.GetSection("Account").Value;
			string key = configurationSection.GetSection("Key").Value;

			CosmosClientOptions options = new CosmosClientOptions
			{
				SerializerOptions = new CosmosSerializationOptions
				{
					PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
				}
			};

			CosmosClient client = new CosmosClient(account, key, options);
			ICosmosDbService cosmosDbService = new CosmosDbService(client, databaseName);
			DatabaseResponse database = await client.CreateDatabaseIfNotExistsAsync(databaseName);

			await database.Database.CreateContainerIfNotExistsAsync(ContainerType.ConversationBuilder.ToString(), "/itemType");
			return new Tuple<ICosmosDbService, CosmosClient>(cosmosDbService, client);
		}
	}
}