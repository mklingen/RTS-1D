using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static Planet;

public partial class Planet : Node3D
{
    [Export]
    public float Radius = 1.0f;


    public Vector3 ProjectToSphere(Vector3 worldPos, float height = 0)
    {
        return ToGlobal(ToLocal(worldPos).Normalized() * (Radius + height));
    }

    public Vector3 ProjectToCylinder(Vector3 worldPos, float height, float depth, float angleOffset = 0)
    {
        Vector3 local = ToLocal(worldPos);
        float r = Radius + height;
        float theta = Mathf.Atan2(local.Y, local.X) + angleOffset;
        return ToGlobal(new Vector3(r * Mathf.Cos(theta), r * Mathf.Sin(theta), -depth));
    }


    public Vector3 ProjectToSurface(Vector3 worldPos, float height = 0)
    {
        Vector3 local = ToLocal(worldPos);
        float r = Radius + height;
        float theta = Mathf.Atan2(local.Y, local.X);
        return ToGlobal(new Vector3(r * Mathf.Cos(theta), r * Mathf.Sin(theta), local.Z));
    }


    public float GetLatitude(Vector3 worldPos)
    {
        Vector3 local = ToLocal(worldPos);
        return Mathf.Atan2(local.Y, local.X);
    }

    public bool Collides(Vector3 worldPos)
    {
        Vector3 local = ToLocal(worldPos);
        return local.Length() < Radius;
    }

    // Represents a tangent frame when looking at the planet from facing up its z axis.
    // The tangent frame is always pointed such that "up" points out of the planet.
    public struct TangentFrame
    {
        // Points out of the planet.
        public Vector3 Up;
        // Points to the right when "up" is pointing out of the planet.
        public Vector3 Right;
        // POints to the left when "up" is pointing out of the planet.
        public Vector3 Left;
        // Points along the planet's negative z axis.
        public Vector3 In;
        // Points along the planet's z axis.
        public Vector3 Out;

        public Basis ToBasis(float scale = 1.0f)
        {
            return new Basis(Left * scale, Up * scale, In * scale);
        }
    }

    public TangentFrame GetTangentFrame(Vector3 worldPos)
    {
        Vector3 local = ToLocal(worldPos).Normalized();
        Vector3 local2d = new Vector3(local.X, local.Y, 0).Normalized();
        // Up vector is the normalized position vector
        Vector3 up = local2d;

        // In vector is the negative of the global Z axis
        Vector3 In = -Basis.Z.Normalized();

        // Right vector is the cross product of In and Up (normalized)
        Vector3 right = In.Cross(up).Normalized();

        // Left vector is the negative of the Right vector
        Vector3 left = -right;


        Vector3 Out = -In;
        return new TangentFrame
        {
            Up = up,
            Right = right,
            Left = left,
            In = In,
            Out = Out
        };
    }

    // Represents a modular grid of type T, which has moudlar (wraparound) characteristics.
    public class Grid<T> where T : new()
    {
        // Flat array of cells.
        protected T[] Cells;

        public void Init()
        {
            for (int k = 0; k < Cells.Length; k++) {
                Cells[k] = new T();
            }
        }

        public Grid(int numCells)
        {
            Cells = new T[numCells];
        }

        public int WrapIndex(int idx)
        {
            if (idx < 0) {
                return (Cells.Length + idx % Cells.Length);
            } else {
                return idx % Cells.Length;
            }
        }

        // Gets the cell at the given index.
        public T Get(int idx)
        {
            return Cells[WrapIndex(idx)];
        }

        // Sets the cell at the given index.
        public void Set(int idx, T cell)
        {
            Cells[WrapIndex(idx)] = cell;
        }

        // Indexing operator.
        public T this[int idx]
        {
            get { return Get(idx); }
            set { Set(idx, value); }
        }

        // Gets the cells adjacent to the given index.
        public IEnumerable<T> GetAdjacent(int idx, int count=1)
        {
            for (int k = -count; k <= count; k++) {
                if (k != 0) {
                    yield return Get(idx + k);
                }
            }
        }

    }

    // A modular grid that indexes via angle (or x, y).
    public class AngleGrid<T> : Grid<T> where T : new()
    {
        // Size of the grid cells in angle (radians).
        private float AngularResolution = 1.0f;
        public AngleGrid(float angularResolution) : base((int)((Mathf.Pi * 2.0f) / angularResolution))
        {
            AngularResolution = angularResolution;
        }

        // Gets the index a the x, y coordinate.
        public int IndexOf(float x, float y)
        {           
            // -PI to PI.
            float wrapped = Mathf.Atan2(y, x);
            return WrapIndex((int)(wrapped / AngularResolution));

        }

        // Index of the given angle (radians).
        public int IndexOf(float angle)
        {
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);
            return IndexOf(x, y);
        }

