using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using TaskLib;
using static Game;
using TeamActionLib;

namespace TeamActionLib
{
    /// <summary>
    /// Generic abstract action that an AI can perform.
    /// </summary>
    public abstract class TeamAction
    {
        /// <summary>
        /// Generalized utility of the action.
        /// </summary>
        /// <returns>A floating point value representing the utility of the action.</returns>
        public abstract float GetUtility();
        /// <summary>
        /// Whether it is legal to perform this action.
        /// </summary>
        /// <returns>true if the action can be performed, false otherwise</returns>
        public abstract bool CanPerform();
        /// <summary>
        /// Creates a behavior tree task to start the action.
        /// </summary>
        /// <returns>A task (behavior tree) that begins ticking the action.</returns>
        public abstract TaskLib.Task Start();
    }

    /// <summary>
    /// Action which causes the AI to build a structure.
    /// </summary>
    public class BuildStructureAction : TeamAction
    {
        /// <summary>
        /// The building to add.
        /// </summary>
        protected UnitStats desiredBuilding;
        /// <summary>
        /// Team associated with the building.
        /// </summary>
        protected Game.Team team;
        /// <summary>
        /// Base utility for building this building. This will be divided by the number of buildings
        /// of this kind that we already have.
        /// </summary>
        protected float baseUtility;


        /// <summary>
        /// Returns the best place to build this building.
        /// </summary>
        /// <returns>A good place to build the building if it exists, or null otherwise.</returns>
        public virtual Vector3? GetGoodPlaceToBuild(Planet planet)
        {
            // Get the "center" of the team's buildilngs.
            Vector3 meanPos = team.GetBuildingAverageMeanPos();
            // Find a location near the "center" (TODO: this might be different depending on the building).
            Vector3? bestBuildPos = team.GetNearestFreeBuildLocation(planet, meanPos);
            return bestBuildPos;
        }

        public class BuildStructureTask : TaskLib.Task
        {
            UnitStats building;
            Game.Team team;
            Planet planet;
            BuildStructureAction action;
            public BuildStructureTask(BuildStructureAction action, Planet planet, Game.Team team, UnitStats building)
            {
                this.action = action;
                this.team = team;
                this.building = building;
                this.planet = planet;
            }
            public override void End()
            {

            }

            public override bool IsDone()
            {
                // The build happens instantaneously (because the construction pile creates separate build tasks for units!)
                return true;
            }

            public override void Start()
            {
                Vector3? bestBuildPos = action.GetGoodPlaceToBuild(planet);
                // If we found a build position, create a construction pile there. Units will automatically go
                // and build at the pile.
                if (bestBuildPos != null) {
                    ConstructionPile pile = building.CreateConstructionPile(Game.Get(), team.Index);
                    pile.GlobalPosition = bestBuildPos.Value;
                    pile.ForceSetPosition(pile.GlobalPosition);
                    pile.Depth = building.DefaultDepth;
                    pile.Height = building.DefaultHeight;
                    Game.Get().GetPlanet().AddBuilding(pile, pile.GlobalPosition);
                }
            }

            public override Status UpdateImpl(double dt)
            {
                return Status.Success;
            }
        }

        public BuildStructureAction(float baseUtility, Game.Team team, UnitStats building)
        {
            this.baseUtility = baseUtility;
            desiredBuilding = building;
            this.team = team;
        }

        public override bool CanPerform()
        {
            return (team.Resources >= desiredBuilding.BuildCost &&
                team.Units.Any(unit => unit.Stats.CanDoAbilities.HasFlag(UnitStats.Abilities.BuildStructures) &&
                !team.ConstructionPiles.Any(pile => pile.Unit == desiredBuilding)
                ));
        }

        public override float GetUtility()
        {
            return baseUtility / (team.Units.Count(unit => unit.Stats == desiredBuilding) + 1);
        }

        public override Task Start()
        {
            return new BuildStructureTask(this, Game.Get().GetPlanet(), team, desiredBuilding);
        }
    }

    /// <summary>
    /// Specific refinery building action ; constructs a refinery near resource piles.
    /// </summary>
    public class BuildRefineryAction : BuildStructureAction
    {
        public BuildRefineryAction(float baseUtility, Team team) : base(baseUtility, team, GD.Load<UnitStats>("res://Units/refinery.tres"))
        {
        }

        public override float GetUtility()
        {
            // TODO maybe something fancier for refineries?
            return base.GetUtility();
        }

        public override Vector3? GetGoodPlaceToBuild(Planet planet)
        {
            Vector3 meanPos = team.GetBuildingAverageMeanPos();
            ResourceField closestField = null;
            float bestScore = float.MinValue;
            const float distanceWeight = 100;
            foreach (ResourceField field in Game.FindChildrenRecursive<ResourceField>(Game.Get())) {
                float score = 1.0f / ((field.GlobalPosition - meanPos).LengthSquared() * distanceWeight);
                score /= field.GetResourcesRemaining();
                if (score > bestScore) {
                    bestScore = score;
                    closestField = field;
                }
            }
            if (closestField == null) {
                return null;
            }
            return team.GetNearestFreeBuildLocation(planet, closestField.GlobalPosition);
        }
    }

    public partial class AIManager : Node
    {
        private Game game;


        public class TeamState
        {
            public Game.Team Team;

            // TODO find the potential actions:
            // (1) if no command center, build one.
            // (2) if no engineer, build one.
            // (3) if no refinery, build one.
            // (4) if no factory, build one.
            // (5) send a unit to a random unexplored area.
            //     a. Implement unexplored areas.
            // (6) attack enemy positions.
            public IEnumerable<TeamActionLib.TeamAction> GetAvailableActions()
            {
                yield return new BuildRefineryAction(1.0f, Team);
                yield return new BuildStructureAction(1.0f, Team, GD.Load<UnitStats>("res://Units/factory.tres"));
            }

            public TeamActionLib.TeamAction CurrentAction;
            public TaskLib.Task CurrentTask;
        }

        public Dictionary<int, TeamState> TeamStates = new Dictionary<int, TeamState>();

        public override void _Ready()
        {
            base._Ready();
            if (game == null) {
                game = Game.Get();
            }
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            foreach (var team in game.GetTeams()) {
                if (team.IsPlayer) {
                    continue;
                }
                TickTeam(delta, team);
            }
        }

        public void TickTeam(double dt, Game.Team team)
        {
            // Add the team state if it does not already exist.
            if (!TeamStates.ContainsKey(team.Index)) {
                TeamStates[team.Index] = new TeamState()
                { Team = team };
            }

            // Get the team state and check the current task.
            TeamState state = TeamStates[team.Index];
            if (state.CurrentTask == null) {
                // Get the best task to do.
                state.CurrentAction = state.GetAvailableActions()
                    .Where(action => action.CanPerform())
                    .OrderBy(action => action.GetUtility()).FirstOrDefault();
                if (state.CurrentAction != null) {
                    state.CurrentTask = state.CurrentAction.Start();
                }
            }
            else {
                // Update whatever the current task is.
                state.CurrentTask.Update(dt);
                if (state.CurrentTask.IsDone()) {
                    state.CurrentTask = null;
                }
            }
        }
    }
}