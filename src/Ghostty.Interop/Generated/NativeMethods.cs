// Hand-written P/Invoke bindings for ghostty.h
// Can be regenerated via ClangSharp: ./generate-bindings.ps1
// Source: include/ghostty.h at commit 4661ab0af

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: DisableRuntimeMarshalling]

namespace Ghostty.Interop;

// -------------------------------------------------------------------
// Enums
// -------------------------------------------------------------------

public enum ghostty_platform_e
{
    GHOSTTY_PLATFORM_INVALID,
    GHOSTTY_PLATFORM_MACOS,
    GHOSTTY_PLATFORM_IOS,
    GHOSTTY_PLATFORM_WINDOWS,
}

public enum ghostty_clipboard_e
{
    GHOSTTY_CLIPBOARD_STANDARD,
    GHOSTTY_CLIPBOARD_SELECTION,
}

public enum ghostty_clipboard_request_e
{
    GHOSTTY_CLIPBOARD_REQUEST_PASTE,
    GHOSTTY_CLIPBOARD_REQUEST_OSC_52_READ,
    GHOSTTY_CLIPBOARD_REQUEST_OSC_52_WRITE,
}

public enum ghostty_input_mouse_state_e
{
    GHOSTTY_MOUSE_RELEASE,
    GHOSTTY_MOUSE_PRESS,
}

public enum ghostty_input_mouse_button_e
{
    GHOSTTY_MOUSE_UNKNOWN,
    GHOSTTY_MOUSE_LEFT,
    GHOSTTY_MOUSE_RIGHT,
    GHOSTTY_MOUSE_MIDDLE,
    GHOSTTY_MOUSE_FOUR,
    GHOSTTY_MOUSE_FIVE,
    GHOSTTY_MOUSE_SIX,
    GHOSTTY_MOUSE_SEVEN,
    GHOSTTY_MOUSE_EIGHT,
    GHOSTTY_MOUSE_NINE,
    GHOSTTY_MOUSE_TEN,
    GHOSTTY_MOUSE_ELEVEN,
}

public enum ghostty_input_mouse_momentum_e
{
    GHOSTTY_MOUSE_MOMENTUM_NONE,
    GHOSTTY_MOUSE_MOMENTUM_BEGAN,
    GHOSTTY_MOUSE_MOMENTUM_STATIONARY,
    GHOSTTY_MOUSE_MOMENTUM_CHANGED,
    GHOSTTY_MOUSE_MOMENTUM_ENDED,
    GHOSTTY_MOUSE_MOMENTUM_CANCELLED,
    GHOSTTY_MOUSE_MOMENTUM_MAY_BEGIN,
}

public enum ghostty_color_scheme_e
{
    GHOSTTY_COLOR_SCHEME_LIGHT = 0,
    GHOSTTY_COLOR_SCHEME_DARK = 1,
}

[Flags]
public enum ghostty_input_mods_e
{
    GHOSTTY_MODS_NONE = 0,
    GHOSTTY_MODS_SHIFT = 1 << 0,
    GHOSTTY_MODS_CTRL = 1 << 1,
    GHOSTTY_MODS_ALT = 1 << 2,
    GHOSTTY_MODS_SUPER = 1 << 3,
    GHOSTTY_MODS_CAPS = 1 << 4,
    GHOSTTY_MODS_NUM = 1 << 5,
    GHOSTTY_MODS_SHIFT_RIGHT = 1 << 6,
    GHOSTTY_MODS_CTRL_RIGHT = 1 << 7,
    GHOSTTY_MODS_ALT_RIGHT = 1 << 8,
    GHOSTTY_MODS_SUPER_RIGHT = 1 << 9,
}

[Flags]
public enum ghostty_binding_flags_e
{
    GHOSTTY_BINDING_FLAGS_CONSUMED = 1 << 0,
    GHOSTTY_BINDING_FLAGS_ALL = 1 << 1,
    GHOSTTY_BINDING_FLAGS_GLOBAL = 1 << 2,
    GHOSTTY_BINDING_FLAGS_PERFORMABLE = 1 << 3,
}

