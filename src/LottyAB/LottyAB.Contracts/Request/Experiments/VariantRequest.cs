namespace LottyAB.Contracts.Request.Experiments;

public record VariantRequest(
    string Name,
    string Value,
    double Weight,
    bool IsControl);