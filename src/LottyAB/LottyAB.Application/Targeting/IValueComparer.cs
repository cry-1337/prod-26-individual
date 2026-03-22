namespace LottyAB.Application.Targeting;

public interface IValueComparer
{
    int Compare(object? left, object? right);
    bool AreEqual(object? left, object? right);
}