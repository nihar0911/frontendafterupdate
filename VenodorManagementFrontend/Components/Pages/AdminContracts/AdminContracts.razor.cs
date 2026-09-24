using Microsoft.AspNetCore.Components;

namespace VendorManagement.Web.Components.Pages.AdminContracts;

public partial class AdminContracts : ComponentBase
{
    protected override void OnInitialized()
    {
        Nav.NavigateTo("/admin", replace: true);
    }
}