public enum ghostty_input_action_e
{
    GHOSTTY_ACTION_RELEASE,
    GHOSTTY_ACTION_PRESS,
    GHOSTTY_ACTION_REPEAT,
}

public enum ghostty_input_key_e
{
    GHOSTTY_KEY_UNIDENTIFIED,
    GHOSTTY_KEY_BACKQUOTE,
    GHOSTTY_KEY_BACKSLASH,
    GHOSTTY_KEY_BRACKET_LEFT,
    GHOSTTY_KEY_BRACKET_RIGHT,
    GHOSTTY_KEY_COMMA,
    GHOSTTY_KEY_DIGIT_0,
    GHOSTTY_KEY_DIGIT_1,
    GHOSTTY_KEY_DIGIT_2,
    GHOSTTY_KEY_DIGIT_3,
    GHOSTTY_KEY_DIGIT_4,
    GHOSTTY_KEY_DIGIT_5,
    GHOSTTY_KEY_DIGIT_6,
    GHOSTTY_KEY_DIGIT_7,
    GHOSTTY_KEY_DIGIT_8,
    GHOSTTY_KEY_DIGIT_9,
    GHOSTTY_KEY_EQUAL,
    GHOSTTY_KEY_INTL_BACKSLASH,
    GHOSTTY_KEY_INTL_RO,
    GHOSTTY_KEY_INTL_YEN,
    GHOSTTY_KEY_A,
    GHOSTTY_KEY_B,
    GHOSTTY_KEY_C,
    GHOSTTY_KEY_D,
    GHOSTTY_KEY_E,
    GHOSTTY_KEY_F,
    GHOSTTY_KEY_G,
    GHOSTTY_KEY_H,
    GHOSTTY_KEY_I,
    GHOSTTY_KEY_J,
    GHOSTTY_KEY_K,
    GHOSTTY_KEY_L,
    GHOSTTY_KEY_M,
    GHOSTTY_KEY_N,
    GHOSTTY_KEY_O,
    GHOSTTY_KEY_P,
    GHOSTTY_KEY_Q,
    GHOSTTY_KEY_R,
    GHOSTTY_KEY_S,
    GHOSTTY_KEY_T,
    GHOSTTY_KEY_U,
    GHOSTTY_KEY_V,
    GHOSTTY_KEY_W,
    GHOSTTY_KEY_X,
    GHOSTTY_KEY_Y,
    GHOSTTY_KEY_Z,
    GHOSTTY_KEY_MINUS,
    GHOSTTY_KEY_PERIOD,
    GHOSTTY_KEY_QUOTE,
    GHOSTTY_KEY_SEMICOLON,
    GHOSTTY_KEY_SLASH,
    GHOSTTY_KEY_ALT_LEFT,
    GHOSTTY_KEY_ALT_RIGHT,
    GHOSTTY_KEY_BACKSPACE,
    GHOSTTY_KEY_CAPS_LOCK,
    GHOSTTY_KEY_CONTEXT_MENU,
    GHOSTTY_KEY_CONTROL_LEFT,
    GHOSTTY_KEY_CONTROL_RIGHT,
    GHOSTTY_KEY_ENTER,
    GHOSTTY_KEY_META_LEFT,
    GHOSTTY_KEY_META_RIGHT,
    GHOSTTY_KEY_SHIFT_LEFT,
    GHOSTTY_KEY_SHIFT_RIGHT,
    GHOSTTY_KEY_SPACE,
    GHOSTTY_KEY_TAB,
    GHOSTTY_KEY_CONVERT,
    GHOSTTY_KEY_KANA_MODE,
    GHOSTTY_KEY_NON_CONVERT,
    GHOSTTY_KEY_DELETE,
    GHOSTTY_KEY_END,
    GHOSTTY_KEY_HELP,
    GHOSTTY_KEY_HOME,
    GHOSTTY_KEY_INSERT,
    GHOSTTY_KEY_PAGE_DOWN,
    GHOSTTY_KEY_PAGE_UP,
    GHOSTTY_KEY_ARROW_DOWN,
    GHOSTTY_KEY_ARROW_LEFT,
    GHOSTTY_KEY_ARROW_RIGHT,
    GHOSTTY_KEY_ARROW_UP,
    GHOSTTY_KEY_NUM_LOCK,
    GHOSTTY_KEY_NUMPAD_0,
    GHOSTTY_KEY_NUMPAD_1,
    GHOSTTY_KEY_NUMPAD_2,
    GHOSTTY_KEY_NUMPAD_3,
    GHOSTTY_KEY_NUMPAD_4,
    GHOSTTY_KEY_NUMPAD_5,
    GHOSTTY_KEY_NUMPAD_6,
    GHOSTTY_KEY_NUMPAD_7,
    GHOSTTY_KEY_NUMPAD_8,
    GHOSTTY_KEY_NUMPAD_9,
    GHOSTTY_KEY_NUMPAD_ADD,
    GHOSTTY_KEY_NUMPAD_BACKSPACE,
    GHOSTTY_KEY_NUMPAD_CLEAR,
    GHOSTTY_KEY_NUMPAD_CLEAR_ENTRY,
    GHOSTTY_KEY_NUMPAD_COMMA,
    GHOSTTY_KEY_NUMPAD_DECIMAL,
    GHOSTTY_KEY_NUMPAD_DIVIDE,
    GHOSTTY_KEY_NUMPAD_ENTER,
    GHOSTTY_KEY_NUMPAD_EQUAL,
    GHOSTTY_KEY_NUMPAD_MEMORY_ADD,
    GHOSTTY_KEY_NUMPAD_MEMORY_CLEAR,
    GHOSTTY_KEY_NUMPAD_MEMORY_RECALL,
    GHOSTTY_KEY_NUMPAD_MEMORY_STORE,
    GHOSTTY_KEY_NUMPAD_MEMORY_SUBTRACT,
    GHOSTTY_KEY_NUMPAD_MULTIPLY,
    GHOSTTY_KEY_NUMPAD_PAREN_LEFT,
    GHOSTTY_KEY_NUMPAD_PAREN_RIGHT,
    GHOSTTY_KEY_NUMPAD_SUBTRACT,
    GHOSTTY_KEY_NUMPAD_SEPARATOR,
    GHOSTTY_KEY_NUMPAD_UP,
    GHOSTTY_KEY_NUMPAD_DOWN,
    GHOSTTY_KEY_NUMPAD_RIGHT,
    GHOSTTY_KEY_NUMPAD_LEFT,
    GHOSTTY_KEY_NUMPAD_BEGIN,
    GHOSTTY_KEY_NUMPAD_HOME,
    GHOSTTY_KEY_NUMPAD_END,
    GHOSTTY_KEY_NUMPAD_INSERT,
    GHOSTTY_KEY_NUMPAD_DELETE,
    GHOSTTY_KEY_NUMPAD_PAGE_UP,
    GHOSTTY_KEY_NUMPAD_PAGE_DOWN,
    GHOSTTY_KEY_ESCAPE,
    GHOSTTY_KEY_F1,
    GHOSTTY_KEY_F2,
    GHOSTTY_KEY_F3,
    GHOSTTY_KEY_F4,
    GHOSTTY_KEY_F5,
    GHOSTTY_KEY_F6,
    GHOSTTY_KEY_F7,
    GHOSTTY_KEY_F8,
    GHOSTTY_KEY_F9,
    GHOSTTY_KEY_F10,
    GHOSTTY_KEY_F11,
    GHOSTTY_KEY_F12,
    GHOSTTY_KEY_F13,
    GHOSTTY_KEY_F14,
    GHOSTTY_KEY_F15,
    GHOSTTY_KEY_F16,
    GHOSTTY_KEY_F17,
    GHOSTTY_KEY_F18,
    GHOSTTY_KEY_F19,
    GHOSTTY_KEY_F20,
    GHOSTTY_KEY_F21,
    GHOSTTY_KEY_F22,
    GHOSTTY_KEY_F23,
    GHOSTTY_KEY_F24,
    GHOSTTY_KEY_F25,
    GHOSTTY_KEY_FN,
    GHOSTTY_KEY_FN_LOCK,
    GHOSTTY_KEY_PRINT_SCREEN,
    GHOSTTY_KEY_SCROLL_LOCK,
    GHOSTTY_KEY_PAUSE,
    GHOSTTY_KEY_BROWSER_BACK,
    GHOSTTY_KEY_BROWSER_FAVORITES,
    GHOSTTY_KEY_BROWSER_FORWARD,
    GHOSTTY_KEY_BROWSER_HOME,
    GHOSTTY_KEY_BROWSER_REFRESH,
    GHOSTTY_KEY_BROWSER_SEARCH,
    GHOSTTY_KEY_BROWSER_STOP,
    GHOSTTY_KEY_EJECT,
    GHOSTTY_KEY_LAUNCH_APP_1,
    GHOSTTY_KEY_LAUNCH_APP_2,
    GHOSTTY_KEY_LAUNCH_MAIL,
    GHOSTTY_KEY_MEDIA_PLAY_PAUSE,
    GHOSTTY_KEY_MEDIA_SELECT,
    GHOSTTY_KEY_MEDIA_STOP,
    GHOSTTY_KEY_MEDIA_TRACK_NEXT,
    GHOSTTY_KEY_MEDIA_TRACK_PREVIOUS,
    GHOSTTY_KEY_POWER,
    GHOSTTY_KEY_SLEEP,
    GHOSTTY_KEY_AUDIO_VOLUME_DOWN,
    GHOSTTY_KEY_AUDIO_VOLUME_MUTE,
    GHOSTTY_KEY_AUDIO_VOLUME_UP,
    GHOSTTY_KEY_WAKE_UP,
    GHOSTTY_KEY_COPY,
    GHOSTTY_KEY_CUT,
    GHOSTTY_KEY_PASTE,
}

