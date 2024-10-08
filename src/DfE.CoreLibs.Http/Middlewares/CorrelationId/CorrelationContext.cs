﻿using DfE.CoreLibs.Http.Interfaces;

namespace DfE.CoreLibs.Http.Middlewares.CorrelationId;

/// <inheritdoc />
public class CorrelationContext : ICorrelationContext
{
    /// <inheritdoc />
    public Guid CorrelationId { get; private set; }

    /// <inheritdoc />
    public void SetContext(Guid correlationId)
    {
        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException("Guid cannot be empty", nameof(correlationId));
        }
        CorrelationId = correlationId;
    }
}