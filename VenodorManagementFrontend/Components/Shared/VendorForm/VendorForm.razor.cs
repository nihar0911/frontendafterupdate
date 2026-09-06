using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;

namespace VenodorManagementFrontend.Components.Shared.VendorForm;

public partial class VendorForm : ComponentBase
{

[Parameter] public VendorDto? EditingVendor { get; set; }
    [Parameter] public bool IsSubmitting { get; set; }

    [Parameter] public EventCallback<CreateVendorCommand> OnCreate { get; set; }
    [Parameter] public EventCallback<UpdateVendorCommand> OnUpdate { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private string VendorName { get; set; } = string.Empty;
    private string Email { get; set; } = string.Empty;
    private string Phone { get; set; } = string.Empty;
    private string Address { get; set; } = string.Empty;
    private decimal? Latitude { get; set; }
    private decimal? Longitude { get; set; }
    private string GSTIN { get; set; } = string.Empty;
    private string Status { get; set; } = "Active";
    private string? ValidationMessage { get; set; }

    protected override void OnParametersSet()
    {
        ValidationMessage = null;

        if (EditingVendor != null)
        {
            VendorName = EditingVendor.VendorName;
            Email = EditingVendor.Email ?? string.Empty;
            Phone = EditingVendor.Phone ?? string.Empty;
            Address = EditingVendor.Address ?? string.Empty;
            Latitude = EditingVendor.Latitude;
            Longitude = EditingVendor.Longitude;
            GSTIN = EditingVendor.GSTIN ?? string.Empty;
            Status = EditingVendor.Status;
        }
        else
        {
            VendorName = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
            Address = string.Empty;
            Latitude = null;
            Longitude = null;
            GSTIN = string.Empty;
            Status = "Active";
        }
    }

    private async Task HandleSubmit()
    {
        ValidationMessage = null;

        if (string.IsNullOrWhiteSpace(VendorName))
        {
            ValidationMessage = "Vendor Name is required.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(Email) && !Email.Contains("@"))
        {
            ValidationMessage = "Please enter a valid email address.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Status))
        {
            ValidationMessage = "Status is required.";
            return;
        }

        if (EditingVendor == null)
        {
            var command = new CreateVendorCommand
            {
                VendorName = VendorName.Trim(),
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
                Latitude = Latitude,
                Longitude = Longitude,
                GSTIN = string.IsNullOrWhiteSpace(GSTIN) ? null : GSTIN.Trim(),
                Status = Status
            };
            await OnCreate.InvokeAsync(command);
        }
        else
        {
            var command = new UpdateVendorCommand
            {
                VendorID = EditingVendor.VendorID,
                VendorName = VendorName.Trim(),
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
                Latitude = Latitude,
                Longitude = Longitude,
                GSTIN = string.IsNullOrWhiteSpace(GSTIN) ? null : GSTIN.Trim(),
                Status = Status
            };
            await OnUpdate.InvokeAsync(command);
        }
    }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

}