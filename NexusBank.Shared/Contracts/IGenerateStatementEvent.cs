using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusBank.Shared.Contracts
{
    public interface IGenerateStatementEvent
    {
        string AccountId { get; }
        string StatementPeriod { get; }
        Guid CorrelationId { get; }
    }
}
