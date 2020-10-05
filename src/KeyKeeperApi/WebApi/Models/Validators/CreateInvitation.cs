namespace KeyKeeperApi.WebApi.Models.Validators
{
    public class CreateInvitation
    {
        public string TenantId { get; set; }

        public string Name { get; set; }

        public string Position { get; set; }

        public string Description { get; set; }

        public string InvitationToken { get; set; }
    }
}