public enum ghostty_input_trigger_tag_e
{
    GHOSTTY_TRIGGER_PHYSICAL,
    GHOSTTY_TRIGGER_UNICODE,
    GHOSTTY_TRIGGER_CATCH_ALL,
}

public enum ghostty_build_mode_e
{
    GHOSTTY_BUILD_MODE_DEBUG,
    GHOSTTY_BUILD_MODE_RELEASE_SAFE,
    GHOSTTY_BUILD_MODE_RELEASE_FAST,
    GHOSTTY_BUILD_MODE_RELEASE_SMALL,
}

public enum ghostty_surface_context_e
{
    GHOSTTY_SURFACE_CONTEXT_WINDOW = 0,
    GHOSTTY_SURFACE_CONTEXT_TAB = 1,
    GHOSTTY_SURFACE_CONTEXT_SPLIT = 2,
}

public enum ghostty_target_tag_e
{
    GHOSTTY_TARGET_APP,
    GHOSTTY_TARGET_SURFACE,
}

public enum ghostty_action_tag_e
{
    GHOSTTY_ACTION_QUIT,
    GHOSTTY_ACTION_NEW_WINDOW,
    GHOSTTY_ACTION_NEW_TAB,
    GHOSTTY_ACTION_CLOSE_TAB,
    GHOSTTY_ACTION_NEW_SPLIT,
    GHOSTTY_ACTION_CLOSE_ALL_WINDOWS,
    GHOSTTY_ACTION_TOGGLE_MAXIMIZE,
    GHOSTTY_ACTION_TOGGLE_FULLSCREEN,
    GHOSTTY_ACTION_TOGGLE_TAB_OVERVIEW,
    GHOSTTY_ACTION_TOGGLE_WINDOW_DECORATIONS,
    GHOSTTY_ACTION_TOGGLE_QUICK_TERMINAL,
    GHOSTTY_ACTION_TOGGLE_COMMAND_PALETTE,
    GHOSTTY_ACTION_TOGGLE_VISIBILITY,
    GHOSTTY_ACTION_TOGGLE_BACKGROUND_OPACITY,
    GHOSTTY_ACTION_MOVE_TAB,
    GHOSTTY_ACTION_GOTO_TAB,
    GHOSTTY_ACTION_GOTO_SPLIT,
    GHOSTTY_ACTION_GOTO_WINDOW,
    GHOSTTY_ACTION_RESIZE_SPLIT,
    GHOSTTY_ACTION_EQUALIZE_SPLITS,
    GHOSTTY_ACTION_TOGGLE_SPLIT_ZOOM,
    GHOSTTY_ACTION_PRESENT_TERMINAL,
    GHOSTTY_ACTION_SIZE_LIMIT,
    GHOSTTY_ACTION_RESET_WINDOW_SIZE,
    GHOSTTY_ACTION_INITIAL_SIZE,
    GHOSTTY_ACTION_CELL_SIZE,
    GHOSTTY_ACTION_SCROLLBAR,
    GHOSTTY_ACTION_RENDER,
    GHOSTTY_ACTION_INSPECTOR,
    GHOSTTY_ACTION_SHOW_GTK_INSPECTOR,
    GHOSTTY_ACTION_RENDER_INSPECTOR,
    GHOSTTY_ACTION_DESKTOP_NOTIFICATION,
    GHOSTTY_ACTION_SET_TITLE,
    GHOSTTY_ACTION_SET_TAB_TITLE,
    GHOSTTY_ACTION_PROMPT_TITLE,
    GHOSTTY_ACTION_PWD,
    GHOSTTY_ACTION_MOUSE_SHAPE,
    GHOSTTY_ACTION_MOUSE_VISIBILITY,
    GHOSTTY_ACTION_MOUSE_OVER_LINK,
    GHOSTTY_ACTION_RENDERER_HEALTH,
    GHOSTTY_ACTION_OPEN_CONFIG,
    GHOSTTY_ACTION_QUIT_TIMER,
    GHOSTTY_ACTION_FLOAT_WINDOW,
    GHOSTTY_ACTION_SECURE_INPUT,
    GHOSTTY_ACTION_KEY_SEQUENCE,
    GHOSTTY_ACTION_KEY_TABLE,
    GHOSTTY_ACTION_COLOR_CHANGE,
    GHOSTTY_ACTION_RELOAD_CONFIG,
    GHOSTTY_ACTION_CONFIG_CHANGE,
    GHOSTTY_ACTION_CLOSE_WINDOW,
    GHOSTTY_ACTION_RING_BELL,
    GHOSTTY_ACTION_UNDO,
    GHOSTTY_ACTION_REDO,
    GHOSTTY_ACTION_CHECK_FOR_UPDATES,
    GHOSTTY_ACTION_OPEN_URL,
    GHOSTTY_ACTION_SHOW_CHILD_EXITED,
    GHOSTTY_ACTION_PROGRESS_REPORT,
    GHOSTTY_ACTION_SHOW_ON_SCREEN_KEYBOARD,
    GHOSTTY_ACTION_COMMAND_FINISHED,
    GHOSTTY_ACTION_START_SEARCH,
    GHOSTTY_ACTION_END_SEARCH,
    GHOSTTY_ACTION_SEARCH_TOTAL,
    GHOSTTY_ACTION_SEARCH_SELECTED,
    GHOSTTY_ACTION_READONLY,
    GHOSTTY_ACTION_COPY_TITLE_TO_CLIPBOARD,
}

