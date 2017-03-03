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
            else if (activity.Type == ActivityTypes.ConversationUpdate) { // greets user DEBUG: displays twice, stop this
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
        public class EchoDialog : IDialog<object> {
            protected int count = 1;
            public async Task StartAsync(IDialogContext context){
                context.Wait(MessageReceivedAsync);
            }
            public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument){
                var message = await argument;
                if (message.Text == "reset"){ // resets count when requested. asks for confirmation.
                    PromptDialog.Confirm(
                        context,
                        AfterResetAsync,
                        "Are you sure you want to reset the count?",
                        "Didn't get that!",
                        promptStyle: PromptStyle.None); // sends result of confirmation to reset handler
                }
                else if (message.Text == "goodbye"){ // says goodbye and exits chat (not gracefully)
                    await context.PostAsync("Goodbye!");
                    context.Wait(MessageReceivedAsync);
                    Environment.Exit(0);
                }
                else {
                    await context.PostAsync($"{this.count++}: You said {message.Text}"); // updates count and echoes user message
                    context.Wait(MessageReceivedAsync);
                }
            }
            public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument){ // conditional reset handler
                var confirm = await argument;
                if (confirm){
                    this.count = 1;
                    await context.PostAsync($"{this.count}: Reset count.");
                }
                else{
                    await context.PostAsync("Did not reset count.");
                }
                context.Wait(MessageReceivedAsync);
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