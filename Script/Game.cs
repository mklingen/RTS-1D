using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents the main game class, inheriting from Node3D.
/// </summary>
public partial class Game : Node3D
{
    /// <summary>
    /// Singleton instance of the Game class.
    /// </summary>
    private static Game instance;

    /// <summary>
    /// The planet associated with the game.
    /// </summary>
    [Export]
    private Planet planet;

    /// <summary>
    /// Dictionary of teams in the game, indexed by integer.
    /// </summary>
    private Dictionary<int, Team> Teams = new Dictionary<int, Team>();

    private Dictionary<int, Color> DefaultTeamColors = new Dictionary<int, Color>
    {
        {0, new Color(0, 0, 1) },
        {1, new Color(1, 0, 0) },
        {2, new Color(0, 1, 0) },
        {3, new Color(1, 1, 0) }
    };

    /// <summary>
    /// Represents a team in the game.
    /// </summary>
    public class Team
    {
        public delegate void OnResourcesChangedEvent(float resources);
        public event OnResourcesChangedEvent OnResourcesChanged;

        /// <summary>
        /// Index of the team.
        /// </summary>
        public int Index;

        private float resources;
        /// <summary>
        /// Resources available to the team.
        /// </summary>
        public float Resources
        {
            get
            {
                return resources;
            }
            set
            {
                resources = value;
                OnResourcesChanged?.Invoke(resources);
            }
        }

        /// <summary>
        /// List of units belonging to the team.
        /// </summary>
        public List<Unit> Units = new List<Unit>();

        /// <summary>
        /// Adds a unit to the team's list of units.
        /// </summary>
        /// <param name="unit">The unit to add.</param>
        public void AddUnit(Unit unit)
        {
            unit.OnDeath += (me) =>
            {
                Units.Remove(me);
            };
            Units.Add(unit);
        }

        /// <summary>
        /// Retrieves all drop-off points associated with the team's units.
        /// </summary>
        /// <returns>An enumerable collection of ResourceDropOff objects.</returns>
        public IEnumerable<ResourceDropOff> GetDropOffs()
        {
            foreach (var unit in Units) {
                foreach (ResourceDropOff dropOff in unit.GetDropOffs()) {
                    yield return dropOff;
                }
            }
        }

        public Color Color;
        public StandardMaterial3D TeamColorMaterial;
    }

    /// <summary>
    /// Adds a unit to the corresponding team's list of units or creates a new team if it doesn't exist.
    /// </summary>
    /// <param name="unit">The unit to add to the team.</param>
    public void AddUnit(Unit unit)
    {
        var team = GetTeam(unit.Team);
        // Add the unit to the corresponding team's list of units
        team.AddUnit(unit);
    }

    /// <summary>
    /// Get the team with the given index.
    /// </summary>
    public Team GetTeam(int idx)
    {
        if (!Teams.ContainsKey(idx)) {
            Teams[idx] = new Team() { Index = idx, Resources = 100.0f, Units = new List<Unit>(), 
                                     Color = DefaultTeamColors[idx]
            };
        }
        return Teams[idx];
    }

    public interface IDamageable
    {
        public void DoDamage(float damage, float armorPiercing);
    }


    public interface ITeamObject
    {
        public int Team { get; set; }
    }

    public static float GetTime()
    {
        return Time.GetTicksMsec() / 1000.0f;
    }

    public Planet GetPlanet()
    {
        if (planet == null) {
            planet = FindChild("Planet") as Planet;
        }
        return planet;
    }

    public static Game Get()
    {
        return instance;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        instance = this;
    }

    public override void _Ready()
    {
        base._Ready();
        planet = FindChild("Planet") as Planet;
        if (planet == null) {
            GD.PrintErr("Could not find the planet named Planet under root.");
            return;
        }
    }

    public static IEnumerable<T> FindChildrenRecursive<T>(Node parent) where T : Node
    {
        if (parent is T) {
            yield return parent as T;
        }
        foreach (var child in parent.GetChildren(true)) {
            foreach (var subchild in FindChildrenRecursive<T>(child)) {
                yield return subchild;
            }
        }
    }

    public static IEnumerable<T> FindInterfacesRecursive<T>(Node parent) where T : class
    {
        if (parent is T) {
            yield return parent as T;
        }
        foreach (var child in parent.GetChildren(true)) {
            foreach (var subchild in FindInterfacesRecursive<T>(child)) {
                yield return subchild;
            }
        }
    }

    public static T FindParent<T>(Node child) where T : class
    {
        if (child is T) {
            return child as T;
        }
        if (child.GetParent() == null) {
            return null;
        }
        return FindParent<T>(child.GetParent());
    }

    public static Vector3 RandomVector3(float magnitude)
    {
        return new Vector3((float)GD.RandRange(-magnitude, magnitude), (float)GD.RandRange(-magnitude, magnitude), (float)GD.RandRange(-magnitude, magnitude));
    }

    public static void ClearChildren(Node node)
    {
        List<Node> children = node.GetChildren(true).ToList();
        foreach (var child in children) {
            ClearChildren(child);
            node.RemoveChild(child);
            child.QueueFree();
        }
    }

    // Gets the Node3D which is a direct descendant of the parent, assuming child is an indirect descendant. 
    public static Node3D GetRootEntity(Node3D parent, Node3D child)
    {
        if (child == parent) {
            return child;
        }
        Node next = child.GetParent();
        if (next == null) {
            return null;
        }
        if (next == parent) {
            return child;
        }
        if (next is Node3D) {
            return GetRootEntity(parent, next as Node3D);
        }
        return null;
    }

}
