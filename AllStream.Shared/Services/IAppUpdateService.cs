using System.Threading.Tasks;
using AllStream.Shared.Models;

namespace AllStream.Shared.Services
{
    public interface IAppUpdateService
    {
        Task<Asset> CheckForUpdatesAsync(string? currentVersion, string formFactor);
    }
}