// -------------------------------------------------------------------
// Structs
// -------------------------------------------------------------------

[StructLayout(LayoutKind.Sequential)]
public struct ghostty_clipboard_content_s
{
    public nint mime;   // const char*
    public nint data;   // const char*
}

[StructLayout(LayoutKind.Sequential)]
public struct ghostty_input_key_s
{
    public ghostty_input_action_e action;
    public ghostty_input_mods_e mods;
    public ghostty_input_mods_e consumed_mods;
    public uint keycode;
    public nint text;   // const char*
    public uint unshifted_codepoint;
    public byte composing; // bool in C with DisableRuntimeMarshalling
}

[StructLayout(LayoutKind.Explicit)]
public struct ghostty_input_trigger_key_u
{
    [FieldOffset(0)] public ghostty_input_key_e translated;
    [FieldOffset(0)] public ghostty_input_key_e physical;
    [FieldOffset(0)] public uint unicode;
}

[StructLayout(LayoutKind.Sequential)]
public struct ghostty_input_trigger_s
{
    public ghostty_input_trigger_tag_e tag;
    public ghostty_input_trigger_key_u key;
    public ghostty_input_mods_e mods;
}

[StructLayout(LayoutKind.Sequential)]
public struct ghostty_info_s
{
    public ghostty_build_mode_e build_mode;
    public nint version;      // const char*
    public nuint version_len;
}

