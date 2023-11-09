using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace SpeedRelativeThrust
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false)]
    public class SpeedRelativeThrustEntityComponent : MyGameLogicComponent
    {
        private SpeedRelativeThrustConfiguration config;
        private IMyThrust thruster;
        private Vector3 thrusterDirection;
        private IMyCubeGrid cubeGrid;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer && MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                NeedsUpdate = MyEntityUpdateEnum.NONE;
                return;
            }

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            thruster = (IMyThrust)Entity;
            thrusterDirection = Base6Directions.GetVector(thruster.Orientation.Forward);
            cubeGrid = thruster.CubeGrid;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (!Util.IsValid(thruster) || !Util.IsValid(cubeGrid) || cubeGrid.Physics == null)
            {
                NeedsUpdate = MyEntityUpdateEnum.NONE;
                return;
            }

            config = SpeedRelativeThrustSessionComponent.Instance?.Config;
            if (config == null)
            {
                NeedsUpdate = MyEntityUpdateEnum.NONE;
                return;
            }

            cubeGrid.OnIsStaticChanged += CubeGrid_OnIsStaticChanged;
            NeedsUpdate = cubeGrid.IsStatic ? MyEntityUpdateEnum.NONE : MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        private void CubeGrid_OnIsStaticChanged(IMyCubeGrid grid, bool isStatic)
        {
            NeedsUpdate = isStatic ? MyEntityUpdateEnum.NONE : MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        public override void UpdateBeforeSimulation10()
        {
            if (!thruster.IsFunctional)
            {
                return;
            }

            if (!Util.IsValid(cubeGrid) || cubeGrid.Physics == null)
            {
                return;
            }

            var speed = cubeGrid.Physics.Speed;
            float speedPercent;
            float scalar = 1f;

            if (cubeGrid.GridSizeEnum == MyCubeSize.Large)
            {
                speedPercent = speed / MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed;

                if (speedPercent >= config.LargeFalloffStartPercent)
                {
                    scalar -= (float)Math.Pow(
                        (speedPercent - config.LargeFalloffStartPercent) / (1f - config.LargeFalloffStartPercent),
                        config.LargeFalloffApplicationPowerScalar);
                    if (scalar < 0 || MathHelper.IsZero(scalar))
                    {
                        scalar = 0;
                    }
                    // Clamp the thrust multiplier between 0.01 (the minimum allowed) and 1 (100%).
                    thruster.ThrustMultiplier = MathHelper.Clamp(MathHelper.Lerp(config.LargeThrustMin, 1, scalar), 0.01f, 1f);
                }
                else
                {
                    // Nothing to do, set it back to 100%
                    thruster.ThrustMultiplier = 1;
                }
            }
            else
            {
                speedPercent = speed / MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed;
                if (speedPercent >= config.SmallFalloffStartPercent)
                {
                    scalar -= (float)Math.Pow(
                        (speedPercent - config.SmallFalloffStartPercent) / (1f - config.SmallFalloffStartPercent),
                        config.SmallFalloffApplicationPowerScalar);
                    if (scalar < 0 || MathHelper.IsZero(scalar))
                    {
                        scalar = 0;
                    }
                    // Clamp the thrust multiplier between 0.01 (the minimum allowed) and 1 (100%).
                    thruster.ThrustMultiplier = MathHelper.Clamp(MathHelper.Lerp(config.SmallThrustMin, 1, scalar), 0.01f, 1f);
                }
                else
                {
                    // Nothing to do, set it back to 100%
                    thruster.ThrustMultiplier = 1;
                }
            }
            return;

            // No need to keep going, thruster is not limited
            if (thruster.ThrustMultiplier == 1f)
            {
                return;
            }

            // Additional code to scale the remaining scalar relative to how close to the axis of travel it is.
            // Left for demo purposes, probably not needed but it's cool.
            var velocityNormal = cubeGrid.Physics.LinearVelocity.Normalized();
            var gridOrientation = cubeGrid.PositionComp.GetOrientation();
            var worldThrustDirection = Vector3.TransformNormal(thrusterDirection, gridOrientation);
            var thrusterVelocityDot = worldThrustDirection.Dot(velocityNormal);

            // If you need the value in degrees of a vector, get the arccosine
            // of the dot product (which is the angle in radians) and convert it to degrees

            // MathHelper.ToDegrees((float)Math.Acos(thrusterVelocityDot));

            // negative dot = opposite the direction of travel so positive means the axis of thrust points along the current vector.
            // That means that this thruster is a stopping thruster, no need to scale it down, it can only slow the grid down.
            // Also handle NaN values for perfectly perpendicular thrusters
            if (thrusterVelocityDot >= 0 || !MathHelper.IsValid(thrusterVelocityDot))
            {
                return;
            }
            // dot will be 0 at perpendicular and 1 at parallel, so reduce the scalar further toward 0 based on that to give side thrusters a little more authority even as speed increases
            thruster.ThrustMultiplier = MathHelper.Clamp(
                MathHelper.Lerp(thruster.ThrustMultiplier, thruster.ThrustMultiplier / 2, Math.Abs(thrusterVelocityDot)),
                0.01f, 1f);
        }
    }
}