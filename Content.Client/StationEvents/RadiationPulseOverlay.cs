#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.Components.StationEvents;
using Content.Shared.GameObjects.Components.Mobs;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.StationEvents
{
    [UsedImplicitly]
    public sealed class RadiationPulseOverlay : Overlay
    {
        [Dependency] private readonly IComponentManager _componentManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private readonly Dictionary<IEntity, TimeSpan> _radiationPulsesRunning = new();
        private readonly ShaderInstance _shader;
        private readonly int _lightLevels = 10;
        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        public RadiationPulseOverlay() : base(nameof(SharedOverlayID.RadiationPulseOverlay))
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("RadPulse").Instance().Duplicate();
        }

        private float GetIntensity(IEntity entity, TimeSpan endTime)
        {
            if (!_radiationPulsesRunning.ContainsKey(entity))
            {
                _radiationPulsesRunning.Add(entity, _gameTiming.CurTime);
            }
            var ratio = (float) ((_gameTiming.CurTime - _radiationPulsesRunning[entity]) / ((endTime - _radiationPulsesRunning[entity]) / 2.0f));
            if (ratio >= 1.0f)
            {
                ratio = 2.0f - ratio;
                ratio = ratio >= 0.0f ? ratio : -ratio;
            }
            return ratio;
        }

        protected override void Draw(DrawingHandleBase handle, OverlaySpace currentSpace)
        {
            // PVS should control the overlay pretty well so the overlay doesn't get instantiated unless we're near one...
            var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;
            if (playerEntity == null)
            {
                return;
            }

            var radiationPulses = _componentManager.EntityQuery<RadiationPulseComponent>().ToList();
            var worldHandle = (DrawingHandleWorld) handle;
            var viewport = _eyeManager.GetWorldViewport();

            foreach (var grid in _mapManager.FindGridsIntersecting(playerEntity.Transform.MapID, viewport))
            {
                foreach (var pulse in radiationPulses)
                {
                    if (!pulse.Draw || grid.Index != pulse.Owner.Transform.GridID) continue;

                    // TODO: Check if viewport intersects circle
                    if (pulse.Owner.TryGetComponent<PointLightComponent>(out var light))
                    {
                        var color = GetIntensity(pulse.Owner, pulse.EndTime) * _lightLevels;
                        //light.Color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                        light.Color = new Color(0.0f, color, 0.0f, 1.0f);
                        worldHandle.UseShader(_shader);
                    }
                    worldHandle.DrawTextureRect(Texture.Transparent, Box2.CenteredAround(pulse.Owner.Transform.WorldPosition, new Vector2(pulse.Range, pulse.Range)));
                    //var circlePosition = _eyeManager.WorldToScreen(pulse.Owner.Transform.WorldPosition);
                    //var z = pulse.Range * 64;
                    //worldHandle.DrawTextureRect(Texture.Transparent, UIBox2.FromDimensions(circlePosition.X - z, circlePosition.Y - z, z * 2, z * 2));
                }
            }
        }
    }
}