[StructLayout(LayoutKind.Sequential)]
public struct ghostty_diagnostic_s
{
    public nint message; // const char*
}

[StructLayout(LayoutKind.Sequential)]
public struct ghostty_string_s
{
    public nint ptr;    // const char*
    public nuint len;
    public byte sentinel; // bool
}

[StructLayout(LayoutKind.Sequential)]
public struct ghostty_platform_macos_s
{
    public nint nsview;
}

[StructLayout(LayoutKind.Sequential)]
public struct ghostty_platform_ios_s
{
    public nint uiview;
}

[StructLayout(LayoutKind.Sequential)]
public struct ghostty_platform_windows_s
{
    public nint hwnd;
    public nint swap_chain_panel;
}

[StructLayout(LayoutKind.Explicit)]
public struct ghostty_platform_u
{
    [FieldOffset(0)] public ghostty_platform_macos_s macos;
    [FieldOffset(0)] public ghostty_platform_ios_s ios;
    [FieldOffset(0)] public ghostty_platform_windows_s windows;
}

[StructLayout(LayoutKind.Sequential)]
public struct ghostty_env_var_s
{
    public nint key;   // const char*
    public nint value; // const char*
}

[StructLayout(LayoutKind.Sequential)]
public struct ghostty_surface_config_s
{
    public ghostty_platform_e platform_tag;
    public ghostty_platform_u platform;
    public nint userdata;
    public double scale_factor;
    public float font_size;
    public nint working_directory; // const char*
    public nint command;           // const char*
    public nint env_vars;          // ghostty_env_var_s*
    public nuint env_var_count;
    public nint initial_input;     // const char*
    public byte wait_after_command; // bool
    public ghostty_surface_context_e context;
}

