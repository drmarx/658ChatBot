using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.Dialogs;

namespace _658ChatBot {
    [BotAuthentication]
    public class MessagesController : ApiController {
        /// <summary>
        /// POST: api/Messages
        /// calls EchoDialog
        /// This code accepts messages and counts them
        /// if the user sends "reset" it will ask to confirm, then reset count
        /// Also greets and says goodbye to user
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity){
            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
            if (activity.Type == ActivityTypes.Message) {
                await Conversation.SendAsync(activity, () => new EchoDialog()); // bot builder style
            }
            else if (activity.Type == ActivityTypes.ConversationUpdate && activity.MembersAdded[0] != null && activity.MembersAdded[0].Name != "Bot") { // greets user when they join
                Activity reply = activity.CreateReply($"Hi! I'm the CS658 ChatBot.");
                await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        [Serializable]
        public class EchoDialog : IDialog<object>
        {
            protected int count = 1;
            public async Task StartAsync(IDialogContext context)
            {
                context.Wait(MessageReceivedAsync);
            }
            public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
            {
                var message = await argument;
                if (message.Text == "reset")
                { // resets count when requested. asks for confirmation.
                    PromptDialog.Confirm(
                        context,
                        AfterResetAsync,
                        "Are you sure you want to reset the count?",
                        "Didn't get that!",
                        promptStyle: PromptStyle.None); // sends result of confirmation to reset handler
                }
                else if (message.Text == "goodbye")
                { // says goodbye and exits chat (not gracefully)
                    await context.PostAsync("Goodbye!");
                    context.Wait(MessageReceivedAsync);
                    Environment.Exit(0);
                }
                else if (message.Text.Contains("email"))
                {
                    PromptDialog.Confirm(
                        context,
                        AfterEmailAsync,
                        "You are having trouble with your email?",
                        "My apologies. I will contact a support tech. Standby.",
                        promptStyle: PromptStyle.Auto);
                }
                else
                {
                    await context.PostAsync($"{this.count++}: You said {message.Text}"); // updates count and echoes user message
                    context.Wait(MessageReceivedAsync);
                }
            }
            public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
            { // conditional reset handler
                var confirm = await argument;
                if (confirm)
                {
                    this.count = 1;
                    await context.PostAsync($"{this.count}: Reset count.");
                }
                else
                {
                    await context.PostAsync("Did not reset count.");
                }
                context.Wait(MessageReceivedAsync);
            }
            public async Task AfterEmailAsync(IDialogContext context, IAwaitable<bool> argument)
            {
                var confirm = await argument;
                if (confirm)
                {
                    PromptDialog.Confirm(
                        context,
                        AfterITAsync,
                        "Are you using the Outlook client or the website Outlook.Office365.com?",
                        "My apologies. I will contact a support tech. Standby.",
                        promptStyle: PromptStyle.Keyboard);
                }
                else
                {
                    await context.PostAsync("My apologies. Please restate request.");
                }
            }
            public async Task AfterITAsync(IDialogContext context, IAwaitable<bool> argument)
            {
                var confirm = await argument;
                if (confirm)
                {
                    await context.PostAsync("Connecting you to a support tech. Standby.");
                }
                else
                {
                    await context.PostAsync("This issue falls under UITS. Please contact them via <way>");
                }
            }
        }

        private Activity HandleSystemMessage(Activity message){
            if (message.Type == ActivityTypes.DeleteUserData){
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate){
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate){
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing){
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping){
            }

            return null;
        }
    }
}