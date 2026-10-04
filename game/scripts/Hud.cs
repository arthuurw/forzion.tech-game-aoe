using Forzion.Presentation;
using Forzion.Simulation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// The HUD: a bar at the top with the human Player's Resources, population, Faction and Age
/// and a button that pauses the match, and a panel at the bottom with the selection and the orders it takes, beside the
/// <see cref="MinimapView"/>. What each shows comes from <see cref="PlayerStatus"/> and
/// <see cref="SelectionPanel"/>, and each button calls <see cref="PlayerControl"/>, which
/// sends the command; this node only lays them out. Texts come from the project's translations.
/// </summary>
public partial class Hud : CanvasLayer
{
    /// <summary>Height of the panel at the bottom of the screen, in pixels.</summary>
    public const int PanelHeight = 210;

    private const int MostUnitsShown = 24;

    /// <summary>The background of the HUD's bars and panels.</summary>
    public static readonly Color PanelColour = new(0.08f, 0.09f, 0.11f, 0.88f);

    private static readonly Color HintColour = new(0.8f, 0.85f, 0.95f);

    // The panel is rebuilt only when what it holds changes, and its buttons act on press, so a
    // rebuild never swallows a click; in between, these refresh the values shown.
    private readonly List<Action<SelectionPanel>> refreshers = [];

    // The amount of each Resource in the top bar.
    private readonly Dictionary<ResourceKind, Label> resources = [];

    private Label population = null!;
    private Label age = null!;
    private ProgressBar ageAdvanceBar = null!;
    private Label ageAdvanceLabel = null!;
    private PanelContainer bottom = null!;
    private HBoxContainer bottomContent = null!;
    private Label placementHint = null!;
    private string shownLayout = "";

    /// <summary>The match the HUD shows.</summary>
    [Export]
    public MatchView MatchView { get; set; } = null!;

    /// <summary>The input whose selection the HUD shows and through which its buttons give orders.</summary>
    [Export]
    public SelectionInput SelectionInput { get; set; } = null!;

    /// <summary>The pause that the top bar's pause button opens.</summary>
    [Export]
    public PauseMenu PauseMenu { get; set; } = null!;

    private PlayerControl PlayerControl => SelectionInput.PlayerControl;

    public override void _Ready()
    {
        var root = new Control { Name = "HudRoot", MouseFilter = Godot.Control.MouseFilterEnum.Ignore };
        root.SetAnchorsAndOffsetsPreset(Godot.Control.LayoutPreset.FullRect);
        AddChild(root);

        root.AddChild(BuildTopBar());
        root.AddChild(BuildBottomPanel());

        placementHint = NewLabel("", 18, HintColour);
        placementHint.Name = "PlacementHint";
        placementHint.HorizontalAlignment = HorizontalAlignment.Center;
        placementHint.SetAnchorsAndOffsetsPreset(Godot.Control.LayoutPreset.BottomWide);
        placementHint.OffsetTop = -PanelHeight - 40;
        placementHint.OffsetBottom = -PanelHeight - 8;
        placementHint.MouseFilter = Godot.Control.MouseFilterEnum.Ignore;
        root.AddChild(placementHint);
    }

    public override void _Process(double delta)
    {
        ShowStatus(PlayerStatus.Of(MatchView.State, MatchView.HumanPlayer));

        var panel = SelectionPanel.For(MatchView.State, MatchView.HumanPlayer, PlayerControl.Selected);
        var layout = panel.Layout;

        if (layout != shownLayout)
        {
            shownLayout = layout;
            Rebuild(panel);
        }

        foreach (var refresh in refreshers)
        {
            refresh(panel);
        }

        bottom.Visible = bottomContent.GetChildCount() > 0;

        placementHint.Visible = PlayerControl.PlacingBuilding is not null;

        if (PlayerControl.PlacingBuilding is { } placing)
        {
            placementHint.Text = Format(HudTexts.PlacementHint, Tr(TextKeys.NameOf(placing)));
        }
    }

    private Control BuildTopBar()
    {
        var bar = new PanelContainer { Name = "TopBar", MouseFilter = Godot.Control.MouseFilterEnum.Stop };
        bar.AddThemeStyleboxOverride("panel", PanelStyle());
        bar.SetAnchorsAndOffsetsPreset(Godot.Control.LayoutPreset.TopWide);
        bar.CustomMinimumSize = new Vector2(0, 40);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 28);
        bar.AddChild(row);

        foreach (var kind in Enum.GetValues<ResourceKind>())
        {
            resources[kind] = NewLabel("", 18, ResourceColour(kind));
            row.AddChild(resources[kind]);
        }