[StructLayout(LayoutKind.Sequential)]
public struct ghostty_surface_size_s
{
    public ushort columns;
    public ushort rows;
    public uint width_px;
    public uint height_px;
    public uint cell_width_px;
    public uint cell_height_px;
}

[StructLayout(LayoutKind.Explicit)]
public struct ghostty_target_u
{
    [FieldOffset(0)] public nint surface; // ghostty_surface_t
}

[StructLayout(LayoutKind.Sequential)]
public struct ghostty_target_s
{
    public ghostty_target_tag_e tag;
    public ghostty_target_u target;
}

// The action union is large. We represent it as a fixed-size blob
// matching the largest union member, since most consumers only
// check the tag. Individual fields can be read via Unsafe.As
// or explicit struct overlays when needed.
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ghostty_action_u
{
    // The union's largest member is ~24 bytes on 64-bit.
    // Use 32 bytes for safety.
    public fixed byte _data[32];
}

[StructLayout(LayoutKind.Sequential)]
public struct ghostty_action_s
{
    public ghostty_action_tag_e tag;
    public ghostty_action_u action;
}

// -------------------------------------------------------------------
// Callback delegates (unmanaged function pointers)
// -------------------------------------------------------------------

// ghostty_runtime_wakeup_cb: void (*)(void*)
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void ghostty_runtime_wakeup_cb(nint userdata);

// ghostty_runtime_action_cb: bool (*)(ghostty_app_t, ghostty_target_s, ghostty_action_s)
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: MarshalAs(UnmanagedType.U1)]
public delegate bool ghostty_runtime_action_cb(nint app, ghostty_target_s target, ghostty_action_s action);

// ghostty_runtime_read_clipboard_cb: bool (*)(void*, ghostty_clipboard_e, void*)
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: MarshalAs(UnmanagedType.U1)]
public delegate bool ghostty_runtime_read_clipboard_cb(nint userdata, ghostty_clipboard_e loc, nint state);

// ghostty_runtime_confirm_read_clipboard_cb: void (*)(void*, const char*, void*, ghostty_clipboard_request_e)
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void ghostty_runtime_confirm_read_clipboard_cb(nint userdata, nint str, nint state, ghostty_clipboard_request_e req);