        // Angle at the center of the cell at the given index.
        public float AngleOf(int index)
        {
            // Angle is ambiguous over the range of indices, so return the angle that is half way through the angular grid cell.
            return index * AngularResolution + AngularResolution * 0.5f;
        }
        
        // Position at the index.
        public Vector3 PositionOf(float radius, int index)
        {
            float angle = AngleOf(WrapIndex(index));
            return new Vector3(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle), 0.0f);
        }

        // Get the cell at the given x, y coordinate.
        public T Get(float x, float y)
        {
            return Get(IndexOf(x, y));
        }

        // Get the cells adjacent to the given x, y coordinate.
        public IEnumerable<T> GetAdjacent(float x, float y, int count=2)
        {
            int idx = IndexOf(x, y);
            foreach (T adj in GetAdjacent(idx, count)) {
                yield return adj;
            }
        }

        // Gets all the indices adjacent to the given x, y, coordinate.
        public IEnumerable<int> GetAdjacentIndices(float x, float y, int count=2, bool includeCenter=false)
        {
            int idx = IndexOf(x, y);
            for(int k = -count; k <= count; k++) {
                if (includeCenter || k != 0) {
                    yield return WrapIndex(idx + k);
                }
            }
        }

        public int Length()
        {
            return this.Cells.Length;
        }
    }


    [ExportGroup("Grids")]
    [Export] private float gridResolutionDegrees = 5;
    public AngleGrid<GridCell> PlanetGrid;



    public class GridCell
    {
        public enum Layer
        {
            Buildings,
            Units,
            Resources
        }

        public Dictionary<Layer, HashSet<Node3D>> Objects = null;

        
        public HashSet<Node3D> GetObjects(Layer layer)
        {
            if (Objects == null) {
                return null;
            }
            if (!Objects.ContainsKey(layer)) {
                return null;
            }
            return Objects[layer];
        }

        public bool HasAny(Layer layer)
        {
            HashSet<Node3D> objectsAtLayer = GetObjects(layer);
            if (objectsAtLayer == null) {
                return false;
            }
            return objectsAtLayer.Count > 0;
        }

        public void AddObject(Layer layer, Node3D planetObject)
        {
            if (Objects == null) {
                Objects = new Dictionary<Layer, HashSet<Node3D>>();
            }
            if (!Objects.ContainsKey(layer)) {
                Objects[layer] = new HashSet<Node3D>();
            }
            Objects[layer].Add(planetObject);
        }

        public bool RemoveObject(Layer layer, Node3D planetObject)
        {
            HashSet<Node3D> objectsAtLayer = GetObjects(layer);
            if (objectsAtLayer == null) {
                return false;
            }
            return objectsAtLayer.Remove(planetObject);
        }


        public GridCell()
        {
        }
    }

    public override void _Ready()
    {
        base._Ready();
        PlanetGrid = new AngleGrid<GridCell>(Mathf.DegToRad(gridResolutionDegrees));
        PlanetGrid.Init();
    }

    public bool CanBuildBuilding(Vector3 location)
    {
        return !PlanetGrid.Get(location.X, location.Y).HasAny(GridCell.Layer.Buildings);
    }

    public void AddBuilding(PlanetObject building, Vector3 location, int numCells = 3)
    {
        AddObject(building, GridCell.Layer.Buildings, location, numCells);
    }

    public void RemoveBuilding(PlanetObject building)
    {
        RemoveObject(building, GridCell.Layer.Buildings);
    }

    public void AddUnit(Node3D unit, Vector3 location, int numCells = 3)
    {
        AddObject(unit, GridCell.Layer.Units, location, numCells);
    }

    public void RemoveUnit(Node3D unit)
    {
        RemoveObject(unit, GridCell.Layer.Units);
    }
    public void AddResource(Node3D resource, Vector3 location, int numCells = 3)
    {
        AddObject(resource, GridCell.Layer.Resources, location, numCells);
    }

    public void RemoveResource(Node3D resource)
    {
        RemoveObject(resource, GridCell.Layer.Resources);
    }

    public void AddObject(Node3D obj, GridCell.Layer layer,  Vector3 location, int numCells = 3)
    {
        foreach (int idx in PlanetGrid.GetAdjacentIndices(location.X, location.Y, numCells, true)) {
            PlanetGrid[idx].AddObject(layer, obj);
        }
    }

    public void RemoveObject(Node3D obj, GridCell.Layer layer)
    {
        for (int x = 0; x < PlanetGrid.Length(); x++) {
            PlanetGrid[x].RemoveObject(layer, obj);
        }
    }

    public int GetGridIndexAt(Vector3 globalPosition)
    {
        Vector3 local = ToLocal(globalPosition);
        return PlanetGrid.IndexOf(local.X, local.Y);
    }
}
