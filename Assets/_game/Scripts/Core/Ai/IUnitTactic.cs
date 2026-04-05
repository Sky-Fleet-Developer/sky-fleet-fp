using System;
using System.Collections.Generic;
using Core.Misc;
using Core.World;
using Zenject;
using ITickable = Core.Misc.ITickable;

namespace Core.Ai
{
    public interface IUnitTactic
    {
        public void UnitEnterTactic(UnitEntity entity);

        public void UnitExitTactic(UnitEntity entity);
    }

    public abstract class MultipleUnitTacticBase : IUnitTactic, ITickable, IDisposable
    {
        private TickService _tickService;
        public virtual int TickRate { get; } = 1;
        protected List<UnitEntity> ControlledEntities = new();
        
        public IReadOnlyList<UnitEntity> ControlledEntitiesList => ControlledEntities;

        public MultipleUnitTacticBase(TickService tickService)
        {
            _tickService = tickService;
            _tickService.Add(this);
        }
        
        public void Dispose()
        {
            _tickService?.Remove(this);
        }
        
        ~MultipleUnitTacticBase() => Dispose();

        public virtual void UnitEnterTactic(UnitEntity entity)
        {
            ControlledEntities.Add(entity);
        }

        public virtual void UnitExitTactic(UnitEntity entity)
        {
            ControlledEntities.Remove(entity);
        }

        public abstract void Tick();
    }
    
    public abstract class SingleUnitTacticBase : IUnitTactic, ITickable, IDisposable
    {
        private TickService _tickService;
        public virtual int TickRate { get; } = 1;
        protected UnitEntity ControlledEntity;

        public SingleUnitTacticBase(TickService tickService)
        {
            _tickService = tickService;
            _tickService.Add(this);
        }
        
        public void Dispose()
        {
            _tickService?.Remove(this);
        }
        
        ~SingleUnitTacticBase() => Dispose();
        
        public virtual void UnitEnterTactic(UnitEntity entity)
        {
            ControlledEntity = entity;
        }

        public virtual void UnitExitTactic(UnitEntity entity)
        {
            ControlledEntity = null;
        }

        public abstract void Tick();
    }

    public class EmptyTactic : IUnitTactic
    {
        public void UnitEnterTactic(UnitEntity entity) { }

        public void UnitExitTactic(UnitEntity entity) { }
    }
}