using System.Threading.Tasks;
using KeyKeeperApi.Common.ReadModels.Blockchains;

namespace KeyKeeperApi.Common.Persistence.Blockchains
{
    public interface IBlockchainsRepository
    {
        Task<Blockchain> GetByIdAsync(string blockchainId);

        Task AddOrUpdateAsync(Blockchain blockchain);
    }
}
