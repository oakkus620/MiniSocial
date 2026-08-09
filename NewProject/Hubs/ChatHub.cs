using Microsoft.AspNetCore.SignalR;

namespace NewProject.Hubs
{
    public class ChatHub : Hub
    {
        public async Task JoinUserGroup(string username)
        {
            if (!string.IsNullOrEmpty(username))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, username.ToLower().Trim());
            }
        }

        public async Task SendCall(string targetUser, string callerUser, string callType)
        {
            if (!string.IsNullOrEmpty(targetUser))
            {
                await Clients.Group(targetUser.ToLower().Trim()).SendAsync("ReceiveCall", callerUser, callType);
            }
        }

        public async Task EndCall(string targetUser)
        {
            if (!string.IsNullOrEmpty(targetUser))
            {
                await Clients.Group(targetUser.ToLower().Trim()).SendAsync("CallEnded");
            }
        }

        public async Task SendMessageNotification(string targetUser, string senderUser, string messageContent)
        {
            if (!string.IsNullOrEmpty(targetUser))
            {
                await Clients.Group(targetUser.ToLower().Trim()).SendAsync("ReceiveMessage", senderUser, messageContent);
            }
        }
    }
}