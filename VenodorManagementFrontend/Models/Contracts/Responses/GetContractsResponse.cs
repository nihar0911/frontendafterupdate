using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class GetContractsResponse
    {
        public List<ContractDto> Contracts { get; set; } = new();
    }