// ghostty_runtime_write_clipboard_cb: void (*)(void*, ghostty_clipboard_e, const ghostty_clipboard_content_s*, size_t, bool)
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void ghostty_runtime_write_clipboard_cb(nint userdata, ghostty_clipboard_e loc, nint content, nuint contentCount, byte confirm);

// ghostty_runtime_close_surface_cb: void (*)(void*, bool)
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void ghostty_runtime_close_surface_cb(nint userdata, byte processAlive);

// -------------------------------------------------------------------
// Runtime config struct
// -------------------------------------------------------------------

[StructLayout(LayoutKind.Sequential)]
public struct ghostty_runtime_config_s
{
    public nint userdata;
    public byte supports_selection_clipboard; // bool
    public nint wakeup_cb;                    // function pointer
    public nint action_cb;                    // function pointer
    public nint read_clipboard_cb;            // function pointer
    public nint confirm_read_clipboard_cb;    // function pointer
    public nint write_clipboard_cb;           // function pointer
    public nint close_surface_cb;             // function pointer
}

// -------------------------------------------------------------------
// Native methods (P/Invoke)
// -------------------------------------------------------------------

public static partial class NativeMethods
{
    private const string LibName = "ghostty";

    // --- Global ---

    [LibraryImport(LibName)]
    public static partial int ghostty_init(nuint argc, nint argv);

    [LibraryImport(LibName)]
    public static partial ghostty_info_s ghostty_info();

    [LibraryImport(LibName)]
    public static partial void ghostty_string_free(ghostty_string_s str);

    // --- Config ---

    [LibraryImport(LibName)]
    public static partial nint ghostty_config_new();

    [LibraryImport(LibName)]
    public static partial void ghostty_config_free(nint config);

    [LibraryImport(LibName)]
    public static partial nint ghostty_config_clone(nint config);

    [LibraryImport(LibName)]
    public static partial void ghostty_config_load_cli_args(nint config);

    [LibraryImport(LibName)]
    public static partial void ghostty_config_load_default_files(nint config);

    [LibraryImport(LibName)]
    public static partial void ghostty_config_load_recursive_files(nint config);

    [LibraryImport(LibName)]
    public static partial void ghostty_config_finalize(nint config);

