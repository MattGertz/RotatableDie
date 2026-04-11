using YachtDiceMaui.ViewModels;
using YachtDiceMaui.Models;
using YachtDiceMaui.Data;
using YachtDiceMaui.Physics;
using YachtDiceMaui.Rendering;

namespace YachtDiceMaui.Views;

public partial class GamePage : ContentPage
{
    private readonly GameViewModel _vm = new();

    // Top-level layout containers
    private Grid _rootGrid = null!;

    // Left panel (uses Grid internally so Exit stays at bottom)
    private Grid _menuPanel = null!;

    // Center panel - dice table + tray
    private Grid _dicePanel = null!;
    private DiceTableView _diceTableView = null!;
    private readonly IDicePhysics _physics = new SimpleDicePhysics();
    private readonly DiceAppearance _appearance = new();
    private Button _rollButton = null!;

    // Right panel - scorecard
    private Grid _scorecardPanel = null!;
    private Entry _playerNameEntry = null!;
    private Grid _scorecardGrid = null!;

    // State
    private bool _isLandscape;
    private readonly Random _tableRng = new();

    public GamePage()
    {
        InitializeComponent();
        BuildUI();
        _vm.DiceChanged += OnDiceChanged;
        _vm.ScorecardChanged += OnScorecardChanged;
        _vm.GameOver += OnGameOver;
        _vm.YachtDetected += OnYachtDetected;
        SizeChanged += OnPageSizeChanged;
    }

    private void BuildUI()
    {
        BuildMenuPanel();
        BuildDicePanel();
        BuildScorecardPanel();
        ApplyLayout();
    }

    // ── Menu Panel (Left) ────────────────────────────────────────

    private void BuildMenuPanel()
    {
        _menuPanel = new Grid
        {
            Padding = new Thickness(10),
            BackgroundColor = Color.FromArgb("#16213E"),
            RowSpacing = 10,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto), // New Single
                new RowDefinition(GridLength.Auto), // New Triple
                new RowDefinition(new GridLength(10)), // spacer
                new RowDefinition(GridLength.Auto), // Stats
                new RowDefinition(GridLength.Auto), // Dice Skins
                new RowDefinition(GridLength.Auto), // Rules
                new RowDefinition(GridLength.Auto), // About
                new RowDefinition(GridLength.Star), // flex spacer
                new RowDefinition(GridLength.Auto), // Exit (Windows only)
            },
        };

        int row = 0;
        AddMenuButton("New Single Game", OnNewSingleGame, row++);
        AddMenuButton("New Triple Game", OnNewTripleGame, row++);
        row++; // skip the 10px spacer row
        AddMenuButton("Stats", OnStats, row++);
        AddMenuButton("Dice Skins", OnDiceSkins, row++);
        AddMenuButton("Rules", OnRules, row++);
        AddMenuButton("About", OnAbout, row++);
        row++; // flex spacer

#if WINDOWS
        AddMenuButton("Exit", OnExit, row);
