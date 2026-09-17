using System;
using System.Threading.Tasks;

namespace BelowTheWing.Net
{
    public sealed class ServiceLeave
    {
        public bool Pending { get; private set; }

        public async Task Run(Func<Task> leave)
        {
            Pending = true;

            try
            {
                await leave();
            }
            finally
            {
                Pending = false;
            }
        }
    }
}
