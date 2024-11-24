using UnityEngine;
using RTS.Units;

namespace RTS.Commands
{
    public abstract class UnitCommand
    {
        protected Unit unit;
        protected Vector3 targetPosition;
        protected Unit targetUnit;

        public UnitCommand(Unit unit)
        {
            this.unit = unit;
        }

        public abstract void Execute();
        public abstract void Cancel();
        public abstract bool IsComplete();
    }

    public class MoveCommand : UnitCommand
    {
        public MoveCommand(Unit unit, Vector3 targetPosition) : base(unit)
        {
            this.targetPosition = targetPosition;
        }

        public override void Execute()
        {
            unit.MoveTo(targetPosition);
        }

        public override void Cancel()
        {
            unit.Stop();
        }

        public override bool IsComplete()
        {
            return Vector3.Distance(unit.transform.position, targetPosition) < 0.1f;
        }
    }

    public class AttackCommand : UnitCommand
    {
        public AttackCommand(Unit unit, Unit target) : base(unit)
        {
            this.targetUnit = target;
        }

        public override void Execute()
        {
            if (targetUnit != null)
            {
                unit.Attack(targetUnit);
            }
        }

        public override void Cancel()
        {
            unit.Stop();
        }

        public override bool IsComplete()
        {
            return targetUnit == null || !targetUnit.gameObject.activeInHierarchy;
        }
    }
}
