using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;

namespace VenodorManagementFrontend.Components.Shared.OutletForm;

public partial class OutletForm : ComponentBase
{

[Parameter] public OutletDto? EditingOutlet { get; set; }
    [Parameter] public List<OrganizationDto> Organizations { get; set; } = new();
    [Parameter] public bool IsSubmitting { get; set; }

    [Parameter] public EventCallback<CreateOutletCommand> OnCreate { get; set; }
    [Parameter] public EventCallback<UpdateOutletCommand> OnUpdate { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private int SelectedOrgId { get; set; }
    private string OutletName { get; set; } = string.Empty;
    private string Address { get; set; } = string.Empty;
    private decimal? Latitude { get; set; }
    private decimal? Longitude { get; set; }
    private string PurchaseOrderApproverRole { get; set; } = "Organization Manager";
    private string? ValidationMessage { get; set; }

    protected override void OnParametersSet()
    {
        ValidationMessage = null;

        if (EditingOutlet != null)
        {
            SelectedOrgId = EditingOutlet.OrganizationID;
            OutletName = EditingOutlet.OutletName;
            Address = EditingOutlet.Address ?? string.Empty;
            Latitude = EditingOutlet.Latitude;
            Longitude = EditingOutlet.Longitude;
            PurchaseOrderApproverRole = string.IsNullOrWhiteSpace(EditingOutlet.PurchaseOrderApproverRole)
                ? "Organization Manager"
                : EditingOutlet.PurchaseOrderApproverRole;
        }
        else
        {
            OutletName = string.Empty;
            Address = string.Empty;
            Latitude = null;
            Longitude = null;
            PurchaseOrderApproverRole = "Organization Manager";
            if (Organizations != null && Organizations.Count > 0)
            {
                SelectedOrgId = Organizations[0].OrganizationID;
            }
        }
    }

    private async Task HandleSubmit()
    {
        ValidationMessage = null;

        if (string.IsNullOrWhiteSpace(OutletName))
        {
            ValidationMessage = "Outlet Name is required.";
            return;
        }

        if (SelectedOrgId <= 0)
        {
            ValidationMessage = "Please select a valid Organization.";
            return;
        }

        if (EditingOutlet == null)
        {
            var command = new CreateOutletCommand
            {
                OrganizationID = SelectedOrgId,
                OutletName = OutletName.Trim(),
                Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
                Latitude = Latitude,
                Longitude = Longitude,
                PurchaseOrderApproverRole = PurchaseOrderApproverRole
            };
            await OnCreate.InvokeAsync(command);
        }
        else
        {
            var command = new UpdateOutletCommand
            {
                OutletID = EditingOutlet.OutletID,
                OrganizationID = SelectedOrgId,
                OutletName = OutletName.Trim(),
                Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
                Latitude = Latitude,
                Longitude = Longitude,
                PurchaseOrderApproverRole = PurchaseOrderApproverRole
            };
            await OnUpdate.InvokeAsync(command);
        }
    }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

}