        population = NewLabel("", 18, new Color(0.85f, 0.9f, 1f));
        row.AddChild(population);

        row.AddChild(new Control { SizeFlagsHorizontal = Godot.Control.SizeFlags.ExpandFill, MouseFilter = Godot.Control.MouseFilterEnum.Ignore });

        ageAdvanceLabel = NewLabel("", 16, HintColour);
        ageAdvanceBar = NewBar(new Color(0.55f, 0.75f, 1f), new Vector2(160, 14));
        ageAdvanceBar.SizeFlagsVertical = Godot.Control.SizeFlags.ShrinkCenter;
        row.AddChild(ageAdvanceLabel);
        row.AddChild(ageAdvanceBar);

        age = NewLabel("", 18, new Color(1f, 0.95f, 0.8f));
        row.AddChild(age);

        var pause = new Button { Name = "Pause", Text = Tr(HudTexts.Pause), FocusMode = Godot.Control.FocusModeEnum.None };
        pause.Pressed += PauseMenu.Pause;
        row.AddChild(pause);

        return bar;
    }

    private Control BuildBottomPanel()
    {
        bottom = new PanelContainer { Name = "SelectionPanel", MouseFilter = Godot.Control.MouseFilterEnum.Stop };
        bottom.AddThemeStyleboxOverride("panel", PanelStyle());
        bottom.SetAnchorsAndOffsetsPreset(Godot.Control.LayoutPreset.BottomWide);
        bottom.OffsetTop = -PanelHeight;

        // The minimap takes the bottom right corner.
        bottom.OffsetRight = -MinimapView.FrameWidth;

        bottomContent = new HBoxContainer { Name = "Content" };
        bottomContent.AddThemeConstantOverride("separation", 24);
        bottom.AddChild(bottomContent);

        return bottom;
    }

    private void ShowStatus(PlayerStatus status)
    {
        foreach (var resource in status.Resources)
        {
            resources[resource.Kind].Text = Format(HudTexts.Labelled, Tr(TextKeys.NameOf(resource.Kind)), resource.Amount);
        }

        population.Text = Format(
            HudTexts.Labelled,
            Tr(HudTexts.Population),
            Format(HudTexts.PopulationOfLimit, status.Population, status.PopulationLimit));
        age.Text = Format(HudTexts.FactionAndAge, Tr(status.FactionNameKey), Tr(status.AgeNameKey));

        var advance = status.AgeAdvance;
        ageAdvanceLabel.Visible = advance is not null;
        ageAdvanceBar.Visible = advance is not null;

        if (advance is not null)
        {
            ageAdvanceLabel.Text = HudTexts.AdvancingText(advance.AgeNameKey, advance.Progress, key => Tr(key));
            ageAdvanceBar.Value = advance.Progress;
        }
    }

    private void Rebuild(SelectionPanel panel)
    {
        refreshers.Clear();

        foreach (var child in bottomContent.GetChildren())
        {
            bottomContent.RemoveChild(child);
            child.QueueFree();
        }

        if (panel.Building is { } building)
        {
            bottomContent.AddChild(BuildingInfo(building));
            bottomContent.AddChild(BuildingOrders(building));

            if (building.TrainingQueue.Count > 0)
            {
                bottomContent.AddChild(TrainingQueue(building.TrainingQueue));
            }
        }
        else if (panel.Units.Count > 0)
        {
            bottomContent.AddChild(UnitsInfo(panel.Units));

            if (panel.BuildingChoices.Count > 0)
            {
                bottomContent.AddChild(BuildingChoices(panel.BuildingChoices));
            }
        }
    }

    private Control BuildingInfo(SelectedBuilding building)
    {
        var column = NewColumn(300);
        column.AddChild(NewLabel(Tr(building.NameKey), 22, Colors.White));
        column.AddChild(HitPointsRow(panel => panel.Building!.HitPoints, panel => panel.Building!.MaxHitPoints));

        if (building.ConstructionProgress is not null)
        {
            var label = NewLabel("", 16, HintColour);
            var bar = NewBar(BarColours.Construction, new Vector2(260, 14));
            column.AddChild(label);
            column.AddChild(bar);
            refreshers.Add(panel =>
            {
                var progress = panel.Building!.ConstructionProgress ?? 1;
                label.Text = Format(HudTexts.Labelled, Tr(HudTexts.Construction), Percent(progress));
                bar.Value = progress;
            });
        }
        else if (building.UnitChoices.Count > 0)
        {
            var hint = NewLabel(Tr(HudTexts.RallyPointHint), 14, HintColour);
            hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            column.AddChild(hint);
        }

        return column;
    }

    private Control BuildingOrders(SelectedBuilding building)
    {
        var column = NewColumn(0);

        if (building.UnitChoices.Count > 0)
        {
            column.AddChild(NewLabel(Tr(HudTexts.Train), 16, HintColour));
            var row = new HBoxContainer();
            column.AddChild(row);

            foreach (var choice in building.UnitChoices)
            {
                var kind = choice.Kind;
                var button = ChoiceButton(Tr(choice.NameKey), choice.Cost, choice.LockedUntilAgeNameKey);
                button.Name = $"Train{kind}";
                button.Pressed += () => PlayerControl.Train(kind);
                row.AddChild(button);
            }
        }

        if (building.AgeAdvance is { } advance)
        {
            column.AddChild(AgeAdvanceControl(advance));
        }

        return column;
    }

    private Control AgeAdvanceControl(AgeAdvanceChoice advance)
    {
        if (!advance.IsUnderway)
        {
            var button = new Button
            {
                Name = "AdvanceAge",
                ActionMode = BaseButton.ActionModeEnum.Press,
                Text = $"{Format(HudTexts.AdvanceTo, Tr(advance.AgeNameKey))}\n{CostText(advance.Cost)}",
                SizeFlagsHorizontal = Godot.Control.SizeFlags.ShrinkBegin,
            };
            button.Pressed += PlayerControl.AdvanceAge;

            return button;
        }

        var box = new VBoxContainer();
        var label = NewLabel("", 16, HintColour);
        var bar = NewBar(new Color(0.55f, 0.75f, 1f), new Vector2(260, 14));
        box.AddChild(label);
        box.AddChild(bar);
        refreshers.Add(panel =>
        {
            if (panel.Building?.AgeAdvance is { Progress: { } progress } underway)
            {
                label.Text = HudTexts.AdvancingText(underway.AgeNameKey, progress, key => Tr(key));
                bar.Value = progress;
            }
        });

        return box;
    }

    private Control TrainingQueue(IReadOnlyList<QueuedUnit> queue)
    {
        var box = NewColumn(0);
        box.AddChild(NewLabel(Tr(HudTexts.TrainingQueue), 16, HintColour));
        var row = new HFlowContainer { CustomMinimumSize = new Vector2(320, 0) };
        box.AddChild(row);

        for (var position = 0; position < queue.Count; position++)
        {
            var at = position;
            var entry = new VBoxContainer();
            var button = new Button
            {
                Name = $"Queued{at}",
                Text = Tr(queue[at].NameKey),
                CustomMinimumSize = new Vector2(96, 32),
                ActionMode = BaseButton.ActionModeEnum.Press,
            };
            button.Pressed += () => PlayerControl.CancelTraining(at);
            entry.AddChild(button);

            if (at == 0)
            {
                var bar = NewBar(new Color(0.45f, 0.85f, 0.45f), new Vector2(96, 8));
                entry.AddChild(bar);
                refreshers.Add(panel =>
                {
                    if (panel.Building is { TrainingQueue.Count: > 0 } building)
                    {
                        bar.Value = building.TrainingQueue[0].Progress;
                    }
                });
            }

            row.AddChild(entry);
        }

        var hint = NewLabel(Tr(HudTexts.CancelTrainingHint), 14, HintColour);
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        hint.CustomMinimumSize = new Vector2(320, 0);
        box.AddChild(hint);

        return box;
    }

    private Control UnitsInfo(IReadOnlyList<SelectedUnit> units)
    {
        var column = NewColumn(300);

        if (units.Count == 1)
        {
            column.AddChild(NewLabel(Tr(units[0].NameKey), 22, Colors.White));
            column.AddChild(HitPointsRow(panel => panel.Units[0].HitPoints, panel => panel.Units[0].MaxHitPoints));

            return column;
        }

        column.AddChild(NewLabel(Format(HudTexts.SelectedUnits, units.Count), 20, Colors.White));
        var grid = new GridContainer { Columns = 8 };
        grid.AddThemeConstantOverride("h_separation", 10);
        column.AddChild(grid);

        for (var index = 0; index < Math.Min(units.Count, MostUnitsShown); index++)
        {
            var at = index;
            var entry = new VBoxContainer();
            entry.AddChild(NewLabel(Tr(units[at].NameKey), 13, Colors.White));
            var bar = NewBar(BarColours.HitPoints(1), new Vector2(90, 6));
            entry.AddChild(bar);
            grid.AddChild(entry);
            refreshers.Add(panel => ShowHitPoints(bar, panel.Units[at].HitPoints, panel.Units[at].MaxHitPoints));
        }

        return column;
    }

    private Control BuildingChoices(IReadOnlyList<BuildingChoice> choices)
    {
        var column = NewColumn(0);
        column.AddChild(NewLabel(Tr(HudTexts.Build), 16, HintColour));
        var row = new HBoxContainer();
        column.AddChild(row);

        foreach (var choice in choices)
        {
            var kind = choice.Kind;
            var button = ChoiceButton(Tr(choice.NameKey), choice.Cost, choice.LockedUntilAgeNameKey);
            button.Name = $"Build{kind}";
            button.Pressed += () => PlayerControl.ChooseBuilding(kind);
            row.AddChild(button);
        }

        return column;
    }

    /// <summary>The hit points as a bar and as numbers, refreshed every frame.</summary>
    private Control HitPointsRow(Func<SelectionPanel, int> hitPoints, Func<SelectionPanel, int> maxHitPoints)
    {
        var box = new VBoxContainer();
        var label = NewLabel("", 15, HintColour);
        var bar = NewBar(BarColours.HitPoints(1), new Vector2(260, 12));
        box.AddChild(label);
        box.AddChild(bar);
        refreshers.Add(panel =>
        {
            label.Text = Format(
                HudTexts.Labelled,
                Tr(HudTexts.HitPoints),
                Format(HudTexts.HitPointsOfMax, hitPoints(panel), maxHitPoints(panel)));
            ShowHitPoints(bar, hitPoints(panel), maxHitPoints(panel));
        });

        return box;
    }

    /// <summary>A button naming a unit or building and its cost; disabled, naming the Age that unlocks it, while locked.</summary>
    private Button ChoiceButton(string name, Cost cost, string? lockedUntilAgeNameKey)
    {
        var button = new Button
        {
            Text = lockedUntilAgeNameKey is null
                ? $"{name}\n{CostText(cost)}"
                : $"{name}\n{Format(HudTexts.LockedUntil, Tr(lockedUntilAgeNameKey))}",
            Disabled = lockedUntilAgeNameKey is not null,
            CustomMinimumSize = new Vector2(150, 56),
            ActionMode = BaseButton.ActionModeEnum.Press,
        };

        return button;
    }

    private static void ShowHitPoints(ProgressBar bar, int hitPoints, int maxHitPoints)
    {
        var fraction = Fractions.Of(hitPoints, maxHitPoints);
        bar.Value = fraction;
        ((StyleBoxFlat)bar.GetThemeStylebox("fill")).BgColor = BarColours.HitPoints(fraction);
    }

    /// <summary>The colour of the amount of a Resource in the top bar.</summary>
    private static Color ResourceColour(ResourceKind kind) => kind switch
    {
        ResourceKind.Food => new Color(0.95f, 0.55f, 0.6f),
        ResourceKind.Wood => new Color(0.85f, 0.65f, 0.4f),
        ResourceKind.Gold => new Color(1f, 0.85f, 0.35f),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown Resource."),
    };

    private string CostText(Cost cost) => HudTexts.CostText(cost, key => Tr(key));

    private string Percent(double fraction) => HudTexts.Percent(fraction, key => Tr(key));

    private string Format(string key, params object[] arguments) => HudTexts.Format(text => Tr(text), key, arguments);

    private static VBoxContainer NewColumn(int width)
    {
        var column = new VBoxContainer { CustomMinimumSize = new Vector2(width, 0) };
        column.AddThemeConstantOverride("separation", 6);

        return column;
    }

    private static Label NewLabel(string text, int size, Color colour)
    {
        var label = new Label { Text = text, MouseFilter = Godot.Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", colour);

        return label;
    }

    private static ProgressBar NewBar(Color fill, Vector2 size)
    {
        var bar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 1,
            Step = 0,
            ShowPercentage = false,
            CustomMinimumSize = size,
            SizeFlagsHorizontal = Godot.Control.SizeFlags.ShrinkBegin,
            MouseFilter = Godot.Control.MouseFilterEnum.Ignore,
        };
        bar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.6f) });
        bar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = fill });

        return bar;
    }

    private static StyleBoxFlat PanelStyle() => new()
    {
        BgColor = PanelColour,
        ContentMarginLeft = 16,
        ContentMarginRight = 16,
        ContentMarginTop = 8,
        ContentMarginBottom = 8,
    };
}
