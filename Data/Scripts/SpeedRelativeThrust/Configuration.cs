using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace SpeedRelativeThrust
{
    public class SpeedRelativeThrustConfiguration
    {
        private const string ConfigFileName = "SpeedRelativeThrustConfig.xml";

        private const float DefaultLargeFalloffStartScalar = 0.6f;
        private const float DefaultLargeThrustMin = 0.0625f;
        private const float DefaultSmallFalloffStartScalar = 0.7f;
        private const float DefaultSmallThrustMin = 0.125f;

        // The percentage of the world max speed to start reducing thrust at for large grids (e.g. 60% or 60ms for vanilla)
        public float LargeFalloffStartPercent { get; set; }
        // The minimum percentage of thrust for large grids.
        public float LargeThrustMin { get; set; }
        // The percentage of the world max speed to start reducing thrust at for Small grids (e.g. 70% or 70ms for vanilla)
        public float SmallFalloffStartPercent { get; set; }
        // The minimum percentage of thrust for small grids.
        public float SmallThrustMin { get; set; }
        // Reduce the power of only thrusters pointing in the direction of movement (false), or all thrusters (true).
        public bool ReduceAllThrusters { get; set; }

        public static SpeedRelativeThrustConfiguration LoadSettings()
        {
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(ConfigFileName, typeof(SpeedRelativeThrustConfiguration)))
            {
                try
                {
                    SpeedRelativeThrustConfiguration loadedSettings;
                    using (var reader =
                           MyAPIGateway.Utilities.ReadFileInWorldStorage(ConfigFileName, typeof(SpeedRelativeThrustConfiguration)))
                    {
                        loadedSettings = MyAPIGateway.Utilities.SerializeFromXML<SpeedRelativeThrustConfiguration>(reader.ReadToEnd());
                    }

                    if (loadedSettings == null || !loadedSettings.Validate())
                    {
                        throw new Exception("SpeedRelativeThrust: Invalid mod configuration");
                    }

                    SaveSettings(loadedSettings);
                    return loadedSettings;
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLineAndConsole($"SpeedRelativeThrust: Failed to load mod settings: {e.Message}\n{e.StackTrace}");
                }

                MyAPIGateway.Utilities.WriteBinaryFileInWorldStorage(ConfigFileName + ".old", typeof(SpeedRelativeThrustConfiguration));
            }

            var settings = new SpeedRelativeThrustConfiguration();
            settings.SetDefaults();
            SaveSettings(settings);
            return settings;
        }

        private static void SaveSettings(SpeedRelativeThrustConfiguration settings)
        {
            try
            {
                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(ConfigFileName, typeof(SpeedRelativeThrustConfiguration)))
                {
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(settings));
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"SpeedRelativeThrust: Failed to save mod settings: {e.Message}\n{e.StackTrace}");
            }
        }
        private bool Validate()
        {
            return LargeFalloffStartPercent > 0 && LargeFalloffStartPercent < 1f &&
                SmallFalloffStartPercent > 0 && SmallFalloffStartPercent < 1f &&
                LargeThrustMin > 0 && LargeThrustMin < 1f &&
                SmallThrustMin > 0 && SmallThrustMin < 1f;
        }

        private void SetDefaults()
        {
            LargeFalloffStartPercent = DefaultLargeFalloffStartScalar;
            SmallFalloffStartPercent = DefaultSmallFalloffStartScalar;
            LargeThrustMin = DefaultLargeThrustMin;
            SmallThrustMin = DefaultSmallThrustMin;
            ReduceAllThrusters = false;
        }
    }
}