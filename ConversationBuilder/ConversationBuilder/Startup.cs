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
			app.UseStaticFiles();
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

				SeedDatabase(dbService).GetAwaiter().GetResult();
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

		private async Task SeedDatabase(ICosmosDbService dbService)
		{
			try
			{
				//Add Animations
				IList<Animation> animations = await dbService.ContainerManager.AnimationData.GetListAsync();
				if(!animations.Any())
				{
					foreach(Animation seedAnimation in GetAnimationSeeds())
					{
						if(!animations.Any(x => x.Name == seedAnimation.Name))
						{
							await dbService.ContainerManager.AnimationData.AddAsync(seedAnimation);
						}
					}
				}
				
				
				DateTime now = DateTime.UtcNow;

				//Add Onboard Speech Config
				IList<SpeechConfiguration> speechConfigurations = await dbService.ContainerManager.SpeechConfigurationData.GetListAsync();
				IList<CharacterConfiguration> characterConfigurations = await dbService.ContainerManager.CharacterConfigurationData.GetListAsync();
				if(!speechConfigurations.Any() && !characterConfigurations.Any())
				{
					string speechConfigGuid = Guid.NewGuid().ToString();
					SpeechConfiguration onboardSpeech = new SpeechConfiguration
					{
						Id = speechConfigGuid,
						Name = "Misty TTS - Vosk ASR",
						CreatedBy = "System",
						Created = now,
						Updated = now,
						SpeechRecognitionService = "Vosk",
						TextToSpeechService = "Misty"
					};
					
					await dbService.ContainerManager.SpeechConfigurationData.AddAsync(onboardSpeech);

					CharacterConfiguration characterConfiguration = new CharacterConfiguration
					{
						Id = Guid.NewGuid().ToString(),
						Name = "Misty",
						SpeechConfiguration = speechConfigGuid,
						CreatedBy = "System",
						Created = now,
						Updated = now,
						
					};
					
					await dbService.ContainerManager.CharacterConfigurationData.AddAsync(characterConfiguration);
				
					speechConfigGuid = Guid.NewGuid().ToString();
					onboardSpeech = new SpeechConfiguration
					{
						Id = speechConfigGuid,
						Name = "Zira TTS - Vosk ASR",
						SpeakingVoice = "Zira",

						CreatedBy = "System",
						Created = now,
						Updated = now,
						SpeechRecognitionService = "Vosk",
						TextToSpeechService = "Skill"
					};
					
					await dbService.ContainerManager.SpeechConfigurationData.AddAsync(onboardSpeech);

					characterConfiguration = new CharacterConfiguration
					{
						Id = Guid.NewGuid().ToString(),
						Name = "Zira",
						SpeechConfiguration = speechConfigGuid,
						CreatedBy = "System",
						Created = now,
						Updated = now,
						
					};
					
					await dbService.ContainerManager.CharacterConfigurationData.AddAsync(characterConfiguration);
				}


				//Add Basic Speech Intents and  Speech Triggers
				IList<SpeechHandler> speechHandlers = await dbService.ContainerManager.SpeechHandlerData.GetListAsync();
				IList<TriggerDetail> triggers = await dbService.ContainerManager.TriggerDetailData.GetListAsync();

				if(!speechHandlers.Any() && !triggers.Any())
				{
					string speechConfigGuid = Guid.NewGuid().ToString();
					SpeechHandler speechHandler = new SpeechHandler
					{
						Id = speechConfigGuid,
						Name = "No",
						CreatedBy = "System",
						Created = now,
						Updated = now,
						ExactPhraseMatchesOnly = false,
						WordMatchRule = "Exact",
						Utterances = new List<string>{"no", "nope", "nada", "didn't", "did not" }
					};

					await dbService.ContainerManager.SpeechHandlerData.AddAsync(speechHandler);

					string triggerGuid = Guid.NewGuid().ToString();
					TriggerDetail trigger = new TriggerDetail
					{
						Id = triggerGuid,
						Name = "Heard No",
						CreatedBy = "System",
						Created = now,
						Updated = now,
						TriggerFilter = speechConfigGuid
					};

					await dbService.ContainerManager.TriggerDetailData.AddAsync(trigger);
				
					speechConfigGuid = Guid.NewGuid().ToString();
					speechHandler = new SpeechHandler
					{
						Id = speechConfigGuid,
						Name = "Yes",
						CreatedBy = "System",
						Created = now,
						Updated = now,
						ExactPhraseMatchesOnly = false,
						WordMatchRule = "Exact",
						Utterances = new List<string>{"yes", "yeah", "sure", "okay", "yep", "did" }
					};

					await dbService.ContainerManager.SpeechHandlerData.AddAsync(speechHandler);

					triggerGuid = Guid.NewGuid().ToString();
					trigger = new TriggerDetail
					{
						Id = triggerGuid,
						Name = "Heard Yes",
						CreatedBy = "System",
						Created = now,
						Updated = now,
						TriggerFilter = speechConfigGuid
					};

					await dbService.ContainerManager.TriggerDetailData.AddAsync(trigger);
				}


				//More common triggers
				if(!triggers.Any(x => x.Name.ToLower().Trim() == "front left bumper pressed"))
				{
					string triggerGuid = Guid.NewGuid().ToString();
					TriggerDetail trigger = new TriggerDetail
					{
						Id = triggerGuid,
						Name = "Front Left Bumper Pressed",
						CreatedBy = "System",
						Created = now,
						Updated = now,
						Trigger = Triggers.BumperPressed,
						TriggerFilter = "FrontLeft"
					};

					await dbService.ContainerManager.TriggerDetailData.AddAsync(trigger);

					triggerGuid = Guid.NewGuid().ToString();
					trigger = new TriggerDetail
					{
						Id = triggerGuid,
						Name = "Front Right Bumper Pressed",
						CreatedBy = "System",
						Created = now,
						Updated = now,
						Trigger = Triggers.BumperPressed,
						TriggerFilter = "FrontRight"
					};

					await dbService.ContainerManager.TriggerDetailData.AddAsync(trigger);

					triggerGuid = Guid.NewGuid().ToString();
					trigger = new TriggerDetail
					{
						Id = triggerGuid,
						Name = "Back Left Bumper Pressed",
						CreatedBy = "System",
						Created = now,
						Updated = now,
						Trigger = Triggers.BumperPressed,
						TriggerFilter = "BackLeft"
					};

					await dbService.ContainerManager.TriggerDetailData.AddAsync(trigger);

					triggerGuid = Guid.NewGuid().ToString();
					trigger = new TriggerDetail
					{
						Id = triggerGuid,
						Name = "Back Right Bumper Pressed",
						CreatedBy = "System",
						Created = now,
						Updated = now,
						Trigger = Triggers.BumperPressed,
						TriggerFilter = "BackRight"
					};

					await dbService.ContainerManager.TriggerDetailData.AddAsync(trigger);

					triggerGuid = Guid.NewGuid().ToString();
					trigger = new TriggerDetail
					{
						Id = triggerGuid,
						Name = "Scruff Touched",
						CreatedBy = "System",
						Created = now,
						Updated = now,
						Trigger = Triggers.CapTouched,
						TriggerFilter = "Scruff"
					};

					await dbService.ContainerManager.TriggerDetailData.AddAsync(trigger);

					triggerGuid = Guid.NewGuid().ToString();
					trigger = new TriggerDetail
					{
						Id = triggerGuid,
						Name = "Chin Touched",
						CreatedBy = "System",
						Created = now,
						Updated = now,
						Trigger = Triggers.CapTouched,
						TriggerFilter = "Chin"
					};

					await dbService.ContainerManager.TriggerDetailData.AddAsync(trigger);

					triggerGuid = Guid.NewGuid().ToString();
					trigger = new TriggerDetail
					{
						Id = triggerGuid,
						Name = "Front Cap Touched",
						CreatedBy = "System",
						Created = now,
						Updated = now,
						Trigger = Triggers.CapTouched,
						TriggerFilter = "Front"
					};

					await dbService.ContainerManager.TriggerDetailData.AddAsync(trigger);

					triggerGuid = Guid.NewGuid().ToString();
					trigger = new TriggerDetail
					{
						Id = triggerGuid,
						Name = "Back Cap Touched",
						CreatedBy = "System",
						Created = now,
						Updated = now,
						Trigger = Triggers.CapTouched,
						TriggerFilter = "Back"
					};

					await dbService.ContainerManager.TriggerDetailData.AddAsync(trigger);

					triggerGuid = Guid.NewGuid().ToString();
					trigger = new TriggerDetail
					{
						Id = triggerGuid,
						Name = "Left Cap Touched",
						CreatedBy = "System",
						Created = now,
						Updated = now,
						Trigger = Triggers.CapTouched,
						TriggerFilter = "Left"
					};

					await dbService.ContainerManager.TriggerDetailData.AddAsync(trigger);

					triggerGuid = Guid.NewGuid().ToString();
					trigger = new TriggerDetail
					{
						Id = triggerGuid,
						Name = "Right Cap Touched",
						CreatedBy = "System",
						Created = now,
						Updated = now,
						Trigger = Triggers.CapTouched,
						TriggerFilter = "Right"
					};

					await dbService.ContainerManager.TriggerDetailData.AddAsync(trigger);

					triggerGuid = Guid.NewGuid().ToString();
					trigger = new TriggerDetail
					{
						Id = triggerGuid,
						Name = "Heard Unknown",
						CreatedBy = "System",
						Created = now,
						Updated = now,
						TriggerFilter = "HeardUnknownSpeech"
					};

					await dbService.ContainerManager.TriggerDetailData.AddAsync(trigger);

					triggerGuid = Guid.NewGuid().ToString();
					trigger = new TriggerDetail
					{
						Id = triggerGuid,
						Name = "Heard Nothing",
						CreatedBy = "System",
						Created = now,
						Updated = now,
						TriggerFilter = "HeardNothing"
					};

					await dbService.ContainerManager.TriggerDetailData.AddAsync(trigger);

					triggerGuid = Guid.NewGuid().ToString();
					trigger = new TriggerDetail
					{
						Id = triggerGuid,
						Name = "Face Seen",
						CreatedBy = "System",
						Created = now,
						Updated = now,
						Trigger = Triggers.FaceRecognized,
						TriggerFilter = ""
					};

					await dbService.ContainerManager.TriggerDetailData.AddAsync(trigger);
				}
				
			}
			catch
			{
				//'ello!?				
			}
		}
		
		private IList<Animation> GetAnimationSeeds()
		{
			IList<Animation> animations = new List<Animation>();

			animations.Add(CreateSeedAnimation("Admire",  
@"IMAGE:e_Admiration.jpg;
HEAD:-5,0,0,200;
ARMS:-89,89,2000;
LED-PATTERN:0,255,0,0,150,0,900, breathe;"
			));

			animations.Add(CreateSeedAnimation("Admire2",  
@"ARMS:0,0,500;
PAUSE:300;
HEAD:-5,-30,0,500;
LED-PATTERN:0,0,255,40,0,112,1200,breathe;
IMAGE:e_Admiration.jpg;"
			));

			animations.Add(CreateSeedAnimation("Angry",  
@"IMAGE:e_Anger.jpg;
LED:255,0,0;
HEAD:0,0,0,500;
ARMS:-45,-45,500;"
			));


			animations.Add(CreateSeedAnimation("Body reset",  
@"IMAGE:e_DefaultContent.jpg;
ARMS:89,89,1000;
HEAD:-5,0,0,1000;
LED-PATTERN:0,0,255,40,0,112,1200,breathe;"
			));

			animations.Add(CreateSeedAnimation("Check it out",  
@"LED-PATTERN:255,255,255,110,110,110,200,breathe;
ARMS:-89,-89,500;
HEAD:-5,5,0,500;
PAUSE:1000;
ARMS:79,79,2000;
HEAD:25,5,0,2000;
PAUSE:2000;
ARMS:-89,-89,2000;
HEAD:-5,5,0,2000;"
			));

			animations.Add(CreateSeedAnimation("Check surrounding fast",  
@"IMAGE:e_ContentRight.jpg;
HEAD:-5,-5,-15,500;
PAUSE:1000;
IMAGE:e_ContentLeft.jpg;
HEAD:-5,5,15,500;
PAUSE:1000;
IMAGE:e_DefaultContent.jpg;
HEAD:-5,0,0,500;"
			));

			animations.Add(CreateSeedAnimation("Check surrounding slow",  
@"LED-PATTERN:0,0,255,40,0,112,1200,breathe;
IMAGE:e_ContentLeft.jpg;
HEAD:-5,0,10,500;
ARMS:89,89,3000;
PAUSE:2000;
IMAGE:e_ContentRight.jpg;
HEAD:-5,0,-10,500;
PAUSE:2000;
IMAGE:e_DefaultContent.jpg;
HEAD:-5,0,0,500;"
			));

			animations.Add(CreateSeedAnimation("Cheers",  
@"ARMS:-89,-89,750;
HEAD:-15,10,-20,1000;
PAUSE:1200;
HEAD:-15,-10,20,1000;
PAUSE:1200;
HEAD:-15,5,0,1000;
ARMS:-89,-89,500;"
			));

			animations.Add(CreateSeedAnimation("Concerned",  
@"IMAGE:e_ApprehensionConcerned.jpg;
LED-PATTERN:0,0,255,40,0,112,1200,breathe;
HEAD:-5,-25,0,500;
ARMS:-89,-89,2000;"
			));

			animations.Add(CreateSeedAnimation("Confused",  
@"IMAGE:e_Terror.jpg;
HEAD:-5,25,0,500;
PAUSE:1500;
HEAD:-5,-25,0,500;
PAUSE:1500;
HEAD:-5,0,0,500;
PAUSE:1000;
ARMS:-89,-89,2000;"
			));

			animations.Add(CreateSeedAnimation("Cry fast",  
@"IMAGE:e_EcstacyHilarious.jpg;
LED-PATTERN:200,200,200,0,0,255,300,breathe;
HEAD:-5,0,30,400;
PAUSE:400;
HEAD:-5,30,30,200;
PAUSE:200;
HEAD:-5,-30,30,200;
PAUSE:200;
HEAD:-5,30,30,200;
PAUSE:200;
HEAD:-5,-30,30,200;
PAUSE:200;
HEAD:-5,0,-30,400;
PAUSE:400;
HEAD:-5,30,-30,200;
PAUSE:200;
HEAD:-5,-30,-30,200;
PAUSE:200;
HEAD:-5,30,-30,200;
PAUSE:200;
HEAD:-5,-30,-30,200;
PAUSE:200;
HEAD:-5,0,0,400;
LED-PATTERN:0,0,255,40,0,112,1200,breathe
IMAGE:e_DefaultContent.jpg;"
			));

			animations.Add(CreateSeedAnimation("Cry slow",  
@"IMAGE:e_EcstacyHilarious.jpg;
LED-PATTERN:200,200,200,0,0,255,400,breathe;
HEAD:-5,0,30,200;
PAUSE:400;
HEAD:-5,25,30,200;
PAUSE:400;
HEAD:-5,-25,30,200;
PAUSE:400;
HEAD:-5,25,30,200;
PAUSE:400;
HEAD:-5,-25,30,200;
PAUSE:400;
HEAD:-5,0,-30,200;
PAUSE:400;
HEAD:-5,25,-30,200;
PAUSE:400;
HEAD:-5,-25,-30,200;
PAUSE:400;
HEAD:-5,25,-30,200;
PAUSE:400;
HEAD:-5,-25,-30,200;
PAUSE:400;
HEAD:-5,0,0,1000;
LED-PATTERN:0,0,255,40,0,112,1200,breathe;
IMAGE:e_DefaultContent.jpg;"
			));

			animations.Add(CreateSeedAnimation("Cute",  
@"IMAGE:e_Joy.jpg;
LED-PATTERN:0,0,255,40,0,112,1200,breathe
LED:255,255,255;
ARMS:-89,-89,1000;
PAUSE:2000;
LED:100,70,255;
ARMS:89,89,2000;
HEAD:-5,25,0,500;"
			));

			animations.Add(CreateSeedAnimation("Dizzy",  
@"IMAGE:e_Disoriented.jpg;
ARMS:30,-20,700;
HEAD:-15,15,15,1000;"
			));

			animations.Add(CreateSeedAnimation("Fear",  
@"IMAGE:e_Fear.jpg;
ARMS:-89,-89,300;
HEAD:-10,0,0,300;"
			));

			animations.Add(CreateSeedAnimation("Fear2",  
@"IMAGE:e_Fear.jpg;
LED-PATTERN:255,255,255,10,10,10,700,breathe;
ARMS:-89,-89,2500;
HEAD:-10,5,0,2500;"
			));

			animations.Add(CreateSeedAnimation("Freefall",  
@"LED:255,0,0;
ARMS:-89,-89,4000;
HEAD:-40,0,0,6000;
PAUSE:6500;
IMAGE:e_JoyGoofy.jpg;
ARMS:0,0,200;
HEAD:0,0,0,500;
LED:0,255,0;"
			));

			animations.Add(CreateSeedAnimation("Grief",  
@"IMAGE:e_Grief.jpg;
ARMS:89,89,2000;
HEAD:10,0,0,3000;
LED-PATTERN:0,0,255,40,0,112,1200,breathe;
PAUSE:2000;"
			));

			animations.Add(CreateSeedAnimation("Head down up nod",  
@"HEAD:10,-5,0,500;
PAUSE:500;
HEAD:-10,-5,0,500;
PAUSE:500;
HEAD:10,-5,0,500;
PAUSE:500;
HEAD:-10,-5,0,500;
PAUSE:500;
HEAD:-5,0,0,500;"
			));

			animations.Add(CreateSeedAnimation("Head nod slow",  
@"LED-PATTERN:0,255,0,0,150,0,900, breathe;
ARMS:-89,-89,3000;
HEAD:5,0,0,500;
PAUSE:700;
HEAD:-5,0,0,500;
PAUSE:700;
HEAD:5,0,0,500;
PAUSE:700;
HEAD:-5,0,0,500;
PAUSE:700;
HEAD:5,0,0,500;
PAUSE:700;
HEAD:-5,0,0,500;
PAUSE:700;
HEAD:5,0,0,500;
PAUSE:700;
HEAD:-5,0,0,500;"
			));

			animations.Add(CreateSeedAnimation("Head up down nod",  
@"HEAD:-15,0,0,500;
PAUSE:500;
HEAD:5,0,0,500;
PAUSE:500;
HEAD:-15,0,0,500;
PAUSE:500;
HEAD:5,0,0,500;
PAUSE:500;
HEAD:-5,0,0,500;
PAUSE:500;"
			));

			animations.Add(CreateSeedAnimation("Hi",  
@"IMAGE:e_Joy.jpg;
ARMS:-89,89,500;
LED-PATTERN:255,255,0,180,235,0,400,breathe"
			));

			animations.Add(CreateSeedAnimation("Hug",  
@"LED-PATTERN:0,0,255,40,0,112,800,breathe;
IMAGE:e_Joy2.jpg;
HEAD:-15,0,0,100;
ARMS:-89,-89,1000;"
			));

			animations.Add(CreateSeedAnimation("Hug2",  
@"IMAGE:e_Joy.jpg;
ARMS:0,0,700;
HEAD:-5,10,0,1000;"
			));

			animations.Add(CreateSeedAnimation("Jump",  
@"IMAGE:e_Terror.jpg;
LED:255,0,0;
ARMS:-30,-30,500;
HEAD:-35,0,0,500;
PAUSE:500;
HEAD:-5,0,0,700;
PAUSE:500;
ARMS:29,29,700;"
			));

			animations.Add(CreateSeedAnimation("Listen",  
@"IMAGE:e_Surprise.jpg;
LED-PATTERN:255,255,0,180,235,0,1200,breathe;
HEAD:-6,30,0,1000;
PAUSE:2500;
HEAD:-5,0,0,500;
IMAGE:e_DefaultContent.jpg;
LED-PATTERN:0,0,255,40,0,112,1200,breathe;"
			));
			

			animations.Add(CreateSeedAnimation("Look at hands",  
@"IMAGE:e_Amazement.jpg;
HEAD:15,5,35,1500;
PAUSE:1500;
HEAD:15,-5,-35,1500;
PAUSE:1500;
IMAGE:e_Terror.jpg;
HEAD:-5,0,0,500;"
			));

			animations.Add(CreateSeedAnimation("Look left",  
@"IMAGE:e_ContentLeft.jpg;
HEAD:-5,5,10,500;
ARMS:-89,89,700;"
			));

			animations.Add(CreateSeedAnimation("Look right",  
@"IMAGE:e_ContentRight.jpg;
HEAD:5,-5,-10,500;
ARMS:89,-89,700;"
			));

			animations.Add(CreateSeedAnimation("Look right then left",  
@"LED:255,255,255;
ARMS:89,-89,500;
HEAD:-5,5,-25,300;
PAUSE:1500;
ARMS:-89,89,500;
HEAD:-5,5,25,300;
PAUSE:1500;
ARMS:89,89,500;
HEAD:-5,0,0,300;"
			));

			animations.Add(CreateSeedAnimation("Look up left",  
@"IMAGE:e_Surprise.jpg;
LED-PATTERN:255,255,0,180,235,0,300,breathe
ARMS:-89,89,1000;
HEAD:-25,-10,50,1500;
PAUSE:3000;
ARMS:89,89,1500;
HEAD:-5,0,0,1500;
LED-PATTERN:0,0,255,40,0,112,1200,breathe;
PAUSE:1500;
IMAGE:e_DefaultContent.jpg;"
			));

			animations.Add(CreateSeedAnimation("Love",  
@"IMAGE:e_Love.jpg;
LED-PATTERN:255,0,0,150,0,0,1200,breathe;
ARMS:89,-89,1000;
HEAD:-5,-10,3,700;"
			));

			animations.Add(CreateSeedAnimation("Mad",  
@"IMAGE:e_DefaultContent.jpg;
LED-PATTERN:0,0,255,40,0,112,1200,breathe;
PAUSE:1000;
HEAD:-10,20,15,100;
ARMS:89,89,1000;
IMAGE:e_Anger.jpg;
LED:180,0,0;
PAUSE:500;
HEAD:-10,-20,-15,100;
PAUSE:500;
HEAD:-5,0,0,100;"
			));

			animations.Add(CreateSeedAnimation("Mad2",  
@"ARMS:-20,20,200;
HEAD:-5,-25,0,500;
PAUSE:500;
ARMS:20,-20,200;
HEAD:-5,25,0,500;
PAUSE:500;
ARMS:-20,20,200;
HEAD:-5,-25,0,500;
PAUSE:500;
ARMS:20,-20,200;
HEAD:-5,25,0,500;
PAUSE:500;
ARMS:-20,20,200;
HEAD:-5,-25,0,500;
PAUSE:500;
ARMS:20,-20,200;
HEAD:-5,25,0,500;
PAUSE:500;
ARMS:89,89,400;
HEAD:-5,0,0,500;"
			));

			animations.Add(CreateSeedAnimation("Mad3",  
@"IMAGE:e_Rage.jpg;
LED-PATTERN:0,255,0,0,0,0,200,blink;
HEAD:5,0,0,500;
PAUSE:700;
HEAD:-10,0,0,500;
PAUSE:700;
HEAD:5,0,0,500;
PAUSE:700;
HEAD:-10,0,0,500;
PAUSE:700;
HEAD:5,0,0,500;
PAUSE:700;
HEAD:-10,0,0,500;"
			));

			animations.Add(CreateSeedAnimation("Mad4",  
@"IMAGE:e_Rage.jpg
LED-PATTERN:255,255,0,180,235,0,100,blink;
ARMS:0,0,750;
HEAD:-5,0,0,500;"
			));

			animations.Add(CreateSeedAnimation("Oh wow",  
@"IMAGE:e_EcstacyStarryEyed.jpg;
LED-PATTERN:255,255,0,180,235,0,100,blink;
HEAD:0,0,0,1000;
ARMS:-89,-89,2000;
PAUSE:1000;
HEAD:-10,5,0,1000;
PAUSE:1500;
LED-PATTERN:0,0,255,40,0,112,1200,breathe;"
			));

			animations.Add(CreateSeedAnimation("Oops",  
@"IMAGE:e_Disoriented.jpg;
ARMS:89,89,2000;
LED-PATTERN:255,0,0,0,0,0,400,blink;"
			));

			animations.Add(CreateSeedAnimation("Party",  
@"IMAGE:e_EcstacyStarryEyed.jpg;
ARMS:0,0,1000;
HEAD:-5,0,0,1000;
LED-PATTERN:255,0,0,0,0,255,300,blink;
PAUSE:4000;
LED-PATTERN:0,0,255,40,0,112,1200,breathe;
IMAGE:e_Joy2.jpg;
ARMS:89,89,1000;"
			));

			animations.Add(CreateSeedAnimation("Sad",  
@"IMAGE:e_RemorseShame.jpg;
ARMS:89,89,2500;
HEAD:-5,0,0,2000;"
			));

			animations.Add(CreateSeedAnimation("Sad2",  
@"IMAGE:e_Sadness.jpg;
LED-PATTERN:0,0,255,40,0,112,1200,breathe;
ARMS:89,-89,2000;
HEAD:-5,25,0,500;"
			));

			animations.Add(CreateSeedAnimation("Sad3",  
@"IMAGE:e_Grief.jpg;
LED-PATTERN:0,0,255,40,0,112,1200,breathe;
ARMS:89,89,3000;
HEAD:5,0,0,3000;"
			));

			animations.Add(CreateSeedAnimation("Scold",  
@"LED-PATTERN:255,200,0,150,40,0,300,breathe;
ARMS:-89,89,400;
HEAD:-5,-20,0,400;
PAUSE:2000;
ARMS:89,-89,400;
HEAD:-5,20,0,400;
PAUSE:2000;
ARMS:89,89,400;
HEAD:-5,0,0,400;"
			));

			animations.Add(CreateSeedAnimation("Self destruct",  
@"IMAGE:e_Terror.jpg;
HEAD:-5,0,0,100;
LED-PATTERN:255,0,0,0,0,0,100,blink;
PAUSE:2000;
IMAGE:e_Terror2.jpg;"
			));

			animations.Add(CreateSeedAnimation("Sleep",  
@"IMAGE:e_SleepingZZZ.jpg;
LED-PATTERN:0,0,255,40,0,155,1200,breathe;
HEAD:-25,-30,40,1500;
ARMS:89,89,1500;"
			));

			animations.Add(CreateSeedAnimation("Surprise",  
@"IMAGE:e_Surprise.jpg;
LED-PATTERN:255,255,0,180,235,0,100,blink;
ARMS:-89,-89,1000;"
			));

			animations.Add(CreateSeedAnimation("Surprise2",  
@"LED-PATTERN:0,0,255,40,0,112,200,blink;
IMAGE:e_Surprise.jpg;
HEAD:-10,0,0,300;
ARMS:-89,-89,700;"
			));

			animations.Add(CreateSeedAnimation("Surprise3",  
@"IMAGE:e_ContentLeft.jpg;
PAUSE:700;
IMAGE:e_ContentRight.jpg;
PAUSE:700;
IMAGE:e_Surprise.jpg;"
			));

			animations.Add(CreateSeedAnimation("Suspicious",  
@"LED-PATTERN:255,0,0,100,0,0,300,blink;
IMAGE:e_Contempt.jpg;
ARMS:89,-89,700;
HEAD:-5,-20,0,500;"
			));

			animations.Add(CreateSeedAnimation("Terror",  
@"IMAGE:e_Terror.jpg;
LED-PATTERN:0,0,255,40,0,112,1200,breathe;
ARMS:-10,-10,300;
HEAD:-5,0,0,300;"
			));

			animations.Add(CreateSeedAnimation("That is correct",  
@"IMAGE:e_Joy2.jpg;
LED:85,0,255;
ARMS:-89,89,500;
HEAD:-10,0,-15,500;"
			));

			animations.Add(CreateSeedAnimation("Think",  
@"IMAGE:e_ContentRight.jpg;
LED-PATTERN:255,255,0,180,235,0,100,blink;
HEAD:-15,-10,-10,500;
PAUSE:3000;
ARMS:89,89,700;
HEAD:-5,0,0,700;
LED-PATTERN:0,0,255,40,0,112,1200,breathe;
IMAGE:e_DefaultContent.jpg;"
			));

			animations.Add(CreateSeedAnimation("Told you",  
@"ARMS:89,-89,500;
HEAD:-5,15,10,500;"
			));

			animations.Add(CreateSeedAnimation("Wake up",  
@"LED-PATTERN:0,0,0,0,255,0,4200,breathe;
ARMS:-89,-89,4200;
HEAD:15,0,0,500;
PAUSE:700;
HEAD:-5,0,0,3000;
IMAGE:e_Sleepy4.jpg;
PAUSE:700;
IMAGE:e_Sleepy3.jpg;
PAUSE:700;
IMAGE:e_Sleepy2.jpg;
PAUSE:700;
IMAGE:e_Sleepy.jpg;
PAUSE:700;
IMAGE:e_DefaultContent.jpg;
LED-PATTERN:0,255,0,0,150,0,900,breathe;"
			));

			animations.Add(CreateSeedAnimation("Walk angry",  
@"IMAGE:e_Anger.jpg;
LED:255,0,0;
ARMS:-39,39,500;
HEAD:10,5,25,1000;
PAUSE:1000;
ARMS:39,-39,500;
HEAD:10,-5,-25,1000;
PAUSE:1000;
ARMS:-39,39,500;
HEAD:10,5,25,1000;
PAUSE:1000;
ARMS:39,-39,500;
HEAD:10,-5,-25,1000;
PAUSE:1000;
ARMS:-39,39,500;
HEAD:10,5,25,1000;
PAUSE:1000;
ARMS:39,-39,500;
HEAD:10,-5,-25,1000;"
			));

			animations.Add(CreateSeedAnimation("Walk fast",  
@"IMAGE:e_ContentLeft.jpg;
ARMS:30,89,500;
HEAD:-5,-5,15,500;
PAUSE:1000;
IMAGE:e_ContentRight.jpg;
ARMS:89,30,500;
HEAD:-5,5,-15,500;
PAUSE:1000;
IMAGE:e_ContentLeft.jpg;
ARMS:30,89,500;
HEAD:-5,-5,15,500;
PAUSE:1000;
IMAGE:e_ContentRight.jpg;
ARMS:89,30,500;
HEAD:-5,5,-15,500;"
			));

			animations.Add(CreateSeedAnimation("Walk happy",  
@"IMAGE:e_Joy.jpg;
LED-PATTERN:0,255,0,0,150,0,300,blink;
ARMS:-89,89,700;
HEAD:-15,-10,-25,700;
PAUSE:1100;
ARMS:89,-89,700;
HEAD:-15,10,25,700;
PAUSE:1100;
ARMS:-89,89,700;
HEAD:-15,-10,-25,700;
PAUSE:1100;
ARMS:89,-89,700;
HEAD:-15,10,25,700;"
			));

			animations.Add(CreateSeedAnimation("Walk slow",  
@"IMAGE:e_ContentLeft.jpg;
ARMS:30,89,500;
HEAD:-5,-5,15,500;
PAUSE:1500;
IMAGE:e_ContentRight.jpg;
ARMS:89,30,500;
HEAD:-5,5,-15,500;
PAUSE:1500;
IMAGE:e_ContentLeft.jpg;
ARMS:30,89,500;
HEAD:-5,-5,15,500;
PAUSE:1500;
IMAGE:e_ContentRight.jpg;
ARMS:89,30,500;
HEAD:-5,5,-15,500;"
			));

			animations.Add(CreateSeedAnimation("Warn",  
@"IMAGE:e_Contempt.jpg;
LED:255,0,0;
HEAD:-5,-25,0,500;
PAUSE:1000;
ARMS:89,-89,700;
PAUSE:2000;
HEAD:-5,0,0,4000;
ARMS:89,-5,500;
PAUSE:500;
ARMS:89,-89,500;
PAUSE:500;
ARMS:89,-5,500;
PAUSE:500;
ARMS:89,-89,500;
PAUSE:500;
ARMS:89,-5,500;
PAUSE:500;
ARMS:89,-89,500;"
			));

			animations.Add(CreateSeedAnimation("Worry",  
@"LED-PATTERN:0,0,255,40,0,112,1200,breathe;
IMAGE:e_ApprehensionConcerned.jpg;
ARMS:29,29,1000;
HEAD:10,0,0,1000;"
			));

			animations.Add(CreateSeedAnimation("Yes",  
@"IMAGE:e_Joy.jpg;
LED:0,255,0;
ARMS:-89,89,2000;
HEAD:5,0,0,1000;
PAUSE:1000;
HEAD:-5,0,0,1000;"
			));

			animations.Add(CreateSeedAnimation("Yes2",  
@"LED-PATTERN:0,0,255,40,0,112,1200,breathe;
ARMS:-89,-89,700;
HEAD:-5,20,0,500;
IMAGE:e_Admiration.jpg;"
			));

			return animations;
		}

		private Animation CreateSeedAnimation(string name, string script)
		{
			DateTime now = DateTime.UtcNow;
			return new Animation
			{
				Id = Guid.NewGuid().ToString(),
				Name = name,
				Silence = false,
				Updated = now,
				Created = now,
				CreatedBy = "System",
				RepeatScript = false,
				AnimationScript = @$"{script}"
			};
		}

	}
}