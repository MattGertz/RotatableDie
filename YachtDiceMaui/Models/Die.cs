using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace YachtDiceMaui.Models;

public class Die : INotifyPropertyChanged
{
    private static readonly Random _rng = new();

    private int _value = 1;
    private bool _isHeld;

    public int Value
    {
        get => _value;
        set { _value = value; OnPropertyChanged(); }
    }

    public bool IsHeld
    {
        get => _isHeld;
        set { _isHeld = value; OnPropertyChanged(); }
    }

    public void Roll()
    {
        if (!IsHeld)
            Value = _rng.Next(1, 7);
    }

    public void ToggleHold()
    {
        IsHeld = !IsHeld;
    }

    public void Reset()
    {
        Value = 1;
        IsHeld = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
