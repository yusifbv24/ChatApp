namespace ChatApp.Modules.Identity.Application.Queries.GetUsers
{
    public class GetUsersQuery
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}