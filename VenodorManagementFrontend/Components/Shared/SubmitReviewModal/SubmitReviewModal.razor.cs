using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models.VendorPerformance;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Shared.SubmitReviewModal;

public partial class SubmitReviewModal : ComponentBase
{
    [Inject] public ApiService Api { get; set; } = default!;
    [Inject] public AuthService Auth { get; set; } = default!;

    [Parameter] public bool IsOpen { get; set; } = false;
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EligibleReviewOrderDto? Item { get; set; }
    [Parameter] public EventCallback<int> OnReviewSubmitted { get; set; }

    protected int OverallRating { get; set; } = 0;
    protected int ProductQualityRating { get; set; } = 0;
    protected int DeliveryRating { get; set; } = 0;
    protected string ReviewText { get; set; } = string.Empty;

    protected bool IsSubmitting { get; set; } = false;
    protected string? ErrorMessage { get; set; }

    protected bool IsDeliveryDelayed
    {
        get
        {
            if (Item?.ActualDeliveryDate.HasValue == true && Item?.ExpectedDeliveryDate.HasValue == true)
            {
                return Item.ActualDeliveryDate.Value.Date > Item.ExpectedDeliveryDate.Value.Date;
            }
            return false;
        }
    }

    protected int DelayDays
    {
        get
        {
            if (IsDeliveryDelayed && Item?.ActualDeliveryDate.HasValue == true && Item?.ExpectedDeliveryDate.HasValue == true)
            {
                return (Item.ActualDeliveryDate.Value.Date - Item.ExpectedDeliveryDate.Value.Date).Days;
            }
            return 0;
        }
    }

    protected override void OnParametersSet()
    {
        if (IsOpen && !IsSubmitting)
        {
            ErrorMessage = null;
        }
    }

    protected void SetOverallRating(int rating)
    {
        OverallRating = rating;
        ErrorMessage = null;
    }

    protected void SetProductQualityRating(int rating)
    {
        ProductQualityRating = rating;
        ErrorMessage = null;
    }

    protected void SetDeliveryRating(int rating)
    {
        DeliveryRating = rating;
        ErrorMessage = null;
    }

    protected async Task HandleBackdropClick()
    {
        if (!IsSubmitting)
        {
            await CloseAsync();
        }
    }

    public async Task CloseAsync()
    {
        if (IsSubmitting) return;

        ResetFormState();
        IsOpen = false;
        await IsOpenChanged.InvokeAsync(false);
    }

    private void ResetFormState()
    {
        OverallRating = 0;
        ProductQualityRating = 0;
        DeliveryRating = 0;
        ReviewText = string.Empty;
        ErrorMessage = null;
        IsSubmitting = false;
    }

    protected async Task SubmitReviewAsync()
    {
        if (Item == null || Item.AlreadyReviewed) return;

        // 1. Validation
        if (OverallRating < 1 || OverallRating > 5)
        {
            ErrorMessage = "Please select an Overall Rating between 1 and 5.";
            return;
        }

        if (ProductQualityRating < 1 || ProductQualityRating > 5)
        {
            ErrorMessage = "Please select a Product Quality rating between 1 and 5.";
            return;
        }

        if (DeliveryRating < 1 || DeliveryRating > 5)
        {
            ErrorMessage = "Please select a Delivery Punctuality rating between 1 and 5.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ReviewText))
        {
            ErrorMessage = "Feedback comments are required.";
            return;
        }

        if (ReviewText.Trim().Length > 1000)
        {
            ErrorMessage = "Feedback comments cannot exceed 1000 characters.";
            return;
        }

        int ratedByUserId = Auth.CurrentUser?.UserID ?? 0;
        if (ratedByUserId <= 0)
        {
            ErrorMessage = "Valid authenticated user required to submit review.";
            return;
        }

        IsSubmitting = true;
        ErrorMessage = null;

        try
        {
            var request = new CreateVendorReviewRequest
            {
                VendorID = Item.VendorID,
                OutletID = Item.OutletID,
                PurchaseOrderID = Item.PurchaseOrderID,
                POItemID = Item.POItemID,
                RatedByUserID = ratedByUserId,
                Rating = (decimal)OverallRating,
                ProductQualityRating = (decimal)ProductQualityRating,
                DeliveryRating = (decimal)DeliveryRating,
                Review = ReviewText.Trim()
            };

            var result = await Api.CreateVendorReviewAsync(request);

            if (result.Success)
            {
                int poItemId = Item.POItemID;
                Item.AlreadyReviewed = true;
                ResetFormState();
                IsOpen = false;
                await IsOpenChanged.InvokeAsync(false);
                await OnReviewSubmitted.InvokeAsync(poItemId);
            }
            else
            {
                // On failure: keep modal open, show real error, do not clear selections
                ErrorMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? result.ErrorMessage
                    : "Unable to submit review. Please verify your connection and try again.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }
}
