using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AfghanPay.API.Data;
using AfghanPay.Hubs;
using AfghanPay.Models;
using AfghanPay.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AfghanPay.Services
{
    public class AdminEventsBrodcaster : IAdminEventsBrodcaster
    {
        private readonly AppDbContext _dbContext;
        private readonly IHubContext<AdminHub> _adminhub;
        public AdminEventsBrodcaster(AppDbContext dbContext, IHubContext<AdminHub> adminhub)
        {
            _dbContext = dbContext;
            _adminhub = adminhub;
        }
        public async Task SaveAndBrodcastAsync(AdminEvents adminevent, CancellationToken cancellationToken = default)
        {
            _dbContext.AdminEvents.Add(adminevent);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await BrodcastAsync(adminevent);
        }
        public Task BrodcastAsync(AdminEvents adminevent)
        {
            //brodcasting to all super_admins

            return _adminhub.Clients.Group("superadmin").SendAsync("admin:event", new
            {
                adminevent.Id,
                adminevent.Type,
                adminevent.OccurredAt,
                adminevent.Amount,
                adminevent.Fee,
                adminevent.Commission,
                adminevent.TxRef,
                adminevent.SenderPhone,
                adminevent.ReciverPhone,
                adminevent.AgentCode,
                adminevent.Staus,
                adminevent.Reason,
                adminevent.TransactionId,
                adminevent.CashoutRequestId,
                adminevent.DataJson

            });
        }
    }
}
