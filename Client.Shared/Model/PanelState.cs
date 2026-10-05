namespace InvestissementsDashboard.Client.Model;

public enum PanelType { AssetClass, SupportType, Risk }

public class PanelState(PanelType type)
{
    private readonly List<string> _path = [];

    public PanelType Type   { get; } = type;
    public bool CanGoBack   => _path.Count > 0;
    public int  Level       => _path.Count;

    public bool IsAtLeafLevel => Type == PanelType.Risk ? Level >= 1 : Level >= 2;

    public string? Selected(int level) => level < _path.Count ? _path[level] : null;

    public void DrillDown(string name) => _path.Add(name);

    public void GoBack()
    {
        if (CanGoBack) _path.RemoveAt(_path.Count - 1);
    }
}
