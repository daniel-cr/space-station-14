using Content.Server.GameObjects.Components.StationEvents;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems.StationEvents
{
    [UsedImplicitly]
    public sealed class RadiationPulseSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var comp in ComponentManager.EntityQuery<RadiationPulseComponent>())
            {
                comp.Update(frameTime);
                var ent = comp.Owner;

                if (ent.Deleted) continue;

                foreach (var entity in EntityManager.GetEntitiesInRange(ent.Transform.Coordinates, comp.Range, true))
                {
                    if (entity.Deleted) continue;
                    if (comp.InRangeUnOccluded(entity, range: comp.Range))
                    {
                        foreach (var radiation in entity.GetAllComponents<IRadiationAct>())
                        {
                            radiation.RadiationAct(frameTime, comp);
                        }
                    }
                }
            }
        }
    }
}
