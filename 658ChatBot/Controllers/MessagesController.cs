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
    
    public class MessagesController : ApiController {
        /// <summary>
        /// POST: api/Messages
        /// calls HelpDialog
        /// begins simple IT troubleshooting / info gathering
        /// </summary>
        static string requestor;
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity){
            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
            if (activity.Type == ActivityTypes.Message){
                await Conversation.SendAsync(activity, () => new HelpDialog()); // begins conversation
            }
            else if (activity.Type == ActivityTypes.ConversationUpdate && activity.MembersAdded[0] != null && activity.MembersAdded[0].Name != "Bot"){ // greets user when they join
                requestor = activity.MembersAdded[0].Id;
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

        [LuisModel("8370a817-9d02-409e-afef-9dfb52b5534e","b3a83bcea307466da3008b8140fab7d0")]
        [Serializable]
        public class HelpDialog : LuisDialog<object> {

            //LuisResult currentR;
            // protected int count = 1;
            string incident = $"Ticket Gen: {Conversation.Container.GetHashCode()}\r\nRequestor: {requestor}\r\n";
            int unknown = 0; // number of times program will ask again before giving up and going to issue description

            public async Task StartAsync(IDialogContext context) {
                context.Wait(MessageReceivedAsync);
            }
            public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument){
                var message = await argument;
                if (message.Text == "goodbye") { // says goodbye and exits chat (not gracefully)
                    await context.PostAsync("Goodbye!");
                    context.Wait(MessageReceivedAsync);
                    Environment.Exit(0);
                }
            }

            [LuisIntent("")]
            public async Task None(IDialogContext context,LuisResult result) {
                unknown++;
                string message = $"Sorry I did not understand: " + string.Join(", ",result.Intents.Select(i => i.Intent));
                await context.PostAsync(message);
                context.Wait(MessageReceivedAsync);
                if (unknown == 2) {
                    PromptDialog.Text(
                        context,
                        AfterITAsync,
                        "We cannot programmatically categorize your request. Please describe the issue you are facing in more detail."); // get issue description
                }
            }

            [LuisIntent("PrinterIntent")]
            public async Task PrinterAsync(IDialogContext context,LuisResult result) {
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

            public async Task AfterPrinterAsync(IDialogContext context,IAwaitable<string> argument) {
                string type = await argument;
                if (type == "Network printer") { // if network printer
                    PromptDialog.Text(
                        context,
                        AfterPrinterQueryAsync,
                        "What is the name of the network printer? Its name will be like " + @"\\\\" + "ADPRINT03\\BOL272a_PCL");
                }
                else { // if local printer
                    PromptDialog.Text(
                        context,
                        AfterPrinterQueryAsync,
                        "What is the model of your printer?");
                }
            }

            public async Task AfterPrinterQueryAsync(IDialogContext context,IAwaitable<string> argument) {
                string type = await argument;
                incident += $"Printer: {argument}\r\n";
                PromptDialog.Text(
                        context,
                        ComputerAsync,
                        "What is your computer's name? You can find this either on a black and white label visible on your computer, or by following the below instructions depending on your device:\r\nWindows: press <Windows Key>+Pause/Break\r\nMac: go to Apple Menu -> System Preferences ->Sharing\r\nThe name will be in the form of SA-{7 or 11 characters}"); // send description to IT handler
            }

            [LuisIntent("EmailIntent")]
            public async Task EmailAsync(IDialogContext context,LuisResult result) {
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

            public async Task AfterEmailAsync(IDialogContext context,IAwaitable<string> argument) {
                var confirm = await argument;
                if (confirm == "Outlook Client") {
                    PromptDialog.Text(
                        context,
                        ComputerAsync,
                        "What is your computer's name? You can find this either on a black and white label visible on your computer, or by following the below instructions depending on your device:\r\nWindows: press <Windows Key>+Pause/Break\r\nMac: go to Apple Menu -> System Preferences ->Sharing\r\nThe name will be in the form of SA-{7 or 11 characters}"); // send description to IT handler
            }
                else {
                    PromptDialog.Confirm(
                        context,
                        UITSAsync,
                        "Are you having troubles accessing other webpages?",
                        promptStyle: PromptStyle.None);
                }
            }

            [LuisIntent("HardwareIntent")]
            public async Task HardwareAsync(IDialogContext context,LuisResult result) {
                EntityRecommendation hard;
                result.TryFindEntity("Hardware",out hard);
                incident += $"Hardware: {hard.Entity}\r\n";
                await context.PostAsync($"It sounds like you're having trouble with hardware, your {hard.Entity}. We are going to ask for your computer name, which we will use for owner and location information.");
                PromptDialog.Text(
                    context,
                    ComputerAsync,
                    "What is your computer's name? You can find this either on a black and white label visible on your computer, or by following the below instructions depending on your device:\r\nWindows: press <Windows Key>+Pause/Break\r\nMac: go to Apple Menu -> System Preferences ->Sharing\r\nThe name will be in the form of SA-{7 or 11 characters}"); // send description to IT handler
            }


            [LuisIntent("SoftwareIntent")]
            public async Task SoftwareAsync(IDialogContext context,LuisResult result) {
                EntityRecommendation soft;
                result.TryFindEntity("Software",out soft);
                incident += $"Software: {soft.Entity}\r\n";
                await context.PostAsync($"It sounds like you're having trouble with software, {soft.Entity}. We are going to ask for your computer name, which we will use for owner and location information.");
                // put software name in cherwell ticket
                PromptDialog.Text(
                    context,
                    ComputerAsync,
                    "What is your computer's name? You can find this either on a black and white label visible on your computer, or by following the below instructions depending on your device:\r\nWindows: press <Windows Key>+Pause/Break\r\nMac: go to Apple Menu -> System Preferences ->Sharing\r\nThe name will be in the form of SA-{7 or 11 characters}"); // send description to IT handler
            }

            public async Task ComputerAsync(IDialogContext context,IAwaitable<string> argument) {
                var computer = await argument;
                incident += $"Computer Name: {computer}\r\n";
                PromptDialog.Text(
                        context,
                        AfterITAsync,
                        "Please describe the issue you are facing in more detail."); // get issue description
            }

            [LuisIntent("NetworkIntent")]
            public async Task NetworkAsync(IDialogContext context,LuisResult result) {
                PromptDialog.Confirm(
                        context,
                        UITSAsync,
                        "Are you connecting via UWMWifi, Eduroam, or UWM's Public Network?",
                        promptStyle: PromptStyle.None);
            }

            public async Task AfterITAsync(IDialogContext context, IAwaitable<string> argument){ // TODO connect to support tech
                var desc = await argument;
                incident += $"Description: {desc}\r\n";
                await context.PostAsync("Thank you for the information. We are submitting an incident into our ticket tracking system on your behalf. A member of our IT department will contact you shortly.");
                await context.PostAsync($"{incident}");
            }

            public async Task UITSAsync(IDialogContext context, IAwaitable<bool> confirm) {
                var conf = await confirm;
                if (!conf) {
                    PromptDialog.Text(
                        context,
                        AfterITAsync,
                        "Please describe the issue you are facing in more detail."); // get issue description
                }
                else await context.PostAsync("Unfortunately, this issue is outside of our scope. Please contact UITS about this issue at https://http://uwm.edu/technology/help/ or by calling 229 4040.");
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