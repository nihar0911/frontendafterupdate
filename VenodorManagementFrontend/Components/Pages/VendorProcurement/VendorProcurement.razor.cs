using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.VendorProcurement;

public partial class VendorProcurement : ComponentBase
{
    [Parameter]
    public int RequestId { get; set; }

    private bool IsLoading { get; set; } = true;
    private bool HasError { get; set; } = false;
    private string ErrorMessage { get; set; } = string.Empty;

    private VendorProcurementOpportunityDto? Opportunity { get; set; }
    private decimal UnitPriceInput { get; set; }
    private DateTime ValidUntilInput { get; set; } = DateTime.Now.AddDays(7);

    private bool IsResponding { get; set; } = false;
    private bool ShowRejectModal { get; set; } = false;
    private string? RejectionReasonInput { get; set; }
    private string? ResponseFeedbackMessage { get; set; }

    private bool IsSubmitting { get; set; } = false;
    private string? FormError { get; set; }
    private QuotationDto? CreatedQuotation { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (Auth.IsAuthenticated && Auth.IsVendorManager)
        {
            await LoadData();
        }
        else
        {
            IsLoading = false;
        }
    }

    private async Task LoadData()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var opportunities = await Api.GetVendorProcurementOpportunitiesAsync();
            Opportunity = opportunities?.FirstOrDefault(o => o.RequestID == RequestId);

            if (Opportunity != null)
            {
                UnitPriceInput = Opportunity.UnitPrice;
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private void OpenRejectModal()
    {
        RejectionReasonInput = string.Empty;
        ShowRejectModal = true;
    }

    private void CloseRejectModal()
    {
        ShowRejectModal = false;
    }

    private async Task HandleRespond(string action)
    {
        if (Opportunity == null) return;

        IsResponding = true;
        ResponseFeedbackMessage = null;
        StateHasChanged();

        try
        {
            var cmd = new RespondToOpportunityCommand
            {
                RequestID = Opportunity.RequestID,
                ProductID = Opportunity.ProductID,
                Action = action,
                RejectionReason = string.Equals(action, "Reject", StringComparison.OrdinalIgnoreCase) ? RejectionReasonInput : null
            };

            var res = await Api.RespondToOpportunityAsync(cmd);
            if (res.Success && res.Data != null)
            {
                Opportunity.OpportunityStatus = res.Data.OpportunityStatus;
                Opportunity.ResponseDate = DateTime.Now;
                if (string.Equals(action, "Reject", StringComparison.OrdinalIgnoreCase))
                {
                    Opportunity.RejectionReason = RejectionReasonInput;
                    ShowRejectModal = false;
                    ResponseFeedbackMessage = "Purchase Request rejected. The request creator has been notified.";
                }
                else
                {
                    ResponseFeedbackMessage = "Purchase Request accepted. You can now prepare and submit your quotation.";
                }
            }
            else
            {
                ResponseFeedbackMessage = res.ErrorMessage ?? "Unable to record your response. Please try again.";
            }
        }
        catch (Exception ex)
        {
            ResponseFeedbackMessage = ex.Message;
        }
        finally
        {
            IsResponding = false;
            StateHasChanged();
        }
    }

    private async Task SubmitQuotation()
    {
        if (Opportunity == null) return;
        FormError = null;

        if (Opportunity.OpportunityStatus != "Accepted")
        {
            FormError = "You must accept this procurement opportunity before submitting a quotation.";
            return;
        }

        if (UnitPriceInput <= 0)
        {
            FormError = "Unit Price must be greater than 0.";
            return;
        }

        IsSubmitting = true;
        StateHasChanged();

        try
        {
            var command = new CreateQuotationCommand
            {
                RequestID = Opportunity.RequestID,
                VendorID = Opportunity.VendorID,
                ValidUntil = ValidUntilInput,
                Status = "Submitted",
                Items = new List<CreateQuotationItemDto>
                {
                    new CreateQuotationItemDto
                    {
                        ProductID = Opportunity.ProductID,
                        Quantity = Opportunity.RequestedQuantity,
                        UnitPrice = UnitPriceInput
                    }
                }
            };

            CreatedQuotation = await Api.CreateQuotationAsync(command);

            if (CreatedQuotation == null)
            {
                FormError = "Unable to submit quotation. Please check requirements and try again.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VendorProcurement] Error creating quotation: {ex.Message}");
            FormError = ex.Message;
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }

    private string VendorName => Opportunity?.VendorName ?? (Auth.UserName ?? "Vendor");

    private void HandleLogout()
    {
        Auth.Logout();
        Nav.NavigateTo("/login");
    }

    private void NavigateToSection(string nav)
    {
        Nav.NavigateTo("/vendor-dashboard");
    }
}