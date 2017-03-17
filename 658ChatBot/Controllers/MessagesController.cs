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
using System.Collections.Generic;

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
            if (activity.Type == ActivityTypes.Message){
                await Conversation.SendAsync(activity, () => new HelpDialog()); // bot builder style
            }
            else if (activity.Type == ActivityTypes.ConversationUpdate && activity.MembersAdded[0] != null && activity.MembersAdded[0].Name != "Bot"){ // greets user when they join
                var reply = activity.CreateReply($"Hi! I'm the CS658 ChatBot.");
                await connector.Conversations.ReplyToActivityAsync(reply);
                reply = activity.CreateReply("What are you having trouble with today?");
                await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        [Serializable]
        public class HelpDialog : IDialog<object> {
            protected int count = 1;
            public async Task StartAsync(IDialogContext context) {
                context.Wait(MessageReceivedAsync);
            }
            public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument){
                var message = await argument;
                if (message.Text.Contains("printer") || message.Text.Contains("print")){ // resets count when requested. asks for confirmation.
                    var printertype = new List<string>();
                    printertype.Add("Local printer");
                    printertype.Add("Network printer");
                    PromptDialog.Choice(
                        context,
                        AfterPrinterAsync,
                        printertype,
                        "What kind of printer is it?",
                        promptStyle: PromptStyle.Auto); // sends result of confirmation to reset handler
                }
                else if (message.Text.Contains("email") || message.Text.Contains("e-mail")){
                    var emailroute = new List<string>();
                    emailroute.Add("Outlook Client");
                    emailroute.Add("Outlook.Office365.com");
                    PromptDialog.Choice(
                        context,
                        AfterEmailAsync,
                        emailroute,
                        "How are you accessing your email?",
                        promptStyle: PromptStyle.Auto);
                }
                else if (message.Text == "goodbye") { // says goodbye and exits chat (not gracefully)
                    await context.PostAsync("Goodbye!");
                    context.Wait(MessageReceivedAsync);
                    Environment.Exit(0);
                }
            }

            public async Task AfterPrinterAsync(IDialogContext context,IAwaitable<string> argument){
                string type = await argument;
                if (type == "Network printer") {
                    PromptDialog.Text(
                        context,
                        AfterNetworkPrinterAsync,
                        "What is the name of the network printer? Its name will be like " + @"\\\\" + "ADPRINT03\\BOL272a_PCL");
                }
                else {
                    PromptDialog.Text(
                        context,
                        AfterNetworkPrinterAsync,
                        "What is the model of your printer?");
                }
            }

            public async Task AfterNetworkPrinterAsync(IDialogContext context,IAwaitable<string> argument) {
                string type = await argument;
                PromptDialog.Text(
                        context,
                        AfterITAsync,
                        "Please describe the issue you are facing.");
            }

            public async Task AfterEmailAsync(IDialogContext context, IAwaitable<string> argument){
                var confirm = await argument;
                if (confirm == "Outlook Client") {
                    PromptDialog.Text(
                        context,
                        AfterITAsync,
                        "Please describe the issue you are facing.");
                }
                else {
                    await context.PostAsync("Are you having troubles accessing other webpages? DEBUG: no response");
                }
            }
            public async Task AfterITAsync(IDialogContext context, IAwaitable<string> argument){
                var confirm = await argument;
                await context.PostAsync("Thank you for the information. Connecting you to a support tech. Standby. DEBUG: send problem string to tech");
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