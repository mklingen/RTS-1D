using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
    public class Grid<T>
    {
        // Flat array of cells.
        protected T[] Cells;

        public Grid(int numCells)
        {
            Cells = new T[numCells];
        }

        public void Clear(T value)
        {
            for (int k = 0; k < Cells.Length; k++) {
                Cells[k] = value;
            }
        }

        // Gets the cell at the given index.
        public T Get(int idx)
        {
            return Cells[idx % Cells.Length];
        }

        // Sets the cell at the given index.
        public void Set(int idx, T cell)
        {
            Cells[idx % Cells.Length] = cell;
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
    public class AngleGrid<T> : Grid<T>
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
            // Rewrap from 0 to 2PI, then divide.
            return (int)((wrapped + Mathf.Pi) / AngularResolution);

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
                    yield return idx + k;
                }
            }
        }

        public int Length()
        {
            return this.Cells.Length;
        }
    }


    [ExportGroup("Grids")]
    [Export] private float buildingPlacementResolutionDegrees = 5;
    public AngleGrid<OccupancyCell> BuildingOccupancyGrid;



    public struct OccupancyCell
    {
        public PlanetObject ObjectOccupyingCell = null;

        public bool IsOccupied { get { return ObjectOccupyingCell != null;  } }

        public OccupancyCell()
        {
        }
    }

    public override void _Ready()
    {
        base._Ready();
        BuildingOccupancyGrid = new AngleGrid<OccupancyCell>(Mathf.DegToRad(buildingPlacementResolutionDegrees));
    }

    public bool CanBuildBuilding(Vector3 location)
    {
        return !BuildingOccupancyGrid.Get(location.X, location.Y).IsOccupied;
    }

    public void AddBuilding(PlanetObject building, Vector3 location, int numCells = 3)
    {
        foreach (int idx in BuildingOccupancyGrid.GetAdjacentIndices(location.X, location.Y, numCells, true)) {
            BuildingOccupancyGrid[idx] = new OccupancyCell { ObjectOccupyingCell = building };
        }
    }

    public void RemoveBuilding( Vector3 location, int numCells = 3)
    {
        foreach (int idx in BuildingOccupancyGrid.GetAdjacentIndices(location.X, location.Y, numCells, true)) {
            BuildingOccupancyGrid[idx] = new OccupancyCell { ObjectOccupyingCell = null };
        }
    }

    public void RemoveBuilding(PlanetObject building)
    {
        for (int x = 0; x < BuildingOccupancyGrid.Length(); x++) {
            if (BuildingOccupancyGrid[x].ObjectOccupyingCell == building) {
                BuildingOccupancyGrid[x] = new OccupancyCell { ObjectOccupyingCell = null };
            }
        }
    }
}
