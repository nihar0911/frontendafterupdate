using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;

namespace VenodorManagementFrontend.Components.Shared.ProductForm;

public partial class ProductForm : ComponentBase
{

[Parameter] public ProductDto? EditingProduct { get; set; }
    [Parameter] public List<TaxRateDto> TaxRates { get; set; } = new();
    [Parameter] public bool IsSubmitting { get; set; }

    [Parameter] public EventCallback<CreateProductCommand> OnCreate { get; set; }
    [Parameter] public EventCallback<UpdateProductCommand> OnUpdate { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private string ProductName { get; set; } = string.Empty;
    private string Category { get; set; } = string.Empty;
    private string Unit { get; set; } = string.Empty;
    private int SelectedTaxRateId { get; set; }
    private string Status { get; set; } = "Active";
    private string? ValidationMessage { get; set; }

    private List<TaxRateDto> AvailableTaxRates
    {
        get
        {
            if (EditingProduct != null)
            {
                return TaxRates;
            }
            return TaxRates.Where(t => string.Equals(t.Status, "Active", StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    protected override void OnParametersSet()
    {
        ValidationMessage = null;

        if (EditingProduct != null)
        {
            ProductName = EditingProduct.ProductName;
            Category = EditingProduct.Category ?? string.Empty;
            Unit = EditingProduct.Unit;
            SelectedTaxRateId = EditingProduct.TaxRateID;
            Status = EditingProduct.Status;
        }
        else
        {
            ProductName = string.Empty;
            Category = string.Empty;
            Unit = string.Empty;
            Status = "Active";
            var activeRates = AvailableTaxRates;
            if (activeRates.Count > 0)
            {
                SelectedTaxRateId = activeRates[0].TaxRateID;
            }
        }
    }

    private async Task HandleSubmit()
    {
        ValidationMessage = null;

        if (string.IsNullOrWhiteSpace(ProductName))
        {
            ValidationMessage = "Product Name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Unit))
        {
            ValidationMessage = "Unit is required.";
            return;
        }

        if (SelectedTaxRateId <= 0)
        {
            ValidationMessage = "Please select a valid Tax Rate.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Status))
        {
            ValidationMessage = "Status is required.";
            return;
        }

        if (EditingProduct == null)
        {
            var command = new CreateProductCommand
            {
                ProductName = ProductName.Trim(),
                Category = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim(),
                Unit = Unit.Trim(),
                TaxRateID = SelectedTaxRateId,
                Status = Status
            };
            await OnCreate.InvokeAsync(command);
        }
        else
        {
            var command = new UpdateProductCommand
            {
                ProductID = EditingProduct.ProductID,
                ProductName = ProductName.Trim(),
                Category = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim(),
                Unit = Unit.Trim(),
                TaxRateID = SelectedTaxRateId,
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