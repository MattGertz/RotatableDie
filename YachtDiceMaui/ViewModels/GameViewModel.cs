using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YachtDiceMaui.Models;

namespace YachtDiceMaui.ViewModels;

public class GameViewModel : INotifyPropertyChanged
{
    private Scorecard? _scorecard;
    private int _rollNumber;
    private bool _gameInProgress;
    private string _playerName = "Matt";
    private string _rollButtonText = "Roll! (1st try)";
    private bool _canRoll;
    private bool _canScore;
    private GameMode _currentMode;

    // Undo state
    private record UndoState(
        ScoreCategory Category, int Column, bool WasYachtBonus,
        int[] DiceValues, bool[] DiceHeld,
        int RollNum, bool CouldRoll, bool CouldScore, string ButtonText);
    private UndoState? _undoState;

    public ObservableCollection<Die> Dice { get; } = new();
    public Scorecard? Scorecard => _scorecard;

    public string PlayerName
    {
        get => _playerName;
        set
        {
            _playerName = value;
            if (_scorecard != null) _scorecard.PlayerName = value;
            OnPropertyChanged();
        }
    }

    public string RollButtonText
    {
        get => _rollButtonText;
        private set { _rollButtonText = value; OnPropertyChanged(); }
    }

    public bool CanRoll
    {
        get => _canRoll;
        private set { _canRoll = value; OnPropertyChanged(); }
    }

    public bool CanScore
    {
        get => _canScore;
        private set { _canScore = value; OnPropertyChanged(); }
    }

    public bool GameInProgress
    {
        get => _gameInProgress;
        private set { _gameInProgress = value; OnPropertyChanged(); }
    }

    public int RollNumber
    {
        get => _rollNumber;
        private set { _rollNumber = value; OnPropertyChanged(); }
    }

    public GameMode CurrentMode => _currentMode;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? DiceChanged;
    public event Action? ScorecardChanged;
    public event Action? GameOver;
    public event Action? YachtDetected;
    public event Action? UndoStateChanged;

    public bool CanUndo => _undoState != null;

    public GameViewModel()
    {
        for (int i = 0; i < ScoreCalculator.NumDice; i++)
            Dice.Add(new Die());
    }

    public void NewGame(GameMode mode)
    {
        _currentMode = mode;
        _scorecard = new Scorecard(mode) { PlayerName = _playerName };
        RollNumber = 0;
        CanRoll = true;
        CanScore = false;
        GameInProgress = true;
        RollButtonText = "Roll! (1st try)";
        _undoState = null;
        UndoStateChanged?.Invoke();

        foreach (var die in Dice)
            die.Reset();

        OnPropertyChanged(nameof(Scorecard));
        ScorecardChanged?.Invoke();
        DiceChanged?.Invoke();
    }

    public void Roll()
    {
        if (!CanRoll || _scorecard == null) return;

        _undoState = null;
        UndoStateChanged?.Invoke();

        RollNumber++;
        // Don't randomize dice values here — physics engine handles rolling.
        // Face values will be read back from physics in OnPhysicsSettled.

        CanScore = true;
        CanRoll = RollNumber < 3;

        RollButtonText = RollNumber switch
        {
            1 => "Roll! (2nd try)",
            2 => "Roll! (3rd try)",
            _ => "Score to continue"
        };

        DiceChanged?.Invoke();
    }

    /// <summary>
    /// Notify that the scorecard should be refreshed (after physics settle updates values).
    /// </summary>
    public void NotifyScorecardChanged() => ScorecardChanged?.Invoke();

    /// <summary>
    /// Raise the YachtDetected event (called from GamePage after physics settle).
    /// </summary>
    public void RaiseYachtDetected() => YachtDetected?.Invoke();

    public int[] GetCurrentValues() =>
        Dice.Select(d => d.Value).ToArray();

    public bool TryScoreCategory(ScoreCategory category, int column)
    {
        if (_scorecard == null || !CanScore) return false;

        int[] dice = GetCurrentValues();
        bool[] held = Dice.Select(d => d.IsHeld).ToArray();

        // Save undo state before scoring
        var savedUndo = new UndoState(
            category, column, false,
            (int[])dice.Clone(), held,
            RollNumber, CanRoll, CanScore, RollButtonText);

        var result = _scorecard.TryScore(category, column, dice);
        if (result == Scorecard.ScoreResult.Failed)
            return false;

        // Record whether it was a yacht bonus
        bool wasYachtBonus = result == Scorecard.ScoreResult.YachtBonus;
        _undoState = savedUndo with { WasYachtBonus = wasYachtBonus };
        UndoStateChanged?.Invoke();

        // Reset for next turn
        RollNumber = 0;
        CanRoll = true;
        CanScore = false;
        RollButtonText = "Roll! (1st try)";

        foreach (var die in Dice)
            die.Reset();

        ScorecardChanged?.Invoke();
        DiceChanged?.Invoke();

        if (_scorecard.IsComplete)
        {
            GameInProgress = false;
            CanRoll = false;
            RollButtonText = "Game Over";
            DiceChanged?.Invoke();
            GameOver?.Invoke();
        }

        return true;
    }

    public bool Undo()
    {
        if (_undoState == null || _scorecard == null) return false;

        var u = _undoState;
        _undoState = null;

        // Reverse the score
        _scorecard.UndoScore(u.Category, u.Column, u.WasYachtBonus);

        // Restore turn state
        RollNumber = u.RollNum;
        CanRoll = u.CouldRoll;
        CanScore = u.CouldScore;
        RollButtonText = u.ButtonText;
        GameInProgress = true;

        // Restore dice values and held states
        for (int i = 0; i < Dice.Count; i++)
        {
            Dice[i].Value = u.DiceValues[i];
            Dice[i].IsHeld = u.DiceHeld[i];
        }

        UndoStateChanged?.Invoke();
        ScorecardChanged?.Invoke();
        DiceChanged?.Invoke();
        return true;
    }

    public void ToggleDieHold(int index)
    {
        if (index < 0 || index >= Dice.Count) return;
        if (RollNumber == 0) return; // Can't hold before first roll
        Dice[index].ToggleHold();
        DiceChanged?.Invoke();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
