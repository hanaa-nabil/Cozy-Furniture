using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace Furniture.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // Each user joins their own group on connect
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Admins/Managers join admin group
            if (Context.User!.IsInRole("Admin") || Context.User.IsInRole("Manager"))
                await Groups.AddToGroupAsync(Context.ConnectionId, "admins");

            await base.OnConnectedAsync();
        }
    }
}
