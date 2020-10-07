using System.Threading.Tasks;
using KeyKeeperApi.MyNoSql;

namespace KeyKeeperApi.Services
{
    public interface IPushNotificator
    {
        Task SendPushNotifications(ApprovalRequestMyNoSqlEntity approvalRequest);
    }
}
