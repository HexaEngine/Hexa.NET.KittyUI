﻿namespace Hexa.NET.KittyUI.WebView
{
    using Hexa.NET.SDL2;
    using System;

    public static class CefHelper
    {
        public static VirtualKey MapSDLKeyCodeToVirtualKey(SDLKeyCode sdlKeyCode) => sdlKeyCode switch
        {
            SDLKeyCode.Unknown => throw new ArgumentOutOfRangeException(nameof(sdlKeyCode), "Unknown SDL key code"),
            SDLKeyCode.Return => VirtualKey.Return,
            SDLKeyCode.Escape => VirtualKey.Escape,
            SDLKeyCode.Backspace => VirtualKey.Back,
            SDLKeyCode.Tab => VirtualKey.Tab,
            SDLKeyCode.Space => VirtualKey.Space,
            SDLKeyCode.A => VirtualKey.A,
            SDLKeyCode.B => VirtualKey.B,
            SDLKeyCode.C => VirtualKey.C,
            SDLKeyCode.D => VirtualKey.D,
            SDLKeyCode.E => VirtualKey.E,
            SDLKeyCode.F => VirtualKey.F,
            SDLKeyCode.G => VirtualKey.G,
            SDLKeyCode.H => VirtualKey.H,
            SDLKeyCode.I => VirtualKey.I,
            SDLKeyCode.J => VirtualKey.J,
            SDLKeyCode.K => VirtualKey.K,
            SDLKeyCode.L => VirtualKey.L,
            SDLKeyCode.M => VirtualKey.M,
            SDLKeyCode.N => VirtualKey.N,
            SDLKeyCode.O => VirtualKey.O,
            SDLKeyCode.P => VirtualKey.P,
            SDLKeyCode.Q => VirtualKey.Q,
            SDLKeyCode.R => VirtualKey.R,
            SDLKeyCode.S => VirtualKey.S,
            SDLKeyCode.T => VirtualKey.T,
            SDLKeyCode.U => VirtualKey.U,
            SDLKeyCode.V => VirtualKey.V,
            SDLKeyCode.W => VirtualKey.W,
            SDLKeyCode.X => VirtualKey.X,
            SDLKeyCode.Y => VirtualKey.Y,
            SDLKeyCode.Z => VirtualKey.Z,
            SDLKeyCode.F1 => VirtualKey.F1,
            SDLKeyCode.F2 => VirtualKey.F2,
            SDLKeyCode.F3 => VirtualKey.F3,
            SDLKeyCode.F4 => VirtualKey.F4,
            SDLKeyCode.F5 => VirtualKey.F5,
            SDLKeyCode.F6 => VirtualKey.F6,
            SDLKeyCode.F7 => VirtualKey.F7,
            SDLKeyCode.F8 => VirtualKey.F8,
            SDLKeyCode.F9 => VirtualKey.F9,
            SDLKeyCode.F10 => VirtualKey.F10,
            SDLKeyCode.F11 => VirtualKey.F11,
            SDLKeyCode.F12 => VirtualKey.F12,
            SDLKeyCode.Insert => VirtualKey.Insert,
            SDLKeyCode.Delete => VirtualKey.Delete,
            SDLKeyCode.Home => VirtualKey.Home,
            SDLKeyCode.End => VirtualKey.End,
            SDLKeyCode.Pageup => VirtualKey.Prior,
            SDLKeyCode.Pagedown => VirtualKey.Next,
            SDLKeyCode.Left => VirtualKey.Left,
            SDLKeyCode.Right => VirtualKey.Right,
            SDLKeyCode.Up => VirtualKey.Up,
            SDLKeyCode.Down => VirtualKey.Down,
            SDLKeyCode.Capslock => VirtualKey.Capital,
            SDLKeyCode.Numlockclear => VirtualKey.NumLock,
            SDLKeyCode.Scrolllock => VirtualKey.Scroll,
            SDLKeyCode.Lctrl => VirtualKey.LControl,
            SDLKeyCode.Rctrl => VirtualKey.RControl,
            SDLKeyCode.Lshift => VirtualKey.LShift,
            SDLKeyCode.Rshift => VirtualKey.RShift,
            SDLKeyCode.Lalt => VirtualKey.LMenu,
            SDLKeyCode.Ralt => VirtualKey.RMenu,
            SDLKeyCode.Lgui => VirtualKey.LWin,
            SDLKeyCode.Rgui => VirtualKey.RWin,
            SDLKeyCode.Menu => VirtualKey.Apps,
            SDLKeyCode.K0 => VirtualKey.D0,
            SDLKeyCode.K1 => VirtualKey.D1,
            SDLKeyCode.K2 => VirtualKey.D2,
            SDLKeyCode.K3 => VirtualKey.D3,
            SDLKeyCode.K4 => VirtualKey.D4,
            SDLKeyCode.K5 => VirtualKey.D5,
            SDLKeyCode.K6 => VirtualKey.D6,
            SDLKeyCode.K7 => VirtualKey.D7,
            SDLKeyCode.K8 => VirtualKey.D8,
            SDLKeyCode.K9 => VirtualKey.D9,
            SDLKeyCode.KpDivide => VirtualKey.Divide,
            SDLKeyCode.KpMultiply => VirtualKey.Multiply,
            SDLKeyCode.KpMinus => VirtualKey.Subtract,
            SDLKeyCode.KpPlus => VirtualKey.Add,
            SDLKeyCode.KpEnter => VirtualKey.Return,
            SDLKeyCode.Kp0 => VirtualKey.Numpad0,
            SDLKeyCode.Kp1 => VirtualKey.Numpad1,
            SDLKeyCode.Kp2 => VirtualKey.Numpad2,
            SDLKeyCode.Kp3 => VirtualKey.Numpad3,
            SDLKeyCode.Kp4 => VirtualKey.Numpad4,
            SDLKeyCode.Kp5 => VirtualKey.Numpad5,
            SDLKeyCode.Kp6 => VirtualKey.Numpad6,
            SDLKeyCode.Kp7 => VirtualKey.Numpad7,
            SDLKeyCode.Kp8 => VirtualKey.Numpad8,
            SDLKeyCode.Kp9 => VirtualKey.Numpad9,
            SDLKeyCode.KpPeriod => VirtualKey.Decimal,
            SDLKeyCode.Printscreen => VirtualKey.Snapshot,
            SDLKeyCode.Pause => VirtualKey.Pause,
            SDLKeyCode.Help => VirtualKey.Help,
            SDLKeyCode.Audionext => VirtualKey.MediaNextTrack,
            SDLKeyCode.Audioprev => VirtualKey.MediaPrevTrack,
            SDLKeyCode.Audiostop => VirtualKey.MediaStop,
            SDLKeyCode.Audioplay => VirtualKey.MediaPlayPause,
            SDLKeyCode.Audiomute => VirtualKey.VolumeMute,
            SDLKeyCode.Volumedown => VirtualKey.VolumeDown,
            SDLKeyCode.Volumeup => VirtualKey.VolumeUp,
            SDLKeyCode.Sleep => VirtualKey.Sleep,
            SDLKeyCode.Again => VirtualKey.Execute,
            SDLKeyCode.Clear => VirtualKey.Clear,
            SDLKeyCode.Crsel => VirtualKey.CrSel,
            SDLKeyCode.Exsel => VirtualKey.ExSel,
            SDLKeyCode.Mode => VirtualKey.ModeChange,
            SDLKeyCode.KpEquals => VirtualKey.OemPlus,
            SDLKeyCode.KpComma => VirtualKey.OemComma,

            _ => throw new ArgumentOutOfRangeException(nameof(sdlKeyCode), $"Unhandled SDLKeyCode: {sdlKeyCode}")
        };
    }
}