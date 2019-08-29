
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace WelcomeUser.Dialogs
{
    public class WelcomeUserBotDialogSet : DialogSet
    {
        public static string StartDialogId => "mainMenuDialog";

        public WelcomeUserBotDialogSet(IStatePropertyAccessor<DialogState> dialogStatePropertyAccessor)
            : base(dialogStatePropertyAccessor)
        {
            // Add the top-level dialog
            Add(new MainMenuDialogs(StartDialogId));
        }
    }
}
