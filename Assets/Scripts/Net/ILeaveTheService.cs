using System.Threading.Tasks;

namespace BelowTheWing.Net
{
    public interface ILeaveTheService
    {
        bool InOne { get; }

        Task LeaveAsync();
    }
}