    [LibraryImport(LibName)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool ghostty_config_get(nint config, nint value, nint key, nuint keyLen);

    [LibraryImport(LibName)]
    public static partial uint ghostty_config_diagnostics_count(nint config);

    [LibraryImport(LibName)]
    public static partial ghostty_diagnostic_s ghostty_config_get_diagnostic(nint config, uint index);

    // --- App ---

    [LibraryImport(LibName)]
    public static partial nint ghostty_app_new(in ghostty_runtime_config_s runtimeCfg, nint config);

    [LibraryImport(LibName)]
    public static partial void ghostty_app_free(nint app);

    [LibraryImport(LibName)]
    public static partial void ghostty_app_tick(nint app);

    [LibraryImport(LibName)]
    public static partial nint ghostty_app_userdata(nint app);

    [LibraryImport(LibName)]
    public static partial void ghostty_app_set_focus(nint app, [MarshalAs(UnmanagedType.U1)] bool focused);

    [LibraryImport(LibName)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool ghostty_app_key(nint app, ghostty_input_key_s key);

    [LibraryImport(LibName)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool ghostty_app_key_is_binding(nint app, ghostty_input_key_s key);

    [LibraryImport(LibName)]
    public static partial void ghostty_app_keyboard_changed(nint app);

    [LibraryImport(LibName)]
    public static partial void ghostty_app_open_config(nint app);

    [LibraryImport(LibName)]
    public static partial void ghostty_app_update_config(nint app, nint config);

    [LibraryImport(LibName)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool ghostty_app_needs_confirm_quit(nint app);

    [LibraryImport(LibName)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool ghostty_app_has_global_keybinds(nint app);

    [LibraryImport(LibName)]
    public static partial void ghostty_app_set_color_scheme(nint app, ghostty_color_scheme_e scheme);

    // --- Surface config ---

    [LibraryImport(LibName)]
    public static partial ghostty_surface_config_s ghostty_surface_config_new();

    // --- Surface ---

    [LibraryImport(LibName)]
    public static partial nint ghostty_surface_new(nint app, in ghostty_surface_config_s config);

    [LibraryImport(LibName)]
    public static partial void ghostty_surface_free(nint surface);

    [LibraryImport(LibName)]
    public static partial nint ghostty_surface_userdata(nint surface);

    [LibraryImport(LibName)]
    public static partial nint ghostty_surface_app(nint surface);

    [LibraryImport(LibName)]
    public static partial void ghostty_surface_refresh(nint surface);

    [LibraryImport(LibName)]
    public static partial void ghostty_surface_draw(nint surface);

    [LibraryImport(LibName)]
    public static partial void ghostty_surface_set_content_scale(nint surface, double x, double y);

    [LibraryImport(LibName)]
    public static partial void ghostty_surface_set_focus(nint surface, [MarshalAs(UnmanagedType.U1)] bool focused);

    [LibraryImport(LibName)]
    public static partial void ghostty_surface_set_occlusion(nint surface, [MarshalAs(UnmanagedType.U1)] bool visible);

    [LibraryImport(LibName)]
    public static partial void ghostty_surface_set_size(nint surface, uint width, uint height);

    [LibraryImport(LibName)]
    public static partial ghostty_surface_size_s ghostty_surface_size(nint surface);

    [LibraryImport(LibName)]
    public static partial void ghostty_surface_set_color_scheme(nint surface, ghostty_color_scheme_e scheme);

    [LibraryImport(LibName)]
    public static partial ghostty_input_mods_e ghostty_surface_key_translation_mods(nint surface, ghostty_input_mods_e mods);

    [LibraryImport(LibName)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool ghostty_surface_key(nint surface, ghostty_input_key_s key);

    [LibraryImport(LibName)]
    public static partial void ghostty_surface_text(nint surface, nint text, nuint len);

    [LibraryImport(LibName)]
    public static partial void ghostty_surface_preedit(nint surface, nint text, nuint len);

    [LibraryImport(LibName)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool ghostty_surface_mouse_captured(nint surface);

    [LibraryImport(LibName)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool ghostty_surface_mouse_button(
        nint surface,
        ghostty_input_mouse_state_e state,
        ghostty_input_mouse_button_e button,
        ghostty_input_mods_e mods);

    [LibraryImport(LibName)]
    public static partial void ghostty_surface_mouse_pos(
        nint surface, double x, double y, ghostty_input_mods_e mods);

    [LibraryImport(LibName)]
    public static partial void ghostty_surface_mouse_scroll(
        nint surface, double x, double y, int mods);

    [LibraryImport(LibName)]
    public static partial void ghostty_surface_mouse_pressure(nint surface, uint stage, double pressure);

    [LibraryImport(LibName)]
    public static partial void ghostty_surface_request_close(nint surface);

    [LibraryImport(LibName)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool ghostty_surface_needs_confirm_quit(nint surface);

    [LibraryImport(LibName)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool ghostty_surface_process_exited(nint surface);

    [LibraryImport(LibName)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool ghostty_surface_has_selection(nint surface);

    [LibraryImport(LibName)]
    public static partial void ghostty_surface_complete_clipboard_request(
        nint surface, nint str, nint state, [MarshalAs(UnmanagedType.U1)] bool confirmed);

    // --- Inspector ---

    [LibraryImport(LibName)]
    public static partial nint ghostty_surface_inspector(nint surface);

    [LibraryImport(LibName)]
    public static partial void ghostty_inspector_free(nint surface);

    [LibraryImport(LibName)]
    public static partial void ghostty_inspector_set_focus(nint inspector, [MarshalAs(UnmanagedType.U1)] bool focused);

    [LibraryImport(LibName)]
    public static partial void ghostty_inspector_set_content_scale(nint inspector, double x, double y);

    [LibraryImport(LibName)]
    public static partial void ghostty_inspector_set_size(nint inspector, uint width, uint height);
}
