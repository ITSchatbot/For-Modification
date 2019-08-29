// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using WelcomeUser.Dialogs;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service. Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    public class WelcomeUserBot : IBot
    {
        

        // Messages sent to the user.
        private const string WelcomeMessage = @"こんにちは。ユーザーサポートチームのChatbotちゃんです！";

        //private const string InfoMessage = @"It's great to have you!";

        //private const string PatternMessage = @"Feel free to ask for help or type 'intro' to see what I can help you with";

        private DialogSet _dialogSet;
        private readonly BotState _botState;


        // The bot state accessor object. Use this to access specific state properties.
        private readonly WelcomeUserStateAccessors _welcomeUserStateAccessors;

        private QnAMaker qnaMaker;

        /// <summary>
        /// Initializes a new instance of the <see cref="WelcomeUserBot"/> class.
        /// </summary>
        /// <param name="statePropertyAccessor"> Bot state accessor object.</param>
        public WelcomeUserBot(WelcomeUserStateAccessors statePropertyAccessor, WelcomeUserBotDialogSet dialogSet, BotState botState)
        {
            _welcomeUserStateAccessors = statePropertyAccessor ?? throw new System.ArgumentNullException("state accessor can't be null");
            _dialogSet = dialogSet ?? throw new ArgumentNullException(nameof(dialogSet));
            _botState = botState ?? throw new ArgumentNullException(nameof(botState));
        }


        /// <summary>
        /// Every conversation turn for our WelcomeUser Bot will call this method, including
        /// any type of activities such as ConversationUpdate or ContactRelationUpdate which
        /// are sent when a user joins a conversation.
        /// This bot doesn't use any dialogs; it's "single turn" processing, meaning a single
        /// request and response.
        /// This bot uses UserState to keep track of first message a user sends.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = new CancellationToken())
        {
            var dialogContext = await _dialogSet.CreateContextAsync(turnContext, cancellationToken);

            // use state accessor to extract the didBotWelcomeUser flag
            var didBotWelcomeUser = await _welcomeUserStateAccessors.WelcomeUserState.GetAsync(turnContext, () => new WelcomeUserState());

            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                if (dialogContext.ActiveDialog != null)
                {
                    await dialogContext.ContinueDialogAsync(cancellationToken);
                }
                else
                {
                    // Your bot should proactively send a welcome message to a personal chat the first time
                    // (and only the first time) a user initiates a personal chat with your bot.
                    if (didBotWelcomeUser.DidBotWelcomeUser == false)
                    {
                        didBotWelcomeUser.DidBotWelcomeUser = true;

                        // Update user state flag to reflect bot handled first user interaction.
                        await _welcomeUserStateAccessors.WelcomeUserState.SetAsync(turnContext, didBotWelcomeUser);
                        await _welcomeUserStateAccessors.UserState.SaveChangesAsync(turnContext);

                        // the channel should sends the user name in the 'From' object
                        var userName = turnContext.Activity.From.Name;

                        //await turnContext.SendActivityAsync($"We have a ton of activities lined up! Feel free to type 'intro' to see how I can help you", cancellationToken: cancellationToken);

                        // await turnContext.SendActivityAsync($"It is a good practice to welcome the user and provide personal greeting. For example, welcome {userName}.", cancellationToken: cancellationToken);
                    }
                    await dialogContext.BeginDialogAsync("mainMenuDialog", null, cancellationToken);
                }
            }

            // Greet when users are added to the conversation.
            // Note that all channels do not send the conversation update activity.
            // If you find that this bot works in the emulator, but does not in
            // another channel the reason is most likely that the channel does not
            // send this activity.
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                if (turnContext.Activity.MembersAdded != null)
                {
                    // Iterate over all new members added to the conversation
                    foreach (var member in turnContext.Activity.MembersAdded)
                    {
                        // Greet anyone that was not the target (recipient) of this message
                        // the 'bot' is the recipient for events from the channel,
                        // turnContext.Activity.MembersAdded == turnContext.Activity.Recipient.Id indicates the
                        // bot was added to the conversation.
                        if (member.Id != turnContext.Activity.Recipient.Id)
                        {
                            await turnContext.SendActivityAsync($"{WelcomeMessage}", cancellationToken: cancellationToken);
                            //await turnContext.SendActivityAsync(InfoMessage, cancellationToken: cancellationToken);
                            //await turnContext.SendActivityAsync(PatternMessage, cancellationToken: cancellationToken);
                        }
                    }
                }
            }
            else
            {
                // Default behavior for all other type of activities.
                //await turnContext.SendActivityAsync($"{turnContext.Activity.Type} activity detected");
                if (dialogContext.ActiveDialog != null)
                {
                    await dialogContext.ContinueDialogAsync(cancellationToken);
                }
                else
                {
                    await dialogContext.BeginDialogAsync("mainMenuDialog", null, cancellationToken);
                }
            }

            // save any state changes made to your state objects.
            await _welcomeUserStateAccessors.UserState.SaveChangesAsync(turnContext);
            // Always persist changes at the end of every turn, here this is the dialog state in the conversation state.
            await _botState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Sends an adaptive card greeting.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        private static async Task SendIntroCardAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var response = turnContext.Activity.CreateReply();

            var card = new HeroCard();
            card.Title = "Welcome to Bot Framework!";
            card.Text = @"Welcome to Welcome Users bot sample! This Introduction card
                         is a great way to introduce your Bot to the user and suggest
                         some things to get them started. We use this opportunity to
                         recommend a few next steps for learning more creating and deploying bots.";
            card.Images = new List<CardImage>() { new CardImage("https://aka.ms/bf-welcome-card-image") };
            card.Buttons = new List<CardAction>()
            {
                new CardAction(ActionTypes.OpenUrl, "Get an overview", null, "Get an overview", "Get an overview", "https://docs.microsoft.com/en-us/azure/bot-service/?view=azure-bot-service-4.0"),
                new CardAction(ActionTypes.OpenUrl, "Ask a question", null, "Ask a question", "Ask a question", "https://stackoverflow.com/questions/tagged/botframework"),
                new CardAction(ActionTypes.OpenUrl, "Learn how to deploy", null, "Learn how to deploy", "Learn how to deploy", "https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-deploy-azure?view=azure-bot-service-4.0"),
            };

            response.Attachments = new List<Attachment>() { card.ToAttachment() };
            await turnContext.SendActivityAsync(response, cancellationToken);
        }

        private static async Task SendSuggestedActionsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var reply = turnContext.Activity.CreateReply("下記に関しては、私が案内できますよ！何について聞きたいか、下記のボタンを押してください。");

            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction() { Title = "アナウンス関連", Type = ActionTypes.ImBack, Value = "アナウンス関連ですね。聞きたいことを記入してください。" },

                    new CardAction() { Title = "Redmine関連", Type = ActionTypes.ImBack, Value = "Redmine関連ですね。聞きたいことを記入してください。" },

                    new CardAction() { Title = "社内情報", Type = ActionTypes.ImBack, Value = "社内情報ですね。聞きたいことを記入してください。" },
                },
            };

            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
    }
}