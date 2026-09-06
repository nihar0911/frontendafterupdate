using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class CreateContractFromQuotationResponse
    {
        public ContractDto? Contract { get; set; }
        public string Message { get; set; } = string.Empty;
    }
