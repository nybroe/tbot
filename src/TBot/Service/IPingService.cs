using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TBot.Service
{
    public interface IPingService
    {
        Task InitiateAsync(bool execute);
    }
}
