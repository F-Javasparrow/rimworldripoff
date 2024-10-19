using Godot;
using System.Collections.Generic;

[Tool]
public partial class Wall : Node
{
    public List<Requirement> requirements = new List<Requirement>
    {
        new Requirement { Name = "Wood", Amount = 20 }
    };

    private Dictionary<string, int> requirementsReady = new Dictionary<string, int>();
    private Dictionary<string, int> requirementsOnTheWay = new Dictionary<string, int>();

    [Export] public int tileMapIndex;
    [Export] public Vector2I tileMapPos;
    [Export] public UI.PlacingMode placingMode;
    [Export] public float buildDifficulty = 1;

    private float buildProgress = 0;

    public Vector2 position;

    public Vector2I GetTileMapPos()
    {
        return tileMapPos;
    }

    public void InitRequirements()
    {
        foreach (var requirement in requirements)
        {
            requirementsOnTheWay[requirement.Name] = 0;
            requirementsReady[requirement.Name] = 0;
        }
    }

    public List<Requirement> GetRequirements()
    {
        return requirements;
    }

    public (string, int) GetRequirement(string requirementName)
    {
        foreach (var requirement in requirements)
        {
            if (requirement.Name == requirementName)
            {
                int amount = requirement.Amount;
                amount -= requirementsOnTheWay[requirementName];
                amount -= requirementsReady[requirementName];
                return (requirementName, amount);
            }
        }
        return (requirementName, 0);
    }

    public bool DoesNeedRequirement(string requirementName)
    {
        return GetRequirement(requirementName).Item2 > 0;
    }

    public bool HasAllRequirements()
    {
        foreach (var requirement in requirements)
        {
            if (GetRequirement(requirement.Name).Item2 > 0)
            {
                return false;
            }
        }
        return true;
    }

    public void ReportRequirementOnTheWay(string requirementName, int amount)
    {
        requirementsOnTheWay[requirementName] += amount;
    }

    public void CancelRequirementOnTheWay(string requirementName, int amount)
    {
        requirementsOnTheWay[requirementName] -= amount;
    }

    public void DeliverRequirement(string requirementName, int amount)
    {
        requirementsReady[requirementName] += amount;
    }

    public bool TryBuild(float amount)
    {
        buildProgress += amount * (1 / buildDifficulty);
        if (IsBuilt())
        {
            // 这里调用 terrain 的方法
            return true;
        }
        return false;
    }

    public bool IsBuilt()
    {
        return buildProgress >= 1;
    }

    public string GetCount()
    {
        if (IsBuilt())
        {
            return "";
        }
        string str = "";
        foreach (var requirement in requirements)
        {
            str += $"{requirement.Name}: {requirementsReady[requirement.Name]} / {requirement.Amount}\n";
        }
        return str;
    }

    public string GetName()
    {
        if (IsBuilt())
        {
            return Name;
        }
        return $"{Name}    {Mathf.FloorToInt(buildProgress * 100)}%";
    }

    public class Requirement
    {
        public string Name;
        public int Amount;
    }
}
