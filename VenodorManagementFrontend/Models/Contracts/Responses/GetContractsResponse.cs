using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class GetContractsResponse
    {
        public List<ContractDto> Contracts { get; set; } = new();
    }
