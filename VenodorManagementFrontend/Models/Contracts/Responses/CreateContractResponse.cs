using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class CreateContractResponse
    {
        public ContractDto? Contract { get; set; }
        public List<ContractDto>? Contracts { get; set; }
    }
