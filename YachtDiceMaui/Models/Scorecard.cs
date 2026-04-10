namespace YachtDiceMaui.Models;

public class Scorecard
{
    private readonly GameMode _mode;
    private readonly int _columnCount;
    private readonly int?[,] _scores;
    private readonly int[] _yachtBonusCount;

    public GameMode Mode => _mode;
    public int ColumnCount => _columnCount;
    public string PlayerName { get; set; } = "Matt";

    public Scorecard(GameMode mode)
    {
        _mode = mode;
        _columnCount = mode == GameMode.Triple ? 3 : 1;
        _scores = new int?[ScoreCalculator.CategoryCount, _columnCount];
        _yachtBonusCount = new int[_columnCount];
    }

    public bool IsSlotAvailable(ScoreCategory category, int column = 0)
    {
        ValidateColumn(column);
        return !_scores[(int)category, column].HasValue;
    }

    public enum ScoreResult { Failed, Scored, Yacht, YachtBonus }

    /// <summary>
    /// Score a category with the given dice. In Triple mode, the multiplier is applied at the cell level.
    /// </summary>
    public ScoreResult TryScore(ScoreCategory category, int column, int[] dice)
    {
        ValidateColumn(column);
        if (!IsSlotAvailable(category, column))
            return ScoreResult.Failed;

        bool isYacht = ScoreCalculator.IsYacht(dice);
        bool gotYachtBonus = false;

        // Check for Yacht bonus: if this is a Yacht and the Yacht cell in this column
        // already has a non-zero score, award a bonus.
        if (isYacht && _scores[(int)ScoreCategory.Yacht, column] == ScoreCalculator.YachtPoints)
        {
            // Strict matching: must qualify for the target category
            if (!ScoreCalculator.YachtQualifiesForCategory(category, dice[0]))
                return ScoreResult.Failed;
            _yachtBonusCount[column]++;
            gotYachtBonus = true;
        }

        int score = _mode == GameMode.Triple
            ? ScoreCalculator.CalculateWithMultiplier(category, dice, column)
            : ScoreCalculator.Calculate(category, dice);

        _scores[(int)category, column] = score;

        if (gotYachtBonus)
            return ScoreResult.YachtBonus;
        if (isYacht && category == ScoreCategory.Yacht)
            return ScoreResult.Yacht;
        return ScoreResult.Scored;
    }

    public int? GetScore(ScoreCategory category, int column = 0)
    {
        ValidateColumn(column);
        return _scores[(int)category, column];
    }

    /// <summary>
    /// Get the potential score for display. In Triple mode, includes the column multiplier.
    /// </summary>
    public int GetPotentialScore(ScoreCategory category, int[] dice, int column = 0)
    {
        return _mode == GameMode.Triple
            ? ScoreCalculator.CalculateWithMultiplier(category, dice, column)
            : ScoreCalculator.Calculate(category, dice);
    }

    /// <summary>
    /// Check if a yacht (all 5 dice the same) has at least one valid cell to be placed in.
    /// This includes: the Yacht cell itself, or any category where YachtQualifiesForCategory
    /// returns true AND the Yacht cell in that column already holds a non-zero score (bonus yacht).
    /// </summary>
    public bool HasValidYachtPlacement(int[] dice)
    {
        if (!ScoreCalculator.IsYacht(dice)) return false;
        int dieValue = dice[0];

        for (int col = 0; col < _columnCount; col++)
        {
            // Can we place it directly in the Yacht cell?
            if (IsSlotAvailable(ScoreCategory.Yacht, col))
                return true;

            // Yacht cell is filled — is it non-zero (enabling bonus)?
            if (_scores[(int)ScoreCategory.Yacht, col] != ScoreCalculator.YachtPoints)
                continue;

            // Check all other categories for valid bonus placement
            foreach (ScoreCategory cat in Enum.GetValues<ScoreCategory>())
            {
                if (cat == ScoreCategory.Yacht) continue;
                if (!IsSlotAvailable(cat, col)) continue;
                if (ScoreCalculator.YachtQualifiesForCategory(cat, dieValue))
                    return true;
            }
        }
        return false;
    }

    public bool IsComplete
    {
        get
        {
            for (int col = 0; col < _columnCount; col++)
                for (int cat = 0; cat < ScoreCalculator.CategoryCount; cat++)
                    if (!_scores[cat, col].HasValue)
                        return false;
            return true;
        }
    }

    public int TurnsRemaining
    {
        get
        {
            int remaining = 0;
            for (int col = 0; col < _columnCount; col++)
                for (int cat = 0; cat < ScoreCalculator.CategoryCount; cat++)
                    if (!_scores[cat, col].HasValue)
                        remaining++;
            return remaining;
        }
    }

    public int GetUpperTotal(int column = 0)
    {
        ValidateColumn(column);
        int total = 0;
        for (int cat = (int)ScoreCategory.Ones; cat <= (int)ScoreCategory.Sixes; cat++)
            total += _scores[cat, column] ?? 0;
        return total;
    }

    public int GetUpperBonus(int column = 0)
    {
        ValidateColumn(column);
        // In Triple mode, scores are already multiplied at the cell level,
        // so the threshold scales with the column multiplier.
        int multiplier = _mode == GameMode.Triple ? column + 1 : 1;
        int threshold = ScoreCalculator.UpperBonusThreshold * multiplier;
        return GetUpperTotal(column) >= threshold
            ? ScoreCalculator.UpperBonusPoints
            : 0;
    }

    public int GetLowerTotal(int column = 0)
    {
        ValidateColumn(column);
        int total = 0;
        for (int cat = (int)ScoreCategory.ThreeOfAKind; cat <= (int)ScoreCategory.Chance; cat++)
            total += _scores[cat, column] ?? 0;
        return total;
    }

    public int GetYachtBonus(int column = 0)
    {
        ValidateColumn(column);
        return _yachtBonusCount[column] * ScoreCalculator.YachtBonusPoints;
    }

    public int GetColumnTotal(int column = 0)
    {
        return GetUpperTotal(column) + GetUpperBonus(column) +
               GetLowerTotal(column) + GetYachtBonus(column);
    }

    public int GetGrandTotal()
    {
        int total = 0;
        for (int col = 0; col < _columnCount; col++)
            total += GetColumnTotal(col);
        return total;
    }

    private void ValidateColumn(int column)
    {
        if (column < 0 || column >= _columnCount)
            throw new ArgumentOutOfRangeException(nameof(column),
                $"Column {column} out of range [0, {_columnCount})");
    }
}
