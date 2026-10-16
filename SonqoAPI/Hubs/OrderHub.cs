using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SonqoAPI.Hubs
{
    [Authorize(Policy = "RequireStaff")]
    public class OrderHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var restaurantIdClaim = Context.User?.FindFirst("RestaurantId")?.Value;

            if (!string.IsNullOrEmpty(restaurantIdClaim) && restaurantIdClaim != "0")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, restaurantIdClaim);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var restaurantIdClaim = Context.User?.FindFirst("RestaurantId")?.Value;

            if (!string.IsNullOrEmpty(restaurantIdClaim) && restaurantIdClaim != "0")
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, restaurantIdClaim);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}