#endif
    }

    private void AddMenuButton(string text, EventHandler handler, int row)
    {
        var btn = new Button
        {
            Text = text,
            FontSize = 14,
            BackgroundColor = Color.FromArgb("#0F3460"),
            TextColor = Colors.White,
            CornerRadius = 4,
            HeightRequest = 42,
            Padding = new Thickness(12, 0),
            HorizontalOptions = LayoutOptions.Fill,
        };
        btn.Clicked += handler;
        _menuPanel.Add(btn, 0, row);
    }

    // ── Dice Panel (Center) ──────────────────────────────────────

    private void BuildDicePanel()
    {
        _dicePanel = new Grid
        {
            BackgroundColor = Color.FromArgb("#1A1A2E"),
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),      // 3D dice table
                new RowDefinition(GridLength.Auto),       // Roll button
            },
            Padding = new Thickness(5),
            RowSpacing = 5,
        };

        // 3D Dice table view (replaces AbsoluteLayout + GraphicsView)
        _diceTableView = new DiceTableView(_physics, _appearance);
        _diceTableView.DiceSettled += OnPhysicsSettled;
        _diceTableView.DieTapped += OnDieTapped;
        var tableBorder = new Border
        {
            Stroke = Color.FromArgb("#533E2D"),
            StrokeThickness = 3,
            Background = new SolidColorBrush(Color.FromArgb("#0D1117")),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Content = _diceTableView,
        };
        _dicePanel.Add(tableBorder, 0, 0);

        // Roll button
        _rollButton = new Button
        {
            Text = "Roll! (1st try)",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = Color.FromArgb("#0F3460"),
            TextColor = Colors.White,
            CornerRadius = 6,
            HeightRequest = 50,
            HorizontalOptions = LayoutOptions.Center,
            Padding = new Thickness(30, 0),
            IsEnabled = false,
            IsVisible = false,
        };
        _rollButton.Clicked += OnRollClicked;
        _dicePanel.Add(_rollButton, 0, 1);
    }

    // ── Scorecard Panel (Right) ──────────────────────────────────

    private void BuildScorecardPanel()
    {
        _scorecardPanel = new Grid
        {
            Padding = new Thickness(6),
            BackgroundColor = Color.FromArgb("#16213E"),
            RowSpacing = 0,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
            },
        };

        _playerNameEntry = new Entry
        {
            Text = _vm.PlayerName,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            BackgroundColor = Colors.Transparent,
            HorizontalTextAlignment = TextAlignment.Center,
        };
        _playerNameEntry.TextChanged += (_, e) => _vm.PlayerName = e.NewTextValue;
        _scorecardPanel.Add(_playerNameEntry, 0, 0);

        _scorecardGrid = new Grid();
        var scrollView = new ScrollView { Content = _scorecardGrid };
        _scorecardPanel.Add(scrollView, 0, 1);

        RebuildScorecardGrid();
    }

    private void RebuildScorecardGrid()
    {
        _scorecardGrid.Children.Clear();
        _scorecardGrid.RowDefinitions.Clear();
        _scorecardGrid.ColumnDefinitions.Clear();

        var sc = _vm.Scorecard;
        int colCount = sc?.ColumnCount ?? 1;

        // Columns: Category name + score columns
        _scorecardGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        for (int c = 0; c < colCount; c++)
            _scorecardGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(60)));

        int row = 0;

        // Header
        _scorecardGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        AddCell(row, 0, "", true, fontSize: 11);
        if (colCount == 1)
        {
            AddCell(row, 1, "Score", true, fontSize: 11);
        }
        else
        {
            for (int c = 0; c < colCount; c++)
                AddCell(row, c + 1, $"{c + 1}×", true, fontSize: 11);
        }
        row++;

        // Upper section header
        _scorecardGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        AddSectionHeader(row, "UPPER", colCount);
        row++;

        // Upper categories
        for (int cat = (int)ScoreCategory.Ones; cat <= (int)ScoreCategory.Sixes; cat++)
        {
            _scorecardGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            AddCategoryRow(row, (ScoreCategory)cat, colCount);
            row++;
        }

        // Upper total
        _scorecardGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        AddTotalRow(row, "Upper Total", colCount, c => sc?.GetUpperTotal(c).ToString() ?? "-");
        row++;

        // Bonus
        _scorecardGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        AddTotalRow(row, "Bonus (63+)", colCount, c =>
        {
            int bonus = sc?.GetUpperBonus(c) ?? 0;
            return bonus > 0 ? $"+{bonus}" : "-";
        });
        row++;

        // Lower section header
        _scorecardGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        AddSectionHeader(row, "LOWER", colCount);
        row++;

        // Lower categories
        for (int cat = (int)ScoreCategory.ThreeOfAKind; cat <= (int)ScoreCategory.Chance; cat++)
        {
            _scorecardGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            AddCategoryRow(row, (ScoreCategory)cat, colCount);
            row++;
        }

        // Yacht bonus
        _scorecardGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        AddTotalRow(row, "Yacht Bonus", colCount, c =>
        {
            int bonus = sc?.GetYachtBonus(c) ?? 0;
            return bonus > 0 ? $"+{bonus}" : "-";
        });
        row++;

        // Grand total
        _scorecardGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        AddCell(row, 0, "TOTAL", false, true, Colors.Gold, 14);
        if (colCount > 1)
        {
            for (int c = 0; c < colCount; c++)
                AddCell(row, c + 1, sc?.GetColumnTotal(c).ToString() ?? "0", false, true, Colors.Gold, 14);
        }
        else
        {
            AddCell(row, 1, sc?.GetGrandTotal().ToString() ?? "0", false, true, Colors.Gold, 14);
        }
    }

    private static readonly string[] CategoryNames =
    {
        "Ones", "Twos", "Threes", "Fours", "Fives", "Sixes",
        "3 of a Kind", "4 of a Kind", "Full House",
        "Sm. Straight", "Lg. Straight", "Yacht", "Chance"
    };

    private void AddCategoryRow(int row, ScoreCategory category, int colCount)
    {
        AddCell(row, 0, CategoryNames[(int)category], false, false, Colors.White, 13);

        var sc = _vm.Scorecard;
        if (sc == null)
        {
            for (int c = 0; c < colCount; c++)
                AddCell(row, c + 1, "-", false, false, Color.FromArgb("#555555"), 13);
            return;
        }

        int[] dice = _vm.GetCurrentValues();

        for (int c = 0; c < colCount; c++)
        {
            int? score = sc.GetScore(category, c);
            bool available = sc.IsSlotAvailable(category, c);

            if (score.HasValue)
            {
                // Locked score
                AddCell(row, c + 1, score.Value.ToString(), false, false, Colors.White, 13);
            }
            else if (available && _vm.CanScore)
            {
                // Potential score — clickable
                int potential = sc.GetPotentialScore(category, dice, c);
                AddScoreButton(row, c + 1, potential, category, c);
            }
            else
            {
                AddCell(row, c + 1, "-", false, false, Color.FromArgb("#555555"), 13);
            }
        }
    }

    private void AddCell(int row, int col, string text, bool isHeader, bool isBold = false,
        Color? color = null, double fontSize = 12)
    {
        var label = new Label
        {
            Text = text,
            FontSize = fontSize,
            FontAttributes = isBold || isHeader ? FontAttributes.Bold : FontAttributes.None,
            TextColor = color ?? (isHeader ? Color.FromArgb("#AAAAAA") : Colors.White),
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = col == 0 ? TextAlignment.Start : TextAlignment.Center,
            Padding = new Thickness(4, 2),
        };
        _scorecardGrid.Add(label, col, row);
    }

    private void AddScoreButton(int row, int col, int potential, ScoreCategory category, int scoreCol)
    {
        var btn = new Button
        {
            Text = potential.ToString(),
            FontSize = 13,
            BackgroundColor = potential > 0 ? Color.FromArgb("#1B5E20") : Color.FromArgb("#4A1515"),
            TextColor = potential > 0 ? Color.FromArgb("#81C784") : Color.FromArgb("#CC6666"),
            CornerRadius = 3,
            Padding = new Thickness(2, 0),
            HeightRequest = 28,
            MinimumHeightRequest = 28,
        };
        btn.Clicked += async (_, _) => await OnScoreCellTapped(category, scoreCol);
        _scorecardGrid.Add(btn, col, row);
    }

    private void AddSectionHeader(int row, string text, int colCount)
    {
        var label = new Label
        {
            Text = text,
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#B8A960"),
            HorizontalTextAlignment = TextAlignment.Center,
            Padding = new Thickness(0, 4, 0, 0),
        };
        Grid.SetColumnSpan(label, colCount + 1);
        _scorecardGrid.Add(label, 0, row);
    }

    private void AddTotalRow(int row, string name, int colCount, Func<int, string> valueGetter)
    {
        AddCell(row, 0, name, false, true, Color.FromArgb("#B8A960"), 12);
        for (int c = 0; c < colCount; c++)
            AddCell(row, c + 1, valueGetter(c), false, true, Color.FromArgb("#B8A960"), 12);
    }

    // ── Layout Management ────────────────────────────────────────

    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        bool landscape = Width > Height;
        if (landscape != _isLandscape)
        {
            _isLandscape = landscape;
            ApplyLayout();
        }
    }

    private void ApplyLayout()
    {
        // Detach panels from any previous parent
        DetachFromParent(_menuPanel);
        DetachFromParent(_dicePanel);
        DetachFromParent(_scorecardPanel);

        _rootGrid = new Grid { Padding = 0, RowSpacing = 0, ColumnSpacing = 0 };

        if (_isLandscape)
        {
            // Landscape: [Menu | Dice | Scorecard] side by side
            _rootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(170)));
            _rootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _rootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(320)));
            _rootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

            _rootGrid.Add(_menuPanel, 0, 0);
            _rootGrid.Add(_dicePanel, 1, 0);
            _rootGrid.Add(_scorecardPanel, 2, 0);
        }
        else
        {
            // Portrait: [Menu + Scorecard] on top, [Dice] on bottom
            _rootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            _rootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

            var topRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(new GridLength(170)),
                    new ColumnDefinition(GridLength.Star),
                },
                ColumnSpacing = 0,
            };
            topRow.Add(_menuPanel, 0, 0);
            topRow.Add(_scorecardPanel, 1, 0);

            _rootGrid.Add(topRow, 0, 0);
            _rootGrid.Add(_dicePanel, 0, 1);
        }

        Content = _rootGrid;
    }

    private static void DetachFromParent(View view)
    {
        if (view.Parent is Layout layout)
            layout.Remove(view);
    }

    // ── Event Handlers ───────────────────────────────────────────

    private async void OnNewSingleGame(object? sender, EventArgs e)
    {
        if (_vm.GameInProgress)
        {
            bool confirm = await DisplayAlert("New Game", "Start a new single game? Current game will be lost.", "Yes", "No");
            if (!confirm) return;
        }
        _vm.NewGame(GameMode.Normal);
        _diceTableView.ResetToStart();
        _rollButton.IsEnabled = true;
        _rollButton.IsVisible = true;
    }

    private async void OnNewTripleGame(object? sender, EventArgs e)
    {
        if (_vm.GameInProgress)
        {
            bool confirm = await DisplayAlert("New Game", "Start a new triple game? Current game will be lost.", "Yes", "No");
            if (!confirm) return;
        }
        _vm.NewGame(GameMode.Triple);
        _diceTableView.ResetToStart();
        _rollButton.IsEnabled = true;
        _rollButton.IsVisible = true;
    }

    private void OnRollClicked(object? sender, EventArgs e)
    {
        // Collect held indices
        var heldIndices = new List<int>();
        for (int i = 0; i < ScoreCalculator.NumDice; i++)
        {
            if (_diceTableView.IsHeld(i))
                heldIndices.Add(i);
        }

        // Disable roll button during animation
        _rollButton.IsEnabled = false;

        // Tell ViewModel about the roll (updates roll count/state)
        _vm.Roll();

        // Start physics roll animation
        _diceTableView.Roll(heldIndices);
    }

    /// <summary>
    /// Called when physics dice have all settled after a roll.
    /// Reads face values and updates the ViewModel dice.
    /// </summary>
    private void OnPhysicsSettled()
    {
        // Read settled face values from physics into the ViewModel
        for (int i = 0; i < ScoreCalculator.NumDice; i++)
        {
            _vm.Dice[i].Value = _diceTableView.GetFaceValue(i);
        }

        // Re-enable roll button if rolls remain
        _rollButton.IsEnabled = _vm.CanRoll;

        // Update scorecard with new values
        _vm.NotifyScorecardChanged();

        // Check for yacht
        int[] values = _vm.GetCurrentValues();
        if (ScoreCalculator.IsYacht(values) && _vm.Scorecard?.HasValidYachtPlacement(values) == true)
            _vm.RaiseYachtDetected();
    }

    private void OnDieTapped(int index)
    {
        if (_vm.RollNumber == 0) return; // Can't hold before first roll
        if (!_vm.CanScore) return; // Don't allow hold during animation

        if (_diceTableView.IsHeld(index))
        {
            _diceTableView.SetUnheld(index);
            _vm.Dice[index].IsHeld = false;
        }
        else
        {
            _diceTableView.SetHeld(index, 0); // slot is computed internally
            _vm.Dice[index].IsHeld = true;
        }
    }

    private async Task OnScoreCellTapped(ScoreCategory category, int column)
    {
        if (_vm.TryScoreCategory(category, column))
        {
            // Reset the 3D dice table to starting positions
            _diceTableView.ResetToStart();
        }
        await Task.CompletedTask;
    }

    private async void OnDiceSkins(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new DiceSkinsPage(_appearance));
    }

    private async void OnRules(object? sender, EventArgs e)
    {
        await DisplayAlert("Yacht Rules",
            "Roll 5 dice up to 3 times per turn.\n" +
            "After each roll, hold dice you want to keep.\n" +
            "Choose a scoring category after your rolls.\n\n" +
            "Upper: Sum of matching dice\n" +
            "Three/Four of a Kind: Sum of all dice\n" +
            "Full House: 25 points\n" +
            "Small Straight (4 in a row): 30 points\n" +
            "Large Straight (5 in a row): 40 points\n" +
            "Yacht (5 of a kind): 50 points\n" +
            "Chance: Sum of all dice\n\n" +
            "Upper Bonus: +35 if upper total ≥ 63\n" +
            "Yacht Bonus: +100 per additional Yacht",
            "OK");
    }

    private async void OnAbout(object? sender, EventArgs e)
    {
        await DisplayAlert("About", "Matt's Yacht v1.0\nA solitaire Yacht dice game.", "OK");
    }

