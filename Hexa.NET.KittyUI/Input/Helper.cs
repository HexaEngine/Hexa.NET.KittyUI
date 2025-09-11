namespace Hexa.NET.KittyUI.Input
{
    using Hexa.NET.SDL3;

    /// <summary>
    /// A utility class that provides conversion methods for translating between different input and SDL types.
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// Converts an SDL key code to its corresponding Key enum value.
        /// </summary>
        /// <param name="code">The SDL key code to convert.</param>
        /// <returns>The corresponding Key enum value.</returns>
        public static Key Convert(int code)
        {
            return (Key)code;
        }

        /// <summary>
        /// Converts a Key enum value to its corresponding SDL key code.
        /// </summary>
        /// <param name="code">The Key enum value to convert.</param>
        /// <returns>The corresponding SDL key code.</returns>
        public static int ConvertBack(Key code)
        {
            return (int)code;
        }

        /// <summary>
        /// Converts a GamepadAxis enum value to its corresponding SDL GameControllerAxis.
        /// </summary>
        /// <param name="gamepadAxis">The GamepadAxis enum value to convert.</param>
        /// <returns>The corresponding SDL GameControllerAxis.</returns>
        public static SDL3.SDLGamepadAxis ConvertBack(GamepadAxis gamepadAxis)
        {
            return (SDL3.SDLGamepadAxis)gamepadAxis;
        }

        /// <summary>
        /// Converts an SDL GameControllerAxis to its corresponding GamepadAxis enum value.
        /// </summary>
        /// <param name="axis">The SDL GameControllerAxis to convert.</param>
        /// <returns>The corresponding GamepadAxis enum value.</returns>
        public static GamepadAxis Convert(SDL3.SDLGamepadAxis axis)
        {
            return (GamepadAxis)axis;
        }

        /// <summary>
        /// Converts a GamepadButton enum value to its corresponding SDL GameControllerButton.
        /// </summary>
        /// <param name="gamepadButton">The GamepadButton enum value to convert.</param>
        /// <returns>The corresponding SDL GameControllerButton.</returns>
        public static SDL3.SDLGamepadButton ConvertBack(GamepadButton gamepadButton)
        {
            return (SDL3.SDLGamepadButton)gamepadButton;
        }

        /// <summary>
        /// Converts an SDL GameControllerButton to its corresponding GamepadButton enum value.
        /// </summary>
        /// <param name="button">The SDL GameControllerButton to convert.</param>
        /// <returns>The corresponding GamepadButton enum value.</returns>
        public static GamepadButton Convert(SDL3.SDLGamepadButton button)
        {
            return (GamepadButton)button;
        }

        /// <summary>
        /// Converts an SDL GameControllerType to its corresponding GamepadType enum value.
        /// </summary>
        /// <param name="gameControllerType">The SDL GameControllerType to convert.</param>
        /// <returns>The corresponding GamepadType enum value.</returns>
        public static GamepadType Convert(SDL3.SDLGamepadType gameControllerType)
        {
            return (GamepadType)gameControllerType;
        }

        /// <summary>
        /// Converts a GamepadSensorType enum value to its corresponding SDL SensorType.
        /// </summary>
        /// <param name="gamepadSensorType">The GamepadSensorType enum value to convert.</param>
        /// <returns>The corresponding SDL SensorType.</returns>
        public static SDL3.SDLSensorType ConvertBack(GamepadSensorType gamepadSensorType)
        {
            return (SDL3.SDLSensorType)gamepadSensorType;
        }

        /// <summary>
        /// Converts an SDL SensorType to its corresponding GamepadSensorType enum value.
        /// </summary>
        /// <param name="sensorType">The SDL SensorType to convert.</param>
        /// <returns>The corresponding GamepadSensorType enum value.</returns>
        public static GamepadSensorType Convert(SDL3.SDLSensorType sensorType)
        {
            return (GamepadSensorType)sensorType;
        }

        /// <summary>
        /// Converts an SDL JoystickType to its corresponding JoystickType enum value.
        /// </summary>
        /// <param name="joystickType">The SDL JoystickType to convert.</param>
        /// <returns>The corresponding JoystickType enum value.</returns>
        public static JoystickType Convert(SDL3.SDLJoystickType joystickType)
        {
            return (JoystickType)joystickType;
        }

        /// <summary>
        /// Converts an SDL PowerState to its corresponding PowerState enum value.
        /// </summary>
        /// <param name="powerState">The SDL PowerState to convert.</param>
        /// <returns>The corresponding PowerState enum value.</returns>
        public static PowerState Convert(SDL3.SDLPowerState powerState)
        {
            return (PowerState)powerState;
        }
    }
}