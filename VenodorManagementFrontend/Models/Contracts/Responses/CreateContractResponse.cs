using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class CreateContractResponse
    {
        public ContractDto? Contract { get; set; }
        public List<ContractDto>? Contracts { get; set; }
    }
