namespace YachtDiceMaui.Models;

public static class ScoreCalculator
{
    public const int NumDice = 5;
    public const int UpperBonusThreshold = 63;
    public const int UpperBonusPoints = 35;
    public const int FullHousePoints = 25;
    public const int SmallStraightPoints = 30;
    public const int LargeStraightPoints = 40;
    public const int YachtPoints = 50;
    public const int YachtBonusPoints = 100;
    public const int CategoryCount = 13;

    public static int Calculate(ScoreCategory category, int[] dice)
    {
        ArgumentNullException.ThrowIfNull(dice);
        if (dice.Length != NumDice)
            throw new ArgumentException("Exactly 5 dice values required.");

        return category switch
        {
            ScoreCategory.Ones => SumOfValue(dice, 1),
            ScoreCategory.Twos => SumOfValue(dice, 2),
            ScoreCategory.Threes => SumOfValue(dice, 3),
            ScoreCategory.Fours => SumOfValue(dice, 4),
            ScoreCategory.Fives => SumOfValue(dice, 5),
            ScoreCategory.Sixes => SumOfValue(dice, 6),
            ScoreCategory.ThreeOfAKind => NOfAKind(dice, 3),
            ScoreCategory.FourOfAKind => NOfAKind(dice, 4),
            ScoreCategory.FullHouse => CalculateFullHouse(dice),
            ScoreCategory.SmallStraight => CalculateSmallStraight(dice),
            ScoreCategory.LargeStraight => CalculateLargeStraight(dice),
            ScoreCategory.Yacht => IsYacht(dice) ? YachtPoints : 0,
            ScoreCategory.Chance => dice.Sum(),
            _ => 0
        };
    }

    /// <summary>
    /// Calculate the score for a category in Triple mode, applying the column multiplier.
    /// Column 0 = 1x, Column 1 = 2x, Column 2 = 3x.
    /// </summary>
    public static int CalculateWithMultiplier(ScoreCategory category, int[] dice, int column)
    {
        int baseScore = Calculate(category, dice);
        int multiplier = column + 1;
        return baseScore * multiplier;
    }

    public static bool IsYacht(int[] dice) =>
        dice.Length == NumDice && dice.Distinct().Count() == 1;

    /// <summary>
    /// Check if a Yacht of the given dice can legally be applied to a category
    /// under strict matching rules. A Yacht qualifies for:
    /// - Its matching upper category (e.g., five 3's → Threes)
    /// - Three of a Kind, Four of a Kind (always, since 5-of-a-kind qualifies)
    /// - Full House (5 of same = 3+2 of same)
    /// - Chance (always)
    /// - Yacht (always)
    /// Does NOT qualify for straights.
    /// </summary>
    public static bool YachtQualifiesForCategory(ScoreCategory category, int dieValue)
    {
        return category switch
        {
            ScoreCategory.Ones => dieValue == 1,
            ScoreCategory.Twos => dieValue == 2,
            ScoreCategory.Threes => dieValue == 3,
            ScoreCategory.Fours => dieValue == 4,
            ScoreCategory.Fives => dieValue == 5,
            ScoreCategory.Sixes => dieValue == 6,
            ScoreCategory.ThreeOfAKind => true,
            ScoreCategory.FourOfAKind => true,
            ScoreCategory.FullHouse => true,
            ScoreCategory.Yacht => true,
            ScoreCategory.Chance => true,
            ScoreCategory.SmallStraight => false,
            ScoreCategory.LargeStraight => false,
            _ => false
        };
    }

    private static int SumOfValue(int[] dice, int value) =>
        dice.Where(d => d == value).Sum();

    private static int NOfAKind(int[] dice, int n)
    {
        var groups = dice.GroupBy(d => d);
        return groups.Any(g => g.Count() >= n) ? dice.Sum() : 0;
    }

    private static int CalculateFullHouse(int[] dice)
    {
        var counts = dice.GroupBy(d => d).Select(g => g.Count()).OrderBy(c => c).ToArray();
        // 5-of-a-kind also counts as full house (strict matching rule)
        if (counts.Length == 1)
            return FullHousePoints;
        return counts.Length == 2 && counts[0] == 2 && counts[1] == 3
            ? FullHousePoints
            : 0;
    }

    private static int CalculateSmallStraight(int[] dice)
    {
        var unique = dice.Distinct().OrderBy(d => d).ToArray();
        for (int i = 0; i <= unique.Length - 4; i++)
        {
            if (unique[i + 1] == unique[i] + 1 &&
                unique[i + 2] == unique[i] + 2 &&
                unique[i + 3] == unique[i] + 3)
                return SmallStraightPoints;
        }
        return 0;
    }

    private static int CalculateLargeStraight(int[] dice)
    {
        var sorted = dice.OrderBy(d => d).ToArray();
        for (int i = 0; i < sorted.Length - 1; i++)
        {
            if (sorted[i + 1] != sorted[i] + 1)
                return 0;
        }
        return LargeStraightPoints;
    }
}
