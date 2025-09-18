using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RMS.Data;
using RMS.Hubs;
using RMS.Models.DTOs.Orders;
using RMS.Models.Entities;

namespace RMS.Services
{
    public class OrderService
    {
        private readonly RmsDbContext _ctx;
        private readonly IHubContext<KdsHub> _hub;

        public OrderService(RmsDbContext ctx, IHubContext<KdsHub> hub)
        {
            _ctx = ctx;
            _hub = hub;
        }

        
        public async Task<OrderDto> CreateAsync(CreateOrderDto dto)
        {
            var order = new Order
            {
                TableId = dto.TableId,
                Discount = dto.Discount,
                TaxPercent = dto.TaxPercent,
                Status = "Pending"
            };
            _ctx.Orders.Add(order);
            await _ctx.SaveChangesAsync();

            
            var table = await _ctx.Tables.FindAsync(dto.TableId);
            if (table != null)
            {
                table.Status = "Occupied";
                await _ctx.SaveChangesAsync();
            }

            foreach (var it in dto.Items)
            {
                _ctx.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    MenuItemId = it.MenuItemId,
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice,
                    Status = "Queued"
                });
            }
            await _ctx.SaveChangesAsync();

            var dtoResult = await GetAsync(order.Id) ?? throw new Exception("Order creation failed");

