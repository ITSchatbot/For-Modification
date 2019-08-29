using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WelcomeUser.Dialogs;
using Microsoft.Bot.Schema;



namespace WelcomeUser.Dialogs
{
    public class MainMenuDialogs : ComponentDialog
    {
        const string MENUPROMPT = "menuPrompt";
        const string FAQDIALOG = "faq1Dialog";
        const string FAQ2DIALOG = "faq2Dialog";
        const string FAQ3DIALOG = "faq3Dialog";
        private object turnContext;
        private object reply;

        public object UXHelpers { get; private set; }

        public MainMenuDialogs(string dialogId) : base(dialogId)
        {
            // ID of the child dialog that should be started anytime the component is started.
            this.InitialDialogId = dialogId;

            // Adds a waterfall dialog that prompts users with the top level menu to the dialog set.
            // Define the steps of the waterfall dialog and add it to the set.
            this.AddDialog(new WaterfallDialog(
                dialogId,
                steps: new WaterfallStep[]
                {
                    PromptForMenuAsync,
                    HandleMenuResultAsync,
                    //HeroCardDisplayTestAsync,
                    ResetDialogAsync,
                }));

            this.AddDialog(new ChoicePrompt(MENUPROMPT));
            this.AddDialog(new FAQDialog(FAQDIALOG));
            this.AddDialog(new FAQ2Dialog(FAQ2DIALOG));
            this.AddDialog(new FAQ3Dialog(FAQ3DIALOG));
            //this.AddDialog(new DonateFoodDialog(DONATEFOODDIALOG));
            //this.AddDialog(new FindFoodDialog(FINDFOODDIALOG));
            //this.AddDialog(new ContactDialog(CONTACTDIALOG));
        }

        private async Task<DialogTurnResult> PromptForMenuAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(MENUPROMPT,
                new PromptOptions
                {
                    Choices = ChoiceFactory.ToChoices(new List<string> {"アナウンス関連", "Redmine関連", "社内情報"}),
                    Prompt = MessageFactory.Text("下記に関しては、私が案内できますよ！何について聞きたいか、数字を記入してください。"),
                    RetryPrompt = MessageFactory.Text("I'm sorry, that wasn't a valid response. Please select one of the options")
                },
                cancellationToken).ConfigureAwait(false);
        }


        private async Task<DialogTurnResult> HandleMenuResultAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var choice = ((FoundChoice)stepContext.Result).Value;

            switch (choice)
            {

                case "アナウンス関連":
                    return await stepContext.BeginDialogAsync(FAQDIALOG, null, cancellationToken).ConfigureAwait(false); ;
                case "Redmine関連":
                    return await stepContext.BeginDialogAsync(FAQ2DIALOG, null, cancellationToken).ConfigureAwait(false); ;
                case "社内情報":
                    return await stepContext.BeginDialogAsync(FAQ3DIALOG, null, cancellationToken).ConfigureAwait(false); ;

                default:
                    break;

            }

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> ResetDialogAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.ReplaceDialogAsync(this.InitialDialogId, null, cancellationToken).ConfigureAwait(false); ;
        }
    }
}