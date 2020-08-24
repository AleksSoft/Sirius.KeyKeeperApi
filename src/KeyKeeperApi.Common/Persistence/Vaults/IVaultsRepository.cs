using System.Threading.Tasks;
using KeyKeeperApi.Common.ReadModels.Vaults;

namespace KeyKeeperApi.Common.Persistence.Vaults
{
    public interface IVaultsRepository
    {
        Task<Vault> GetByIdAsync(long vaultId);

        Task AddOrUpdateAsync(Vault vault);
    }
}