            await _hub.Clients.All.SendAsync("NewOrder", dtoResult);
            return dtoResult;
        }

        
        public async Task<OrderDto?> GetAsync(int id)
        {
            var order = await _ctx.Orders
                .Include(o => o.Table)
                .Include(o => o.Items).ThenInclude(i => i.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return null;

            return new OrderDto
            {
                Id = order.Id,
                Status = order.Status,
                TableId = order.TableId,
                TableName = order.Table?.Name ?? "",
                Items = order.Items.Select(i => new OrderItemDto
                {
                    MenuItemId = i.MenuItemId,
                    Name = i.MenuItem.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };
        }

        
        public async Task<IEnumerable<OrderDto>> ListAsync()
        {
            var orders = await _ctx.Orders
                .Include(o => o.Table)
                .Include(o => o.Items).ThenInclude(i => i.MenuItem)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return orders.Select(order => new OrderDto
            {
                Id = order.Id,
                Status = order.Status,
                TableId = order.TableId,
                TableName = order.Table?.Name ?? "",
                Items = order.Items.Select(i => new OrderItemDto
                {
                    MenuItemId = i.MenuItemId,
                    Name = i.MenuItem.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            });
        }

        
        public async Task<IEnumerable<OrderDto>> LiveOrdersAsync()
        {
            var orders = await _ctx.Orders
                .Include(o => o.Table)
                .Include(o => o.Items).ThenInclude(i => i.MenuItem)
                .Where(o => o.Status != "Billed")
                .ToListAsync();

            return orders.Select(order => new OrderDto
            {
                Id = order.Id,
                Status = order.Status,
                TableId = order.TableId,
                TableName = order.Table?.Name ?? "",
                Items = order.Items.Select(i => new OrderItemDto
                {
                    MenuItemId = i.MenuItemId,
                    Name = i.MenuItem.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            });
        }

        
        public async Task<bool> MarkItemReadyAsync(int orderItemId)
        {
            var item = await _ctx.OrderItems.FindAsync(orderItemId);
            if (item == null) return false;

            item.Status = "Ready";
            await _ctx.SaveChangesAsync();

            var allReady = await _ctx.OrderItems
                .Where(i => i.OrderId == item.OrderId)
                .AllAsync(i => i.Status == "Ready");

            if (allReady)
            {
                var order = await _ctx.Orders.FindAsync(item.OrderId);
                if (order != null)
                {
                    order.Status = "Ready";
                    await _ctx.SaveChangesAsync();
                }
            }

            var dto = await GetAsync(item.OrderId);
            if (dto != null)
                await _hub.Clients.All.SendAsync("OrderUpdated", dto);

            return true;
        }

        
        public async Task<bool> MarkReadyForBillingAsync(int orderId)
        {
            var order = await _ctx.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.Status = "ReadyForBilling";
            await _ctx.SaveChangesAsync();

            var dto = await GetAsync(orderId);
            if (dto != null)
                await _hub.Clients.All.SendAsync("OrderUpdated", dto);

            return true;
        }

        
        public async Task<bool> AddItemsAsync(int orderId, IEnumerable<OrderItemDto> items)
        {
            var order = await _ctx.Orders.FindAsync(orderId);
            if (order == null || order.Status == "Billed") return false;

            foreach (var it in items)
            {
                _ctx.OrderItems.Add(new OrderItem
                {
                    OrderId = orderId,
                    MenuItemId = it.MenuItemId,
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice,
                    Status = "Queued"
                });
            }
            order.Status = "Pending";
            await _ctx.SaveChangesAsync();

            var dto = await GetAsync(orderId);
            if (dto != null)
                await _hub.Clients.All.SendAsync("OrderUpdated", dto);

            return true;
        }

        
        public async Task<OrderDto?> AddAsNewOrderAsync(int sourceOrderId, IEnumerable<OrderItemDto> items)
        {
            var source = await _ctx.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == sourceOrderId);
            if (source == null || source.Status == "Billed") return null;

            var newOrder = new Order
            {
                TableId = source.TableId,
                Discount = 0,
                TaxPercent = source.TaxPercent,
                Status = "Pending"
            };

            _ctx.Orders.Add(newOrder);
            await _ctx.SaveChangesAsync();

            
            var table = await _ctx.Tables.FindAsync(source.TableId);
            if (table != null)
            {
                table.Status = "Occupied";
                await _ctx.SaveChangesAsync();
            }

            foreach (var it in items)
            {
                _ctx.OrderItems.Add(new OrderItem
                {
                    OrderId = newOrder.Id,
                    MenuItemId = it.MenuItemId,
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice,
                    Status = "Queued"
                });
            }
            await _ctx.SaveChangesAsync();

            var dto = await GetAsync(newOrder.Id);
            if (dto != null)
                await _hub.Clients.All.SendAsync("NewOrder", dto);

            return dto;
        }

        
        public async Task<bool> CloseOrderAsync(int orderId)
        {
            var order = await _ctx.Orders
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return false;

            order.Status = "Billed";
            if (order.Table != null)
                order.Table.Status = "Available"; 

            await _ctx.SaveChangesAsync();

            var dto = await GetAsync(orderId);
            if (dto != null)
                await _hub.Clients.All.SendAsync("OrderUpdated", dto);

            return true;
        }

        
        public async Task<bool> SetStatusAsync(int orderId, string status)
        {
            var order = await _ctx.Orders
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return false;

            order.Status = status;

            
            if (status == "Billed" && order.Table != null)
                order.Table.Status = "Available";
            if (status == "Pending" && order.Table != null)
                order.Table.Status = "Occupied";

            await _ctx.SaveChangesAsync();

            var dto = await GetAsync(orderId);
            if (dto != null)
                await _hub.Clients.All.SendAsync("OrderUpdated", dto);

            return true;
        }

        
        public async Task<bool> UpdateItemsAsync(int orderId, IEnumerable<OrderItemDto> items)
        {
            var order = await _ctx.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || order.Status == "Billed")
                return false;

            
            _ctx.OrderItems.RemoveRange(order.Items);

            
            foreach (var it in items)
            {
                _ctx.OrderItems.Add(new OrderItem
                {
                    OrderId = orderId,
                    MenuItemId = it.MenuItemId,
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice,
                    Status = "Queued"
                });
            }

            

            await _ctx.SaveChangesAsync();

            var dto = await GetAsync(orderId);
            if (dto != null)
                await _hub.Clients.All.SendAsync("OrderUpdated", dto);

            return true;
        }


    }
}
