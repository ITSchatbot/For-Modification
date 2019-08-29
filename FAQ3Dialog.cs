using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.AI.QnA;
using System.Threading.Tasks;
using System.Threading;

namespace WelcomeUser.Dialogs
{
    public class FAQ3Dialog : ComponentDialog
    {

        public static string StartDialogId => "mainMenuDialog";
        public static readonly string QnAMakerKey = "QnABot";

        private const string FAQPROMPT = "FAQPROMPT";

        private const string WelcomeText = @"This bot will introduce you to QnA Maker.
                                         Ask a question to get started.";

        private QnAMaker qnaMaker;
        public FAQ3Dialog(string dialogId) : base(dialogId)
        {

            // ID of the child dialog that should be started anytime the component is started.
            this.InitialDialogId = dialogId;
            this.AddDialog(new ChoicePrompt("choicePrompt"));
            this.AddDialog(new TextPrompt(FAQPROMPT));

            var qnAMakerEndpoint = new QnAMakerEndpoint();
            qnAMakerEndpoint.EndpointKey = "d069e7cb-0fe1-46a0-8d91-19aac112b7fb";
            qnAMakerEndpoint.Host = "https://kbforpilot.azurewebsites.net/qnamaker";
            qnAMakerEndpoint.KnowledgeBaseId = "595a0d27-b768-4a75-8710-c5fa6629d1f8";

            qnaMaker = new QnAMaker(qnAMakerEndpoint);

            // Adds a waterfall dialog that prompts users with the top level menu to the dialog set.
            // Define the steps of the waterfall dialog and add it to the set.
            this.AddDialog(new WaterfallDialog(
                dialogId,
                new WaterfallStep[]
                {
                    this.PromptForQuestion,
                    this.AnswerQuestion
                }));
        }

        private async Task<DialogTurnResult> AnswerQuestion(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var answers = await qnaMaker.GetAnswersAsync(stepContext.Context);

            if (answers.Length == 0)
            {
                await stepContext.Context.SendActivityAsync("Sorry, I don't know the answer to that one.", cancellationToken: cancellationToken).ConfigureAwait(false);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            await stepContext.Context.SendActivityAsync(answers[0].Answer).ConfigureAwait(false);
            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForQuestion(WaterfallStepContext stepContext, System.Threading.CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(
                FAQPROMPT,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("聞きたいことを記入してください。"),
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}