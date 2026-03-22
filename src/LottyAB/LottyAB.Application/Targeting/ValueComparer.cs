namespace LottyAB.Application.Targeting;

public class ValueComparer : IValueComparer
{
    public int Compare(object? left, object? right)
    {
        switch (left)
        {
            case null when right == null:
                return 0;
            case null:
                return -1;
        }

        if (right == null) return 1;

        var leftStr = left.ToString();
        var rightStr = right.ToString();

        if (double.TryParse(leftStr, out var leftNum) && double.TryParse(rightStr, out var rightNum))
            return leftNum.CompareTo(rightNum);

        if (DateTime.TryParse(leftStr, out var leftDate) && DateTime.TryParse(rightStr, out var rightDate))
            return leftDate.CompareTo(rightDate);

        if (bool.TryParse(leftStr, out var leftBool) && bool.TryParse(rightStr, out var rightBool))
            return leftBool.CompareTo(rightBool);

        return string.Compare(leftStr, rightStr, StringComparison.OrdinalIgnoreCase);
    }

    public bool AreEqual(object? left, object? right)
    {
        return Compare(left, right) == 0;
    }
}