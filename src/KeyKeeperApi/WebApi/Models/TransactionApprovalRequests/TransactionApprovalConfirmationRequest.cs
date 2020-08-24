using System.ComponentModel.DataAnnotations;

namespace KeyKeeperApi.WebApi.Models.TransactionApprovalRequests
{
    public class TransactionApprovalConfirmationRequest
    {
        public long TransactionApprovalRequestId { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public string Secret { get; set; }
    }
}
