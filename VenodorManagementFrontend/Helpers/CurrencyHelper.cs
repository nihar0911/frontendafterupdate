namespace VenodorManagementFrontend.Helpers;

public static class CurrencyHelper
{
    public const string CurrencyPrefix = "Rs.";

   
    // Formats a decimal amount using the standardized application currency format: "Rs. {amount:N2}".
    // Example: 250 => "Rs. 250.00", 2625 => "Rs. 2,625.00", 119783.72 => "Rs. 119,783.72"
   
    public static string Format(decimal amount) => $"Rs. {amount:N2}";

   
    // Formats a nullable decimal amount using the standardized application currency format: "Rs. {amount:N2}".
    // Returns "Rs. 0.00" if amount is null.
    
    public static string Format(decimal? amount) => amount.HasValue ? $"Rs. {amount.Value:N2}" : "Rs. 0.00";

  
    // Formats a double amount using the standardized application currency format: "Rs. {amount:N2}".
   
    public static string Format(double amount) => $"Rs. {amount:N2}";

    
    // Formats a nullable double amount using the standardized application currency format: "Rs. {amount:N2}".
   
    public static string Format(double? amount) => amount.HasValue ? $"Rs. {amount.Value:N2}" : "Rs. 0.00";
}
