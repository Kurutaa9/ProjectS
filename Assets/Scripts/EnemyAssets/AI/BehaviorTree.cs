using System;
using System.Collections.Generic;

namespace BT
{
    public enum NodeState { Success, Failure, Running }

    public abstract class BTNode
    {
        public NodeState State { get; protected set; } = NodeState.Failure;
        public abstract NodeState Evaluate();
    }

    public abstract class Composite : BTNode
    {
        protected readonly List<BTNode> children = new List<BTNode>();
        public Composite(params BTNode[] kids)
        {
            if (kids != null) children.AddRange(kids);
        }
        public void AddChild(BTNode child) => children.Add(child);
    }

    public class Sequence : Composite
    {
        public Sequence(params BTNode[] kids) : base(kids) { }
        public override NodeState Evaluate()
        {
            bool anyRunning = false;
            foreach (var child in children)
            {
                var result = child.Evaluate();
                switch (result)
                {
                    case NodeState.Failure:
                        State = NodeState.Failure; return State;
                    case NodeState.Running:
                        anyRunning = true; break;
                    case NodeState.Success:
                        break;
                }
            }
            State = anyRunning ? NodeState.Running : NodeState.Success;
            return State;
        }
    }

    public class Selector : Composite
    {
        public Selector(params BTNode[] kids) : base(kids) { }
        public override NodeState Evaluate()
        {
            foreach (var child in children)
            {
                var result = child.Evaluate();
                switch (result)
                {
                    case NodeState.Success:
                        State = NodeState.Success; return State;
                    case NodeState.Running:
                        State = NodeState.Running; return State;
                    case NodeState.Failure:
                        continue;
                }
            }
            State = NodeState.Failure; return State;
        }
    }

    public class ConditionNode : BTNode
    {
        private readonly Func<bool> predicate;
        public ConditionNode(Func<bool> predicate) { this.predicate = predicate; }
        public override NodeState Evaluate()
        {
            State = (predicate != null && predicate()) ? NodeState.Success : NodeState.Failure;
            return State;
        }
    }

    public class ActionNode : BTNode
    {
        private readonly Func<NodeState> action;
        public ActionNode(Func<NodeState> action) { this.action = action; }
        public override NodeState Evaluate()
        {
            State = action != null ? action() : NodeState.Failure;
            return State;
        }
    }
}
