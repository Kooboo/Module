using Kooboo.Web.ViewModel;

namespace SqlEx.Module.code
{
    public class PagedListViewModelWithPrimaryKey<T> : PagedListViewModel<T>
    {
        public string PrimaryKey { get; set; }
    }
}
