using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace SpeedRelativeThrust
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class SpeedRelativeThrustSessionComponent : MySessionComponentBase
    {
        public static SpeedRelativeThrustSessionComponent Instance { get; private set; }
        public SpeedRelativeThrustConfiguration Config { get; private set; }

        public override void LoadData()
        {
            if (MyAPIGateway.Session.IsServer || !MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                Config = SpeedRelativeThrustConfiguration.LoadSettings();
            }

            Instance = this;
        }

        protected override void UnloadData()
        {
            Instance = null;
        }
    }
}