#if WINDOWS
    private async void OnExit(object? sender, EventArgs e)
    {
        if (_vm.GameInProgress)
        {
            bool confirm = await DisplayAlert("Exit", "Game in progress. Are you sure you want to exit?", "Yes", "No");
            if (!confirm) return;
        }
        Application.Current?.Quit();
    }
#endif

    // ── VM Event Handlers ────────────────────────────────────────

    private void OnDiceChanged()
    {
        _rollButton.Text = _vm.RollButtonText;
        _rollButton.IsEnabled = _vm.CanRoll;
        _rollButton.IsVisible = _vm.GameInProgress;
    }

    private void OnScorecardChanged()
    {
        RebuildScorecardGrid();
    }

    private async void OnYachtDetected()
    {
        var label = new Label
        {
            Text = "YACHT!",
            FontSize = 1,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Gold,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Colors.OrangeRed),
                Offset = new Point(0, 0),
                Radius = 20,
            },
        };

        // Overlay on the dice panel
        _dicePanel.Add(label, 0, 0);

        // Animate: scale font from tiny to ~1/3 screen height then fade out
        double targetSize = Math.Max(Height / 3, 120);
        uint growMs = 600;
        uint fadeMs = 400;

        var growAnim = new Animation(v => label.FontSize = v, 1, targetSize, Easing.CubicOut);
        growAnim.Commit(label, "YachtGrow", length: growMs);

        await Task.Delay((int)growMs);

        await label.FadeTo(0, fadeMs, Easing.CubicIn);
        _dicePanel.Remove(label);
    }

    private async void OnGameOver()
    {
        var sc = _vm.Scorecard!;
        string mode = _vm.CurrentMode == GameMode.Triple ? "Triple Yacht" : "Yacht";
        int finalScore = sc.GetGrandTotal();

        // Save high score
        HighScoreStore.TryAdd(_vm.CurrentMode, sc.PlayerName, finalScore);

        await DisplayAlert("Game Over!",
            $"{mode} complete!\n\nFinal Score: {finalScore}", "OK");
    }

    private async void OnStats(object? sender, EventArgs e)
    {
        var normal = HighScoreStore.Get(GameMode.Normal);
        var triple = HighScoreStore.Get(GameMode.Triple);

        string normalText = normal != null
            ? $"{normal.Value.Name}  —  {normal.Value.Score}  —  {normal.Value.Date:MMM d, yyyy  h:mm tt}"
            : "No high score yet.";

        string tripleText = triple != null
            ? $"{triple.Value.Name}  —  {triple.Value.Score}  —  {triple.Value.Date:MMM d, yyyy  h:mm tt}"
            : "No high score yet.";

        // Build action choices
        var choices = new List<string>();
        if (normal != null) choices.Add("Clear Normal High Score");
        if (triple != null) choices.Add("Clear Triple High Score");

        string text = $"── Normal Yacht ──\n{normalText}\n\n── Triple Yacht ──\n{tripleText}";

        if (choices.Count == 0)
        {
            await DisplayAlert("High Scores", text, "OK");
            return;
        }

        string? action = await DisplayActionSheet("High Scores\n\n" + text,
            "Close", null, choices.ToArray());

        if (action == "Clear Normal High Score")
        {
            bool confirm = await DisplayAlert("Clear", "Clear the Normal Yacht high score?", "Yes", "No");
            if (confirm) { HighScoreStore.Clear(GameMode.Normal); OnStats(sender, e); }
        }
        else if (action == "Clear Triple High Score")
        {
            bool confirm = await DisplayAlert("Clear", "Clear the Triple Yacht high score?", "Yes", "No");
            if (confirm) { HighScoreStore.Clear(GameMode.Triple); OnStats(sender, e); }
        }
    }
}
