using System.Threading.Tasks;
using KeyKeeperApi.Common.ReadModels.Vaults;

namespace KeyKeeperApi.Common.Persistence.Vaults
{
    public interface IVaultsRepository
    {
        Task<Vault> GetByIdOrDefaultAsync(long vaultId);

        Task InsertOrUpdateAsync(Vault vault);
    }
}
