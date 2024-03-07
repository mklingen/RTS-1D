using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace TaskLib
{
    public abstract class Task
    {
        public enum Status
        {
            Running,
            Success,
            Failure
        }

        public Unit Unit;
        public bool Ticked = false;
        public abstract void Start();
        public abstract void End();
        public abstract bool IsDone();
        public abstract Status UpdateImpl(double dt);
        public Status Update(double dt)
        {
            if (!Ticked) {
                Start();
            }
            Ticked = true;
            return UpdateImpl(dt);
        }

        public void Restart()
        {
            Ticked = false;
        }
    }

    public class GoToTask : Task
    {
        private Vector3 target;
        private float threshold;
        public GoToTask(Unit unit, Vector3 globalTarget, float threshold)
        {
            Unit = unit;
            target = globalTarget;
            this.threshold = threshold;
        }

        public override void Start()
        {
            Unit.GoTo(target);
        }

        public override bool IsDone()
        {
            bool isDone = Unit.IsNearGoToTarget(threshold);
            return isDone;
        }

        public override Status UpdateImpl(double dt)
        {
            // Unit is already going to target.
            if (IsDone()) {
                return Status.Success;
            }
            Unit.GoTo(target);
            return Status.Running;
        }

        public override void End()
        {
            Unit.Stop();
        }
    }

    public class SequenceTask : Task
    {
        List<Task> sequence;
        int idx = 0;

        public SequenceTask(IEnumerable<Task> otherTasks)
        {
            sequence = otherTasks.ToList();
        }

        public override void End()
        {
            if (idx >= 0 && idx < sequence.Count) {
                sequence[idx].End();
            }
        }

        public override bool IsDone()
        {
            if (sequence.Count == 0) {
                return true;
            }
            if (idx >= 0 && idx == sequence.Count - 1) {
                return sequence[idx].IsDone();
            }
            return idx >= sequence.Count;
        }

        public override void Start()
        {
            idx = 0;
        }

        public override Status UpdateImpl(double dt)
        {
            if (idx >= 0 && idx < sequence.Count) {
                var childStatus = sequence[idx].Update(dt);
                switch (childStatus) {
                    case Status.Running:
                        break;
                    case Status.Failure: {
                            sequence[idx].End();
                            return Status.Failure;
                    }
                    case Status.Success: {
                        if (sequence[idx].IsDone()) {
                            sequence[idx].End();
                            idx++;
                        }
                        break;
                    }
                }
            }
            if (idx >= sequence.Count) {
                return Status.Success;
            }
            return Status.Running;
        }
    }

    public class Domain : Task
    {
        private Task wrappedChild;
        private Func<bool> domain;
        public Domain(Task child, Func<bool> domainCheck)
        {
            wrappedChild = child;
            domain = domainCheck;
        }
        public override void End()
        {
            if (domain()) {
                wrappedChild.End();
            }
        }

        public override bool IsDone()
        {
            return (!domain()) || wrappedChild.IsDone();
        }

        public override void Start()
        {
            if (domain()) {
                wrappedChild.Start();
            }
        }

        public override Status UpdateImpl(double dt)
        {
            if (!domain()) {
                return Status.Failure;
            }
            return wrappedChild.Update(dt);
        }
    }


    public class Invert : Task
    {
        private Task wrappedChild;
        public Invert(Task child)
        {
            wrappedChild = child;
        }
        public override void End()
        {
            wrappedChild.End();
        }

        public override bool IsDone()
        {
            return wrappedChild.IsDone();
        }

        public override void Start()
        {
            wrappedChild.Start();
        }

        public override Status UpdateImpl(double dt)
        {
            Status childUpdate =  wrappedChild.Update(dt);
            switch (childUpdate) {
                case Status.Running:
                    return Status.Running;
                case Status.Success:
                    return Status.Failure;
                case Status.Failure:
                    return Status.Success;
            }
            return Status.Running;
        }
    }


    public class SelectTask : Task
    {
        List<Task> children;
        int idx = 0;

        public SelectTask(IEnumerable<Task> otherTasks)
        {
            children = otherTasks.ToList();
        }

        public override void End()
        {
            if (idx >= 0 && idx < children.Count) {
                children[idx].End();
            }
        }

        public override bool IsDone()
        {
            if (children.Count == 0) {
                return true;
            }
            if (idx >= 0 && idx == children.Count - 1) {
                return children[idx].IsDone();
            }
            return false;
        }

        public override void Start()
        {
            idx = 0;
            if (children.Count == 0) {
                return;
            }
            children[0].Start();
        }

        public override Status UpdateImpl(double dt)
        {
            if (idx >= 0 && idx < children.Count) {
                var childStatus = children[idx].Update(dt);
                switch (childStatus) {
                    case Status.Running:
                        break;
                    case Status.Failure:
                    case Status.Success: {
                            if (children[idx].IsDone()) {
                                children[idx].End();
                                idx++;
                            }
                            break;
                        }
                }
            }
            if (idx >= children.Count) {
                return Status.Success;
            }
            return Status.Running;
        }
    }

    public class HarvestTask : Task
    {
        private ResourceField Field;
        public HarvestTask(Unit unit, ResourceField field)
        {
            Unit = unit;
            Field = field;

        }

        public override void End()
        {
            foreach (var collector in Unit.GetCollectors()) {
                collector.Disable();
            }
        }

        public override bool IsDone()
        {
            return Unit.GetCollectors().Count == 0 || Unit.GetCollectors().All(collector => collector.IsFull()) || Field.GetResourcesRemaining() < 1e-4;
        }

        public override void Start()
        {
        }

        public override Status UpdateImpl(double dt)
        {
            foreach (var collector in Unit.GetCollectors()) {
                collector.UpdateCollection(Field, (float)dt);
            }
            if (IsDone()) {
                return Status.Success;
            }
            return Status.Running;
        }
    }

    public class ActionTask : Task
    {
        private Func<bool> action;
        private bool actionReturnCode = false;
        public ActionTask(Func<bool> action)
        {
            this.action = action;
        }

        public override void End()
        {

        }

        public override bool IsDone()
        {
            return true;
        }

        public override void Start()
        {
            actionReturnCode = this.action();
        }

        public override Status UpdateImpl(double dt)
        {
            if (actionReturnCode) {
                return Status.Success;
            }
            return Status.Failure;
        }
    }


    public class DropOffTask : SequenceTask
    {
        public DropOffTask(Unit unit, Collector collector, ResourceDropOff dropOff) :
            base(new List<Task> { 
                new GoToTask(unit, dropOff.GlobalPosition, 0.01f) 
              , new ActionTask(() => { collector.DropOff(dropOff); return true; })})

        {

            Unit = unit;
        }

        public override bool IsDone()
        {
            return Unit.GetCollectors().Count == 0 || Unit.GetCollectors().All(collector => collector.IsEmpty());
        }
    }


    public class BuildTask : SequenceTask
    {

        public class WaitWhileBuildingTask : Task 
        {

            private Unit unit;
            private Builder builder;
            public WaitWhileBuildingTask(Unit unit, Builder builder)
            {
                this.unit = unit;
                this.builder = builder;
            }

            public override void End()
            {
               
            }

            public override bool IsDone()
            {
                return this.builder.GetProgress() >= 1.0f || !this.builder.IsBuilding();
            }

            public override void Start()
            {
             
            }

            public override Status UpdateImpl(double dt)
            {
                if (IsDone()) {
                    return Status.Success;
                }
                return Status.Running;
            }
        }

        public BuildTask(Unit unit, Builder builder, ConstructionPile blueprint) :
            base (new List<Task>
            {
                new GoToTask(unit, blueprint.GlobalPosition, 0.01f),
                new WaitWhileBuildingTask(unit, builder)
            })
        {

        }
    }


}