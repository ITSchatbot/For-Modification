// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WelcomeUser.Dialogs;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// The Startup class configures services and the app's request pipeline.
    /// </summary>
    public class Startup
    {
        private ILoggerFactory _loggerFactory;
        private bool _isProduction = false;

        public Startup(IHostingEnvironment env)
        {
            _isProduction = env.IsProduction();

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        /// <summary>
        /// Gets the configuration that represents a set of key/value application configuration properties.
        /// </summary>
        /// <value>
        /// The <see cref="IConfiguration"/> that represents a set of key/value application configuration properties.
        /// </value>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">Specifies the contract for a <see cref="IServiceCollection"/> of service descriptors.</param>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/dependency-injection"/>
        /// <seealso cref="https://docs.microsoft.com/en-us/azure/bot-service/bot-service-manage-channels?view=azure-bot-service-4.0"/>
        public void ConfigureServices(IServiceCollection services)
        {
            // Memory Storage is for local bot debugging only. When the bot
            // is restarted, everything stored in memory will be gone.
            IStorage dataStore = new MemoryStorage();
            var conversationState = new ConversationState(dataStore);
            var userState2 = new UserState(dataStore);

            // For production bots use the Azure Blob or
            // Azure CosmosDB storage providers. For the Azure
            // based storage providers, add the Microsoft.Bot.Builder.Azure
            // Nuget package to your solution. That package is found at:
            // https://www.nuget.org/packages/Microsoft.Bot.Builder.Azure/
            // Un-comment the following lines to use Azure Blob Storage
            // // Storage configuration name or ID from the .bot file.
            // const string StorageConfigurationId = "<STORAGE-NAME-OR-ID-FROM-BOT-FILE>";
            // var blobConfig = botConfig.FindServiceByNameOrId(StorageConfigurationId);
            // if (!(blobConfig is BlobStorageService blobStorageConfig))
            // {
            //    throw new InvalidOperationException($"The .bot file does not contain an blob storage with name '{StorageConfigurationId}'.");
            // }
            // // Default container name.
            // const string DefaultBotContainer = "<DEFAULT-CONTAINER>";
            // var storageContainer = string.IsNullOrWhiteSpace(blobStorageConfig.Container) ? DefaultBotContainer : blobStorageConfig.Container;
            // IStorage dataStore = new Microsoft.Bot.Builder.Azure.AzureBlobStorage(blobStorageConfig.ConnectionString, storageContainer);
            // Create and add conversation state.
            //var conversationState = new ConversationState(dataStore);
            //services.AddSingleton(conversationState);
            // The dialogset will need a state store accessor. Since the state accessor is only going to be used by the FoodBotDialogSet, use  
            // AddSingleton to register and inline create the FoodBotDialogSet, at the same time creating the the property accessor in the
            // conversation state
            //services.AddSingleton(sp => new EchoBotDialogSet(conversationState.CreateProperty<DialogState>("ECHOBOTDIALOGSTATE")));
            services.AddBot<WelcomeUserBot>(options =>
            {
                var appId = Configuration.GetSection("MicrosoftAppId").Value;
                var appPassword = Configuration.GetSection("MicrosoftAppPassword").Value;
                options.CredentialProvider = new SimpleCredentialProvider(appId, appPassword);
                // Catches any errors that occur during a conversation turn and logs them to currently
                // configured ILogger.
                ILogger logger = _loggerFactory.CreateLogger<WelcomeUserBot>();
                options.OnTurnError = async (context, exception) =>
                {
                    logger.LogError($"Exception caught : {exception}");
                    await context.SendActivityAsync("Sorry, it looks like something went wrong.");
                };
               // IStorage dataStore2 = new MemoryStorage();
                // var userState = new UserState(dataStore2);
                options.State.Add(userState2);
            });
            // Create and register state accessors.
            // Accessors created here are passed into the IBot-derived class on every turn.
            services.AddSingleton<WelcomeUserStateAccessors>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<BotFrameworkOptions>>().Value;
                if (options == null)
                {
                    throw new InvalidOperationException("BotFrameworkOptions must be configured prior to setting up the State Accessors");
                }
                var userState = options.State.OfType<UserState>().FirstOrDefault();
                if (userState == null)
                {
                    throw new InvalidOperationException("UserState must be defined and added before adding user-scoped state accessors.");
                }
                // Create the custom state accessor.
                // State accessors enable other components to read and write individual properties of state.
                var accessors = new WelcomeUserStateAccessors(userState, conversationState)
                {
                    WelcomeUserState = userState.CreateProperty<WelcomeUserState>(WelcomeUserStateAccessors.WelcomeUserName),
                };
                return accessors;
            });

            services.AddSingleton<BotState>(conversationState);
            // The dialogset will need a state store accessor. Since the state accessor is only going to be used by the FoodBotDialogSet, use  
            // AddSingleton to register and inline create the FoodBotDialogSet, at the same time creating the the property accessor in the
            // conversation state
            services.AddSingleton(sp => new WelcomeUserBotDialogSet(conversationState.CreateProperty<DialogState>("WELCOMEBOTDIALOGSTATE2")));
            services.AddSingleton<IStatePropertyAccessor<DialogState>>(conversationState.CreateProperty<DialogState>("WELCOMEBOTDIALOGSTATE"));
            services.AddSingleton<WelcomeUserBotDialogSet>();

        }


        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseBotFramework();
        }
    }
}
