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
            if (!thruster.IsWorking)
            {
                return;
            }

            if (!Util.IsValid(cubeGrid) || cubeGrid.Physics == null)
            {
                return;
            }

            float reduction;
            if (!config.ReduceAllThrusters)
            {
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
                    reduction = 0;
                }
                else
                {
                    // dot will be 0 at perpendicular and -1 at parallel, so take the absolute value and reduce the scalar
                    // if it's off axis with travel to give side thrust authority back while preventing more forward thrust
                    reduction = GetThrustReduction() * Math.Abs(thrusterVelocityDot);
                }
            }
            else
            {
                reduction = GetThrustReduction();
            }
            thruster.ThrustMultiplier = reduction > 0 ? MathHelper.Clamp(1f - reduction, 0.01f, 1f) : 1f;
        }

        private float GetThrustReduction()
        {
            var speed = cubeGrid.Physics.Speed;
            float speedPercent;
            float scalar;
            float reduction = 0f;
            if (cubeGrid.GridSizeEnum == MyCubeSize.Large)
            {
                speedPercent = speed / MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed;

                if (speedPercent >= config.LargeFalloffStartPercent)
                {
                    var speedFactor = 1 - (speedPercent - config.LargeFalloffStartPercent) / (1 - config.LargeFalloffStartPercent);
                    scalar = (float)(Math.Pow(0.5f, Math.Pow(speedFactor, -1f)) / 0.5f);
                    if (scalar < 0 || MathHelper.IsZero(scalar))
                    {
                        scalar = 0;
                    }
                    // Clamp the thrust multiplier between 0.01 (the minimum allowed) and 1 (100%).
                    reduction = MathHelper.Lerp(0, 1 - config.LargeThrustMin, 1 - scalar);
                }
            }
            else
            {
                speedPercent = speed / MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed;

                if (speedPercent >= config.SmallFalloffStartPercent)
                {
                    var speedFactor = 1 - (speedPercent - config.SmallFalloffStartPercent) / (1 - config.SmallFalloffStartPercent);
                    scalar = (float)(Math.Pow(0.5f, Math.Pow(speedFactor, -1f)) / 0.5f);
                    if (scalar < 0 || MathHelper.IsZero(scalar))
                    {
                        scalar = 0;
                    }
                    // Clamp the thrust multiplier between 0.01 (the minimum allowed) and 1 (100%).
                    reduction = MathHelper.Lerp(0, 1 - config.SmallThrustMin, 1 - scalar);
                }
            }

            return reduction;
        }
    }
}