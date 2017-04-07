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
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;

namespace _658ChatBot {
    [BotAuthentication]
    [LuisModel("8370a817-9d02-409e-afef-9dfb52b5534e","b3a83bcea307466da3008b8140fab7d0")]
    public class MessagesController : ApiController {
        /// <summary>
        /// POST: api/Messages
        /// calls HelpDialog
        /// begins simple IT troubleshooting / info gathering
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity){
            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
            if (activity.Type == ActivityTypes.Message){
                await Conversation.SendAsync(activity, () => new HelpDialog()); // begins conversation
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
            // protected int count = 1;
            public async Task StartAsync(IDialogContext context) {
                context.Wait(MessageReceivedAsync);
            }
            public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument){
                var message = await argument;
                if (message.Text.Contains("printer") || message.Text.Contains("print")){ // begins printer troubleshooting
                    var printertype = new List<string>();
                    printertype.Add("Local printer");
                    printertype.Add("Network printer");
                    PromptDialog.Choice(
                        context,
                        AfterPrinterAsync,
                        printertype,
                        "What kind of printer is it?",
                        promptStyle: PromptStyle.Auto); // sends result to printer handler
                }
                else if (message.Text.Contains("email") || message.Text.Contains("e-mail")){ // begins email troubleshooting
                    var emailroute = new List<string>();
                    emailroute.Add("Outlook Client");
                    emailroute.Add("Outlook.Office365.com");
                    PromptDialog.Choice(
                        context,
                        AfterEmailAsync,
                        emailroute,
                        "How are you accessing your email?",
                        promptStyle: PromptStyle.Auto); // sends result to email handler
                }
                else if (message.Text == "goodbye") { // says goodbye and exits chat (not gracefully)
                    await context.PostAsync("Goodbye!");
                    context.Wait(MessageReceivedAsync);
                    Environment.Exit(0);
                }
            }

            [LuisIntent("")]
            public async Task None(IDialogContext context,LuisResult result) {
                string message = $"Sorry I did not understand: " + string.Join(", ",result.Intents.Select(i => i.Intent));
                await context.PostAsync(message);
                context.Wait(MessageReceivedAsync);
            }

            [LuisIntent("PrinterIntent")]
            public async Task AfterPrinterAsync(IDialogContext context,IAwaitable<string> argument){
                string type = await argument;
                if (type == "Network printer") { // if network printer
                    PromptDialog.Text(
                        context,
                        AfterNetworkPrinterAsync,
                        "What is the name of the network printer? Its name will be like " + @"\\\\" + "ADPRINT03\\BOL272a_PCL");
                }
                else { // if local printer
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
                        "Please describe the issue you are facing."); // send description to IT handler
            }

            [LuisIntent("EmailIntent")]
            public async Task AfterEmailAsync(IDialogContext context, IAwaitable<string> argument){
                var confirm = await argument;
                if (confirm == "Outlook Client") {
                    PromptDialog.Text(
                        context,
                        AfterITAsync,
                        "Please describe the issue you are facing."); // send description to IT handler
                }
                else {
                    await context.PostAsync("Are you having troubles accessing other webpages? DEBUG: no response"); // to network?
                }
            }

            [LuisIntent("HardwareIntent")]
            public async Task HardwareAsync(IDialogContext context, IAwaitable<string> argument) {
                var device = await argument;
                switch (device) {
                    case "computer":
                        // computer name dialog
                        break;
                    case "monitor":
                        // ask if they want a rental
                        break;
                    case "iPad":
                        // device name dialog
                        break;
                    case "Phone":
                        // find out if they're in RVW or CAMB, o.w. send to Phone Services
                        break;
                    default:
                        // generic question OR connect to HD tech
                        break; 
                }
            }

            [LuisIntent("SoftwareIntent")]
            public async Task SoftwareAsync(IDialogContext context, IAwaitable<string> argument) {
                var soft = await argument;
                // ask software name
            }

            [LuisIntent("NetworkIntent")]
            public async Task NetworkAsync(IDialogContext context, IAwaitable<string> argument) {
                var desc = await argument;
                // find out how many users are affected
                // wifi or ethernet?
            }

            public async Task AfterITAsync(IDialogContext context, IAwaitable<string> argument){ // TODO connect to support tech
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