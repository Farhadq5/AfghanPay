using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AfghanPay.Models;

namespace AfghanPay.Services.Interfaces
{
    public interface IAdminEventsBrodcaster
    {
        Task BrodcastAsync(AdminEvents adminevent);
        Task SaveAndBrodcastAsync(AdminEvents adminevent, CancellationToken cancellationToken = default);
    }
}
