namespace FE.viewmodels
{
    public class TypeComponentViewModel
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public long? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? StatusCode { get; set; }
    }

    public class TypeComponentIndexViewModel
    {
        public IEnumerable<TypeComponentViewModel> Items { get; set; } = new List<TypeComponentViewModel>();
        public string? Search { get; set; }
        public long? BranchId { get; set; }
        public string? StatusCode { get; set; }
    }
}
