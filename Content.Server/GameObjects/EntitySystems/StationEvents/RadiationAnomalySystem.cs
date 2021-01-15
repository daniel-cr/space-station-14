using Content.Server.GameObjects.Components.StationEvents;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;

namespace Content.Server.GameObjects.EntitySystems.StationEvents
{
    [UsedImplicitly]
    public sealed class RadiationAnomalySystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private readonly int _lightLevels = 10;
        private readonly Dictionary<(IEntity, IEntity), TimeSpan> _alreadyRadiated = new();
        //private readonly Dictionary<IEntity, TimeSpan> _alreadyRadiated = new();
        private readonly TimeSpan _radiationCooldown = TimeSpan.FromSeconds(2.0f);

        private readonly Dictionary<IEntity, Dictionary<IEntity, TimeSpan>> _alr = new();

        public override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);
        }

        private float GetIntensity(TimeSpan startTime, TimeSpan endTime)
        {
            var ratio = (float) ((_gameTiming.CurTime - startTime) / ((endTime - startTime) / 2.0f));
            if (ratio >= 1.0f)
            {
                ratio = 2.0f - ratio;
                ratio = ratio >= 0.0f ? ratio : -ratio;
            }
            return ratio;
        }

        private bool CanIrradiate(IEntity origin, IEntity destination)
        {
            if (_alr.ContainsKey(origin))
            {
                if (_alr[origin].ContainsKey(destination))
                {
                    if (_gameTiming.CurTime > _alr[origin][destination])
                    {
                        _alr[origin][destination] = _gameTiming.CurTime + _radiationCooldown;
                        return true;
                    }
                    return false;
                }
                else
                {
                    return _alr[origin].TryAdd(destination, _gameTiming.CurTime + _radiationCooldown);
                }
            } else
            {
                return _alr.TryAdd(origin, new Dictionary<IEntity, TimeSpan>() { { destination, _gameTiming.CurTime + _radiationCooldown } });
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var component in ComponentManager.EntityQuery<RadiationPulseComponent>())
            {
                component.Update(frameTime);
                var radiationAnomalyEntity = component.Owner;

                if (radiationAnomalyEntity.Deleted)
                {
                    _alr.Remove(radiationAnomalyEntity);
                    continue;
                }

                if (radiationAnomalyEntity.TryGetComponent<PointLightComponent>(out var light))
                {
                    light.Color = new Color(0.0f, GetIntensity(component.StartTime, component.EndTime) * _lightLevels, 0.0f, 1.0f);
                }

                foreach (var affectedEntity in EntityManager.GetEntitiesInRange(radiationAnomalyEntity.Transform.Coordinates, component.Range, true))
                {
                    if (affectedEntity.Deleted)
                    {
                        continue;
                    }

                    if (CanIrradiate(radiationAnomalyEntity, affectedEntity))
                    {
                        if (radiationAnomalyEntity.InRangeUnOccluded(affectedEntity, range: component.Range))
                        {
                            foreach (var radiation in affectedEntity.GetAllComponents<IRadiationAct>())
                            {
                                radiation.RadiationAct(frameTime, component);
                            }
                        }
                    }
                }
            }
        }
    }
}
