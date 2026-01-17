using System.Threading.Tasks;

namespace AllStream.Shared.Services
{
    public interface IAppUpdateService
    {
        Task CheckForUpdatesAsync();
    }
}
