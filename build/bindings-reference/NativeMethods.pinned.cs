using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghostty.Vt.Native
{
    public enum GhosttyResult
    {
        GHOSTTY_SUCCESS = 0,
        GHOSTTY_OUT_OF_MEMORY = -1,
        GHOSTTY_INVALID_VALUE = -2,
        GHOSTTY_OUT_OF_SPACE = -3,
        GHOSTTY_NO_VALUE = -4,
        GHOSTTY_IO_ERROR = -5,
        GHOSTTY_LIMIT_EXCEEDED = -6,
        GHOSTTY_REJECTED = -7,
        GHOSTTY_RESULT_MAX_VALUE = 2147483647,
    }

    public partial struct GhosttyTerminalImpl
    {
    }

    public partial struct GhosttySnapshotDecoderImpl
    {
    }

    public partial struct GhosttyTrackedGridRefImpl
    {
    }

    public partial struct GhosttyKittyGraphicsImpl
    {
    }

    public partial struct GhosttyKittyGraphicsImageImpl
    {
    }

    public partial struct GhosttyKittyGraphicsPlacementIteratorImpl
    {
    }

    public partial struct GhosttyRenderStateImpl
    {
    }

    public partial struct GhosttyRenderStateRowIteratorImpl
    {
    }

    public partial struct GhosttyRenderStateRowCellsImpl
    {
    }

    public partial struct GhosttySearchImpl
    {
    }

    public partial struct GhosttySgrParserImpl
    {
    }

    public partial struct GhosttyFormatterImpl
    {
    }

    public partial struct GhosttyOscParserImpl
    {
    }

    public partial struct GhosttyOscCommandImpl
    {
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyFormatterFormat : uint
    {
        GHOSTTY_FORMATTER_FORMAT_PLAIN,
        GHOSTTY_FORMATTER_FORMAT_VT,
        GHOSTTY_FORMATTER_FORMAT_HTML,
        GHOSTTY_FORMATTER_FORMAT_MAX_VALUE = 2147483647,
    }

    public unsafe partial struct GhosttyString
    {
        [NativeTypeName("const uint8_t *")]
        public byte* ptr;

        [NativeTypeName("size_t")]
        public nuint len;
    }

    public unsafe partial struct GhosttyBuffer
    {
        [NativeTypeName("uint8_t *")]
        public byte* ptr;

        [NativeTypeName("size_t")]
        public nuint cap;

        [NativeTypeName("size_t")]
        public nuint len;
    }

    public partial struct GhosttySurfacePosition
    {
        public double x;

        public double y;
    }

    public unsafe partial struct GhosttyCodepoints
    {
        [NativeTypeName("const uint32_t *")]
        public uint* ptr;

        [NativeTypeName("size_t")]
        public nuint len;
    }

    public unsafe partial struct GhosttyAllocatorVtable
    {
        [NativeTypeName("void *(*)(void *, size_t, uint8_t, uintptr_t)")]
        public delegate* unmanaged[Cdecl]<void*, nuint, byte, nuint, void*> alloc;

        [NativeTypeName("_Bool (*)(void *, void *, size_t, uint8_t, size_t, uintptr_t)")]
        public delegate* unmanaged[Cdecl]<void*, void*, nuint, byte, nuint, nuint, byte> resize;

        [NativeTypeName("void *(*)(void *, void *, size_t, uint8_t, size_t, uintptr_t)")]
        public delegate* unmanaged[Cdecl]<void*, void*, nuint, byte, nuint, nuint, void*> remap;

        [NativeTypeName("void (*)(void *, void *, size_t, uint8_t, uintptr_t)")]
        public delegate* unmanaged[Cdecl]<void*, void*, nuint, byte, nuint, void> free;
    }

    public unsafe partial struct GhosttyAllocator
    {
        public void* ctx;

        [NativeTypeName("const GhosttyAllocatorVtable *")]
        public GhosttyAllocatorVtable* vtable;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyOptimizeMode : uint
    {
        GHOSTTY_OPTIMIZE_DEBUG = 0,
        GHOSTTY_OPTIMIZE_RELEASE_SAFE = 1,
        GHOSTTY_OPTIMIZE_RELEASE_SMALL = 2,
        GHOSTTY_OPTIMIZE_RELEASE_FAST = 3,
        GHOSTTY_OPTIMIZE_MODE_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyBuildInfo : uint
    {
        GHOSTTY_BUILD_INFO_INVALID = 0,
        GHOSTTY_BUILD_INFO_SIMD = 1,
        GHOSTTY_BUILD_INFO_KITTY_GRAPHICS = 2,
        GHOSTTY_BUILD_INFO_TMUX_CONTROL_MODE = 3,
        GHOSTTY_BUILD_INFO_OPTIMIZE = 4,
        GHOSTTY_BUILD_INFO_VERSION_STRING = 5,
        GHOSTTY_BUILD_INFO_VERSION_MAJOR = 6,
        GHOSTTY_BUILD_INFO_VERSION_MINOR = 7,
        GHOSTTY_BUILD_INFO_VERSION_PATCH = 8,
        GHOSTTY_BUILD_INFO_VERSION_PRE = 9,
        GHOSTTY_BUILD_INFO_VERSION_BUILD = 10,
        GHOSTTY_BUILD_INFO_MAX_VALUE = 2147483647,
    }

    public partial struct GhosttyColorRgb
    {
        [NativeTypeName("uint8_t")]
        public byte r;

        [NativeTypeName("uint8_t")]
        public byte g;

        [NativeTypeName("uint8_t")]
        public byte b;
    }

    public partial struct GhosttyColorPaletteMask
    {
        [NativeTypeName("uint64_t[4]")]
        public _bits_e__FixedBuffer bits;

        [InlineArray(4)]
        public partial struct _bits_e__FixedBuffer
        {
            public nuint e0;
        }
    }

    public unsafe partial struct GhosttyColorX11Entry
    {
        [NativeTypeName("const char *")]
        public sbyte* name;

        public GhosttyColorRgb color;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyColorScheme : uint
    {
        GHOSTTY_COLOR_SCHEME_LIGHT = 0,
        GHOSTTY_COLOR_SCHEME_DARK = 1,
        GHOSTTY_COLOR_SCHEME_MAX_VALUE = 2147483647,
    }

    public partial struct GhosttyDeviceAttributesPrimary
    {
        [NativeTypeName("uint16_t")]
        public ushort conformance_level;

        [NativeTypeName("uint16_t[64]")]
        public _features_e__FixedBuffer features;

        [NativeTypeName("size_t")]
        public nuint num_features;

        [InlineArray(64)]
        public partial struct _features_e__FixedBuffer
        {
            public ushort e0;
        }
    }

    public partial struct GhosttyDeviceAttributesSecondary
    {
        [NativeTypeName("uint16_t")]
        public ushort device_type;

        [NativeTypeName("uint16_t")]
        public ushort firmware_version;

        [NativeTypeName("uint16_t")]
        public ushort rom_cartridge;
    }

    public partial struct GhosttyDeviceAttributesTertiary
    {
        [NativeTypeName("uint32_t")]
        public uint unit_id;
    }

    public partial struct GhosttyDeviceAttributes
    {
        public GhosttyDeviceAttributesPrimary primary;

        public GhosttyDeviceAttributesSecondary secondary;

        public GhosttyDeviceAttributesTertiary tertiary;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyFocusEvent : uint
    {
        GHOSTTY_FOCUS_GAINED = 0,
        GHOSTTY_FOCUS_LOST = 1,
        GHOSTTY_FOCUS_MAX_VALUE = 2147483647,
    }

    public unsafe partial struct GhosttyReader
    {
        [NativeTypeName("GhosttyReaderFn")]
        public delegate* unmanaged[Cdecl]<void*, byte*, nuint, nuint*, byte> read;

        public void* userdata;
    }

    public unsafe partial struct GhosttyWriter
    {
        [NativeTypeName("GhosttyWriterFn")]
        public delegate* unmanaged[Cdecl]<void*, byte*, nuint, byte> write;

        public void* userdata;
    }

    public unsafe partial struct GhosttyMimeReader
    {
        [NativeTypeName("GhosttyMimeReaderFn")]
        public delegate* unmanaged[Cdecl]<void*, GhosttyString, GhosttyWriter, byte> read;

        public void* userdata;
    }

    public unsafe partial struct GhosttyCellsView
    {
        [NativeTypeName("const GhosttyCell *")]
        public nuint* ptr;

        [NativeTypeName("size_t")]
        public nuint len;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyCellContentTag : uint
    {
        GHOSTTY_CELL_CONTENT_CODEPOINT = 0,
        GHOSTTY_CELL_CONTENT_CODEPOINT_GRAPHEME = 1,
        GHOSTTY_CELL_CONTENT_BG_COLOR_PALETTE = 2,
        GHOSTTY_CELL_CONTENT_BG_COLOR_RGB = 3,
        GHOSTTY_CELL_CONTENT_TAG_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyCellWide : uint
    {
        GHOSTTY_CELL_WIDE_NARROW = 0,
        GHOSTTY_CELL_WIDE_WIDE = 1,
        GHOSTTY_CELL_WIDE_SPACER_TAIL = 2,
        GHOSTTY_CELL_WIDE_SPACER_HEAD = 3,
        GHOSTTY_CELL_WIDE_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyCellSemanticContent : uint
    {
        GHOSTTY_CELL_SEMANTIC_OUTPUT = 0,
        GHOSTTY_CELL_SEMANTIC_INPUT = 1,
        GHOSTTY_CELL_SEMANTIC_PROMPT = 2,
        GHOSTTY_CELL_SEMANTIC_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyCellData : uint
    {
        GHOSTTY_CELL_DATA_INVALID = 0,
        GHOSTTY_CELL_DATA_CODEPOINT = 1,
        GHOSTTY_CELL_DATA_CONTENT_TAG = 2,
        GHOSTTY_CELL_DATA_WIDE = 3,
        GHOSTTY_CELL_DATA_HAS_TEXT = 4,
        GHOSTTY_CELL_DATA_HAS_STYLING = 5,
        GHOSTTY_CELL_DATA_STYLE_ID = 6,
        GHOSTTY_CELL_DATA_HAS_HYPERLINK = 7,
        GHOSTTY_CELL_DATA_PROTECTED = 8,
        GHOSTTY_CELL_DATA_SEMANTIC_CONTENT = 9,
        GHOSTTY_CELL_DATA_COLOR_PALETTE = 10,
        GHOSTTY_CELL_DATA_COLOR_RGB = 11,
        GHOSTTY_CELL_DATA_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyRowSemanticPrompt : uint
    {
        GHOSTTY_ROW_SEMANTIC_NONE = 0,
        GHOSTTY_ROW_SEMANTIC_PROMPT = 1,
        GHOSTTY_ROW_SEMANTIC_PROMPT_CONTINUATION = 2,
        GHOSTTY_ROW_SEMANTIC_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyRowData : uint
    {
        GHOSTTY_ROW_DATA_INVALID = 0,
        GHOSTTY_ROW_DATA_WRAP = 1,
        GHOSTTY_ROW_DATA_WRAP_CONTINUATION = 2,
        GHOSTTY_ROW_DATA_GRAPHEME = 3,
        GHOSTTY_ROW_DATA_STYLED = 4,
        GHOSTTY_ROW_DATA_HYPERLINK = 5,
        GHOSTTY_ROW_DATA_SEMANTIC_PROMPT = 6,
        GHOSTTY_ROW_DATA_KITTY_VIRTUAL_PLACEHOLDER = 7,
        GHOSTTY_ROW_DATA_DIRTY = 8,
        GHOSTTY_ROW_DATA_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyStyleColorTag : uint
    {
        GHOSTTY_STYLE_COLOR_NONE = 0,
        GHOSTTY_STYLE_COLOR_PALETTE = 1,
        GHOSTTY_STYLE_COLOR_RGB = 2,
        GHOSTTY_STYLE_COLOR_TAG_MAX_VALUE = 2147483647,
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct GhosttyStyleColorValue
    {
        [FieldOffset(0)]
        [NativeTypeName("GhosttyColorPaletteIndex")]
        public byte palette;

        [FieldOffset(0)]
        public GhosttyColorRgb rgb;

        [FieldOffset(0)]
        [NativeTypeName("uint64_t")]
        public nuint _padding;
    }

    public partial struct GhosttyStyleColor
    {
        public GhosttyStyleColorTag tag;

        public GhosttyStyleColorValue value;
    }

    public partial struct GhosttyStyle
    {
        [NativeTypeName("size_t")]
        public nuint size;

        public GhosttyStyleColor fg_color;

        public GhosttyStyleColor bg_color;

        public GhosttyStyleColor underline_color;

        [NativeTypeName("_Bool")]
        public byte bold;

        [NativeTypeName("_Bool")]
        public byte italic;

        [NativeTypeName("_Bool")]
        public byte faint;

        [NativeTypeName("_Bool")]
        public byte blink;

        [NativeTypeName("_Bool")]
        public byte inverse;

        [NativeTypeName("_Bool")]
        public byte invisible;

        [NativeTypeName("_Bool")]
        public byte strikethrough;

        [NativeTypeName("_Bool")]
        public byte overline;

        public int underline;
    }

    public unsafe partial struct GhosttyGridRef
    {
        [NativeTypeName("size_t")]
        public nuint size;

        public void* node;

        [NativeTypeName("uint16_t")]
        public ushort x;

        [NativeTypeName("uint16_t")]
        public ushort y;
    }

    public partial struct GhosttyPointCoordinate
    {
        [NativeTypeName("uint16_t")]
        public ushort x;

        [NativeTypeName("uint32_t")]
        public uint y;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyPointTag : uint
    {
        GHOSTTY_POINT_TAG_ACTIVE = 0,
        GHOSTTY_POINT_TAG_VIEWPORT = 1,
        GHOSTTY_POINT_TAG_SCREEN = 2,
        GHOSTTY_POINT_TAG_HISTORY = 3,
        GHOSTTY_POINT_TAG_MAX_VALUE = 2147483647,
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct GhosttyPointValue
    {
        [FieldOffset(0)]
        public GhosttyPointCoordinate coordinate;

        [FieldOffset(0)]
        [NativeTypeName("uint64_t[2]")]
        public __padding_e__FixedBuffer _padding;

        [InlineArray(2)]
        public partial struct __padding_e__FixedBuffer
        {
            public nuint e0;
        }
    }

    public partial struct GhosttyPoint
    {
        public GhosttyPointTag tag;

        public GhosttyPointValue value;
    }

    public partial struct GhosttySelectionGestureImpl
    {
    }

    public partial struct GhosttySelectionGestureEventImpl
    {
    }

    public partial struct GhosttySelection
    {
        [NativeTypeName("size_t")]
        public nuint size;

        public GhosttyGridRef start;

        public GhosttyGridRef end;

        [NativeTypeName("_Bool")]
        public byte rectangle;
    }

    public unsafe partial struct GhosttySelectionBuffer
    {
        public GhosttySelection* ptr;

        [NativeTypeName("size_t")]
        public nuint cap;

        [NativeTypeName("size_t")]
        public nuint len;
    }

    public unsafe partial struct GhosttyTerminalSelectWordOptions
    {
        [NativeTypeName("size_t")]
        public nuint size;

        public GhosttyGridRef @ref;

        [NativeTypeName("const uint32_t *")]
        public uint* boundary_codepoints;

        [NativeTypeName("size_t")]
        public nuint boundary_codepoints_len;
    }

    public unsafe partial struct GhosttyTerminalSelectWordBetweenOptions
    {
        [NativeTypeName("size_t")]
        public nuint size;

        public GhosttyGridRef start;

        public GhosttyGridRef end;

        [NativeTypeName("const uint32_t *")]
        public uint* boundary_codepoints;

        [NativeTypeName("size_t")]
        public nuint boundary_codepoints_len;
    }

    public unsafe partial struct GhosttyTerminalSelectLineOptions
    {
        [NativeTypeName("size_t")]
        public nuint size;

        public GhosttyGridRef @ref;

        [NativeTypeName("const uint32_t *")]
        public uint* whitespace;

        [NativeTypeName("size_t")]
        public nuint whitespace_len;

        [NativeTypeName("_Bool")]
        public byte semantic_prompt_boundary;
    }

    public unsafe partial struct GhosttyTerminalSelectionFormatOptions
    {
        [NativeTypeName("size_t")]
        public nuint size;

        public GhosttyFormatterFormat emit;

        [NativeTypeName("_Bool")]
        public byte unwrap;

        [NativeTypeName("_Bool")]
        public byte trim;

        [NativeTypeName("const GhosttySelection *")]
        public GhosttySelection* selection;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttySelectionOrder : uint
    {
        GHOSTTY_SELECTION_ORDER_FORWARD = 0,
        GHOSTTY_SELECTION_ORDER_REVERSE = 1,
        GHOSTTY_SELECTION_ORDER_MIRRORED_FORWARD = 2,
        GHOSTTY_SELECTION_ORDER_MIRRORED_REVERSE = 3,
        GHOSTTY_SELECTION_ORDER_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttySelectionAdjust : uint
    {
        GHOSTTY_SELECTION_ADJUST_LEFT = 0,
        GHOSTTY_SELECTION_ADJUST_RIGHT = 1,
        GHOSTTY_SELECTION_ADJUST_UP = 2,
        GHOSTTY_SELECTION_ADJUST_DOWN = 3,
        GHOSTTY_SELECTION_ADJUST_HOME = 4,
        GHOSTTY_SELECTION_ADJUST_END = 5,
        GHOSTTY_SELECTION_ADJUST_PAGE_UP = 6,
        GHOSTTY_SELECTION_ADJUST_PAGE_DOWN = 7,
        GHOSTTY_SELECTION_ADJUST_BEGINNING_OF_LINE = 8,
        GHOSTTY_SELECTION_ADJUST_END_OF_LINE = 9,
        GHOSTTY_SELECTION_ADJUST_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttySelectionGestureBehavior : uint
    {
        GHOSTTY_SELECTION_GESTURE_BEHAVIOR_CELL = 0,
        GHOSTTY_SELECTION_GESTURE_BEHAVIOR_WORD = 1,
        GHOSTTY_SELECTION_GESTURE_BEHAVIOR_LINE = 2,
        GHOSTTY_SELECTION_GESTURE_BEHAVIOR_OUTPUT = 3,
        GHOSTTY_SELECTION_GESTURE_BEHAVIOR_MAX_VALUE = 2147483647,
    }

    public partial struct GhosttySelectionGestureBehaviors
    {
        public GhosttySelectionGestureBehavior single_click;

        public GhosttySelectionGestureBehavior double_click;

        public GhosttySelectionGestureBehavior triple_click;
    }

    public partial struct GhosttySelectionGestureGeometry
    {
        [NativeTypeName("uint32_t")]
        public uint columns;

        [NativeTypeName("uint32_t")]
        public uint cell_width;

        [NativeTypeName("uint32_t")]
        public uint padding_left;

        [NativeTypeName("uint32_t")]
        public uint screen_height;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttySelectionGestureAutoscroll : uint
    {
        GHOSTTY_SELECTION_GESTURE_AUTOSCROLL_NONE = 0,
        GHOSTTY_SELECTION_GESTURE_AUTOSCROLL_UP = 1,
        GHOSTTY_SELECTION_GESTURE_AUTOSCROLL_DOWN = 2,
        GHOSTTY_SELECTION_GESTURE_AUTOSCROLL_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttySelectionGestureData : uint
    {
        GHOSTTY_SELECTION_GESTURE_DATA_CLICK_COUNT = 0,
        GHOSTTY_SELECTION_GESTURE_DATA_DRAGGED = 1,
        GHOSTTY_SELECTION_GESTURE_DATA_AUTOSCROLL = 2,
        GHOSTTY_SELECTION_GESTURE_DATA_BEHAVIOR = 3,
        GHOSTTY_SELECTION_GESTURE_DATA_ANCHOR = 4,
        GHOSTTY_SELECTION_GESTURE_DATA_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttySelectionGestureEventType : uint
    {
        GHOSTTY_SELECTION_GESTURE_EVENT_TYPE_PRESS = 0,
        GHOSTTY_SELECTION_GESTURE_EVENT_TYPE_RELEASE = 1,
        GHOSTTY_SELECTION_GESTURE_EVENT_TYPE_DRAG = 2,
        GHOSTTY_SELECTION_GESTURE_EVENT_TYPE_AUTOSCROLL_TICK = 3,
        GHOSTTY_SELECTION_GESTURE_EVENT_TYPE_DEEP_PRESS = 4,
        GHOSTTY_SELECTION_GESTURE_EVENT_TYPE_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttySelectionGestureEventOption : uint
    {
        GHOSTTY_SELECTION_GESTURE_EVENT_OPT_REF = 0,
        GHOSTTY_SELECTION_GESTURE_EVENT_OPT_POSITION = 1,
        GHOSTTY_SELECTION_GESTURE_EVENT_OPT_REPEAT_DISTANCE = 2,
        GHOSTTY_SELECTION_GESTURE_EVENT_OPT_TIME_NS = 3,
        GHOSTTY_SELECTION_GESTURE_EVENT_OPT_REPEAT_INTERVAL_NS = 4,
        GHOSTTY_SELECTION_GESTURE_EVENT_OPT_WORD_BOUNDARY_CODEPOINTS = 5,
        GHOSTTY_SELECTION_GESTURE_EVENT_OPT_BEHAVIORS = 6,
        GHOSTTY_SELECTION_GESTURE_EVENT_OPT_RECTANGLE = 7,
        GHOSTTY_SELECTION_GESTURE_EVENT_OPT_GEOMETRY = 8,
        GHOSTTY_SELECTION_GESTURE_EVENT_OPT_VIEWPORT = 9,
        GHOSTTY_SELECTION_GESTURE_EVENT_OPT_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyModeReportState : uint
    {
        GHOSTTY_MODE_REPORT_NOT_RECOGNIZED = 0,
        GHOSTTY_MODE_REPORT_SET = 1,
        GHOSTTY_MODE_REPORT_RESET = 2,
        GHOSTTY_MODE_REPORT_PERMANENTLY_SET = 3,
        GHOSTTY_MODE_REPORT_PERMANENTLY_RESET = 4,
        GHOSTTY_MODE_REPORT_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttySizeReportStyle : uint
    {
        GHOSTTY_SIZE_REPORT_MODE_2048 = 0,
        GHOSTTY_SIZE_REPORT_CSI_14_T = 1,
        GHOSTTY_SIZE_REPORT_CSI_16_T = 2,
        GHOSTTY_SIZE_REPORT_CSI_18_T = 3,
        GHOSTTY_SIZE_REPORT_STYLE_MAX_VALUE = 2147483647,
    }

    public partial struct GhosttySizeReportSize
    {
        [NativeTypeName("uint16_t")]
        public ushort rows;

        [NativeTypeName("uint16_t")]
        public ushort columns;

        [NativeTypeName("uint32_t")]
        public uint cell_width;

        [NativeTypeName("uint32_t")]
        public uint cell_height;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyKittyGraphicsData : uint
    {
        GHOSTTY_KITTY_GRAPHICS_DATA_INVALID = 0,
        GHOSTTY_KITTY_GRAPHICS_DATA_PLACEMENT_ITERATOR = 1,
        GHOSTTY_KITTY_GRAPHICS_DATA_GENERATION = 2,
        GHOSTTY_KITTY_GRAPHICS_DATA_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyKittyGraphicsPlacementData : uint
    {
        GHOSTTY_KITTY_GRAPHICS_PLACEMENT_DATA_INVALID = 0,
        GHOSTTY_KITTY_GRAPHICS_PLACEMENT_DATA_IMAGE_ID = 1,
        GHOSTTY_KITTY_GRAPHICS_PLACEMENT_DATA_PLACEMENT_ID = 2,
        GHOSTTY_KITTY_GRAPHICS_PLACEMENT_DATA_IS_VIRTUAL = 3,
        GHOSTTY_KITTY_GRAPHICS_PLACEMENT_DATA_X_OFFSET = 4,
        GHOSTTY_KITTY_GRAPHICS_PLACEMENT_DATA_Y_OFFSET = 5,
        GHOSTTY_KITTY_GRAPHICS_PLACEMENT_DATA_SOURCE_X = 6,
        GHOSTTY_KITTY_GRAPHICS_PLACEMENT_DATA_SOURCE_Y = 7,
        GHOSTTY_KITTY_GRAPHICS_PLACEMENT_DATA_SOURCE_WIDTH = 8,
        GHOSTTY_KITTY_GRAPHICS_PLACEMENT_DATA_SOURCE_HEIGHT = 9,
        GHOSTTY_KITTY_GRAPHICS_PLACEMENT_DATA_COLUMNS = 10,
        GHOSTTY_KITTY_GRAPHICS_PLACEMENT_DATA_ROWS = 11,
        GHOSTTY_KITTY_GRAPHICS_PLACEMENT_DATA_Z = 12,
        GHOSTTY_KITTY_GRAPHICS_PLACEMENT_DATA_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyKittyPlacementLayer : uint
    {
        GHOSTTY_KITTY_PLACEMENT_LAYER_ALL = 0,
        GHOSTTY_KITTY_PLACEMENT_LAYER_BELOW_BG = 1,
        GHOSTTY_KITTY_PLACEMENT_LAYER_BELOW_TEXT = 2,
        GHOSTTY_KITTY_PLACEMENT_LAYER_ABOVE_TEXT = 3,
        GHOSTTY_KITTY_PLACEMENT_LAYER_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyKittyGraphicsPlacementIteratorOption : uint
    {
        GHOSTTY_KITTY_GRAPHICS_PLACEMENT_ITERATOR_OPTION_LAYER = 0,
        GHOSTTY_KITTY_GRAPHICS_PLACEMENT_ITERATOR_OPTION_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyKittyImageFormat : uint
    {
        GHOSTTY_KITTY_IMAGE_FORMAT_RGB = 0,
        GHOSTTY_KITTY_IMAGE_FORMAT_RGBA = 1,
        GHOSTTY_KITTY_IMAGE_FORMAT_PNG = 2,
        GHOSTTY_KITTY_IMAGE_FORMAT_GRAY_ALPHA = 3,
        GHOSTTY_KITTY_IMAGE_FORMAT_GRAY = 4,
        GHOSTTY_KITTY_IMAGE_FORMAT_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyKittyImageCompression : uint
    {
        GHOSTTY_KITTY_IMAGE_COMPRESSION_NONE = 0,
        GHOSTTY_KITTY_IMAGE_COMPRESSION_ZLIB_DEFLATE = 1,
        GHOSTTY_KITTY_IMAGE_COMPRESSION_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyKittyGraphicsImageData : uint
    {
        GHOSTTY_KITTY_IMAGE_DATA_INVALID = 0,
        GHOSTTY_KITTY_IMAGE_DATA_ID = 1,
        GHOSTTY_KITTY_IMAGE_DATA_NUMBER = 2,
        GHOSTTY_KITTY_IMAGE_DATA_WIDTH = 3,
        GHOSTTY_KITTY_IMAGE_DATA_HEIGHT = 4,
        GHOSTTY_KITTY_IMAGE_DATA_FORMAT = 5,
        GHOSTTY_KITTY_IMAGE_DATA_COMPRESSION = 6,
        GHOSTTY_KITTY_IMAGE_DATA_DATA_PTR = 7,
        GHOSTTY_KITTY_IMAGE_DATA_DATA_LEN = 8,
        GHOSTTY_KITTY_IMAGE_DATA_GENERATION = 9,
        GHOSTTY_KITTY_IMAGE_DATA_MAX_VALUE = 2147483647,
    }

    public partial struct GhosttyKittyGraphicsPlacementRenderInfo
    {
        [NativeTypeName("size_t")]
        public nuint size;

        [NativeTypeName("uint32_t")]
        public uint pixel_width;

        [NativeTypeName("uint32_t")]
        public uint pixel_height;

        [NativeTypeName("uint32_t")]
        public uint grid_cols;

        [NativeTypeName("uint32_t")]
        public uint grid_rows;

        [NativeTypeName("int32_t")]
        public int viewport_col;

        [NativeTypeName("int32_t")]
        public int viewport_row;

        [NativeTypeName("_Bool")]
        public byte viewport_visible;

        [NativeTypeName("uint32_t")]
        public uint source_x;

        [NativeTypeName("uint32_t")]
        public uint source_y;

        [NativeTypeName("uint32_t")]
        public uint source_width;

        [NativeTypeName("uint32_t")]
        public uint source_height;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyTerminalCompressionMode : uint
    {
        GHOSTTY_TERMINAL_COMPRESSION_MODE_INCREMENTAL = 0,
        GHOSTTY_TERMINAL_COMPRESSION_MODE_FULL = 1,
        GHOSTTY_TERMINAL_COMPRESSION_MODE_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyTerminalCompressionResult : uint
    {
        GHOSTTY_TERMINAL_COMPRESSION_RESULT_UNSUPPORTED = 0,
        GHOSTTY_TERMINAL_COMPRESSION_RESULT_PENDING = 1,
        GHOSTTY_TERMINAL_COMPRESSION_RESULT_COMPLETE = 2,
        GHOSTTY_TERMINAL_COMPRESSION_RESULT_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyTerminalScrollViewportTag : uint
    {
        GHOSTTY_SCROLL_VIEWPORT_TOP,
        GHOSTTY_SCROLL_VIEWPORT_BOTTOM,
        GHOSTTY_SCROLL_VIEWPORT_DELTA,
        GHOSTTY_SCROLL_VIEWPORT_ROW,
        GHOSTTY_SCROLL_VIEWPORT_MAX_VALUE = 2147483647,
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct GhosttyTerminalScrollViewportValue
    {
        [FieldOffset(0)]
        [NativeTypeName("intptr_t")]
        public nint delta;

        [FieldOffset(0)]
        [NativeTypeName("size_t")]
        public nuint row;

        [FieldOffset(0)]
        [NativeTypeName("uint64_t[2]")]
        public __padding_e__FixedBuffer _padding;

        [InlineArray(2)]
        public partial struct __padding_e__FixedBuffer
        {
            public nuint e0;
        }
    }

    public partial struct GhosttyTerminalScrollViewport
    {
        public GhosttyTerminalScrollViewportTag tag;

        public GhosttyTerminalScrollViewportValue value;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyTerminalScreen : uint
    {
        GHOSTTY_TERMINAL_SCREEN_PRIMARY = 0,
        GHOSTTY_TERMINAL_SCREEN_ALTERNATE = 1,
        GHOSTTY_TERMINAL_SCREEN_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyTerminalCursorStyle : uint
    {
        GHOSTTY_TERMINAL_CURSOR_STYLE_BAR = 0,
        GHOSTTY_TERMINAL_CURSOR_STYLE_BLOCK = 1,
        GHOSTTY_TERMINAL_CURSOR_STYLE_UNDERLINE = 2,
        GHOSTTY_TERMINAL_CURSOR_STYLE_BLOCK_HOLLOW = 3,
        GHOSTTY_TERMINAL_CURSOR_STYLE_MAX_VALUE = 2147483647,
    }

    public partial struct GhosttyTerminalScrollbar
    {
        [NativeTypeName("uint64_t")]
        public nuint total;

        [NativeTypeName("uint64_t")]
        public nuint offset;

        [NativeTypeName("uint64_t")]
        public nuint len;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyTerminalUnknownSequenceTag : uint
    {
        GHOSTTY_TERMINAL_UNKNOWN_SEQUENCE_APC = 0,
        GHOSTTY_TERMINAL_UNKNOWN_SEQUENCE_MAX_VALUE = 2147483647,
    }

    public partial struct GhosttyTerminalUnknownStringSequence
    {
        [NativeTypeName("_Bool")]
        public byte truncated;

        public GhosttyString content;
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct GhosttyTerminalUnknownSequenceValue
    {
        [FieldOffset(0)]
        public GhosttyTerminalUnknownStringSequence apc;

        [FieldOffset(0)]
        [NativeTypeName("uint64_t[16]")]
        public __padding_e__FixedBuffer _padding;

        [InlineArray(16)]
        public partial struct __padding_e__FixedBuffer
        {
            public nuint e0;
        }
    }

    public partial struct GhosttyTerminalUnknownSequence
    {
        public GhosttyTerminalUnknownSequenceTag tag;

        public GhosttyTerminalUnknownSequenceValue value;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyClipboardLocation : uint
    {
        GHOSTTY_CLIPBOARD_LOCATION_STANDARD = 0,
        GHOSTTY_CLIPBOARD_LOCATION_SELECTION = 1,
        GHOSTTY_CLIPBOARD_LOCATION_PRIMARY = 2,
        GHOSTTY_CLIPBOARD_LOCATION_MAX_VALUE = 2147483647,
    }

    public partial struct GhosttyClipboardContent
    {
        public GhosttyString mime;

        public GhosttyString data;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyClipboardWriteResult : uint
    {
        GHOSTTY_CLIPBOARD_WRITE_RESULT_SUCCESS = 0,
        GHOSTTY_CLIPBOARD_WRITE_RESULT_DENIED = 1,
        GHOSTTY_CLIPBOARD_WRITE_RESULT_UNSUPPORTED = 2,
        GHOSTTY_CLIPBOARD_WRITE_RESULT_BUSY = 3,
        GHOSTTY_CLIPBOARD_WRITE_RESULT_INVALID_DATA = 4,
        GHOSTTY_CLIPBOARD_WRITE_RESULT_IO_ERROR = 5,
        GHOSTTY_CLIPBOARD_WRITE_RESULT_MAX_VALUE = 2147483647,
    }

    public partial struct GhosttyClipboardWriteReply
    {
        [NativeTypeName("size_t")]
        public nuint size;

        public GhosttyClipboardWriteResult result;

        [NativeTypeName("_Bool")]
        public byte remember;
    }

    public unsafe partial struct GhosttyClipboardWrite
    {
        [NativeTypeName("size_t")]
        public nuint size;

        public GhosttyClipboardLocation location;

        [NativeTypeName("const GhosttyClipboardContent *")]
        public GhosttyClipboardContent* contents;

        [NativeTypeName("size_t")]
        public nuint contents_len;

        public GhosttyString name;

        [NativeTypeName("_Bool")]
        public byte granted;

        [NativeTypeName("_Bool")]
        public byte can_remember;

        [NativeTypeName("const void *")]
        public void* ctx;

        [NativeTypeName("GhosttyClipboardWriteReplyFn")]
        public delegate* unmanaged[Cdecl]<GhosttyClipboardWrite*, GhosttyClipboardWriteReply*, void> reply;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyClipboardReadResult : uint
    {
        GHOSTTY_CLIPBOARD_READ_RESULT_SUCCESS = 0,
        GHOSTTY_CLIPBOARD_READ_RESULT_DENIED = 1,
        GHOSTTY_CLIPBOARD_READ_RESULT_UNSUPPORTED = 2,
        GHOSTTY_CLIPBOARD_READ_RESULT_BUSY = 3,
        GHOSTTY_CLIPBOARD_READ_RESULT_IO_ERROR = 4,
        GHOSTTY_CLIPBOARD_READ_RESULT_MAX_VALUE = 2147483647,
    }

    public unsafe partial struct GhosttyClipboardReadReply
    {
        [NativeTypeName("size_t")]
        public nuint size;

        public GhosttyClipboardReadResult result;

        [NativeTypeName("const GhosttyClipboardContent *")]
        public GhosttyClipboardContent* contents;

        [NativeTypeName("size_t")]
        public nuint contents_len;

        [NativeTypeName("const GhosttyString *")]
        public GhosttyString* available;

        [NativeTypeName("size_t")]
        public nuint available_len;

        [NativeTypeName("_Bool")]
        public byte remember;
    }

    public unsafe partial struct GhosttyClipboardRead
    {
        [NativeTypeName("size_t")]
        public nuint size;

        public GhosttyClipboardLocation location;

        [NativeTypeName("const GhosttyString *")]
        public GhosttyString* mimes;

        [NativeTypeName("size_t")]
        public nuint mimes_len;

        [NativeTypeName("_Bool")]
        public byte list;

        public GhosttyString name;

        [NativeTypeName("_Bool")]
        public byte granted;

        [NativeTypeName("_Bool")]
        public byte can_remember;

        [NativeTypeName("const void *")]
        public void* ctx;

        [NativeTypeName("GhosttyClipboardReadReplyFn")]
        public delegate* unmanaged[Cdecl]<GhosttyClipboardRead*, GhosttyClipboardReadReply*, void> reply;
    }

    public partial struct GhosttyTerminalDesktopNotification
    {
        [NativeTypeName("size_t")]
        public nuint size;

        public GhosttyString title;

        public GhosttyString body;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyTerminalProgressState : uint
    {
        GHOSTTY_TERMINAL_PROGRESS_STATE_REMOVE = 0,
        GHOSTTY_TERMINAL_PROGRESS_STATE_SET = 1,
        GHOSTTY_TERMINAL_PROGRESS_STATE_ERROR = 2,
        GHOSTTY_TERMINAL_PROGRESS_STATE_INDETERMINATE = 3,
        GHOSTTY_TERMINAL_PROGRESS_STATE_PAUSE = 4,
        GHOSTTY_TERMINAL_PROGRESS_STATE_MAX_VALUE = 2147483647,
    }

    public partial struct GhosttyTerminalProgressReport
    {
        [NativeTypeName("size_t")]
        public nuint size;

        public GhosttyTerminalProgressState state;

        [NativeTypeName("int8_t")]
        public sbyte progress;
    }

    public partial struct GhosttyTerminalModeConfig
    {
        [NativeTypeName("GhosttyMode")]
        public ushort mode;

        [NativeTypeName("_Bool")]
        public byte value;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyTerminalOption : uint
    {
        GHOSTTY_TERMINAL_OPT_USERDATA = 0,
        GHOSTTY_TERMINAL_OPT_WRITE_PTY = 1,
        GHOSTTY_TERMINAL_OPT_BELL = 2,
        GHOSTTY_TERMINAL_OPT_ENQUIRY = 3,
        GHOSTTY_TERMINAL_OPT_XTVERSION = 4,
        GHOSTTY_TERMINAL_OPT_TITLE_CHANGED = 5,
        GHOSTTY_TERMINAL_OPT_SIZE = 6,
        GHOSTTY_TERMINAL_OPT_COLOR_SCHEME = 7,
        GHOSTTY_TERMINAL_OPT_DEVICE_ATTRIBUTES = 8,
        GHOSTTY_TERMINAL_OPT_TITLE = 9,
        GHOSTTY_TERMINAL_OPT_PWD = 10,
        GHOSTTY_TERMINAL_OPT_COLOR_FOREGROUND = 11,
        GHOSTTY_TERMINAL_OPT_COLOR_BACKGROUND = 12,
        GHOSTTY_TERMINAL_OPT_COLOR_CURSOR = 13,
        GHOSTTY_TERMINAL_OPT_COLOR_PALETTE = 14,
        GHOSTTY_TERMINAL_OPT_KITTY_IMAGE_STORAGE_LIMIT = 15,
        GHOSTTY_TERMINAL_OPT_KITTY_IMAGE_MEDIUM_FILE = 16,
        GHOSTTY_TERMINAL_OPT_KITTY_IMAGE_MEDIUM_TEMP_FILE = 17,
        GHOSTTY_TERMINAL_OPT_KITTY_IMAGE_MEDIUM_SHARED_MEM = 18,
        GHOSTTY_TERMINAL_OPT_APC_MAX_BYTES = 19,
        GHOSTTY_TERMINAL_OPT_APC_MAX_BYTES_KITTY = 20,
        GHOSTTY_TERMINAL_OPT_SELECTION = 21,
        GHOSTTY_TERMINAL_OPT_DEFAULT_CURSOR_STYLE = 22,
        GHOSTTY_TERMINAL_OPT_DEFAULT_CURSOR_BLINK = 23,
        GHOSTTY_TERMINAL_OPT_GLYPH_PROTOCOL = 24,
        GHOSTTY_TERMINAL_OPT_PWD_CHANGED = 25,
        GHOSTTY_TERMINAL_OPT_CLIPBOARD_WRITE = 26,
        GHOSTTY_TERMINAL_OPT_SCROLLBACK_MAX_BYTES = 27,
        GHOSTTY_TERMINAL_OPT_SCROLLBACK_MAX_LINES = 28,
        GHOSTTY_TERMINAL_OPT_DESKTOP_NOTIFICATION = 29,
        GHOSTTY_TERMINAL_OPT_PROGRESS_REPORT = 30,
        GHOSTTY_TERMINAL_OPT_CONTINUATION_MAX_BYTES = 31,
        GHOSTTY_TERMINAL_OPT_TITLE_REPORT = 32,
        GHOSTTY_TERMINAL_OPT_MODE_DEFAULT = 33,
        GHOSTTY_TERMINAL_OPT_MODE = 34,
        GHOSTTY_TERMINAL_OPT_UNKNOWN_SEQUENCE = 35,
        GHOSTTY_TERMINAL_OPT_UNKNOWN_MAX_BYTES = 36,
        GHOSTTY_TERMINAL_OPT_TERMINFO_NAME = 37,
        GHOSTTY_TERMINAL_OPT_CLIPBOARD_READ = 38,
        GHOSTTY_TERMINAL_OPT_CLIPBOARD_WRITE_MAX_BYTES = 39,
        GHOSTTY_TERMINAL_OPT_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyTerminalData : uint
    {
        GHOSTTY_TERMINAL_DATA_INVALID = 0,
        GHOSTTY_TERMINAL_DATA_COLS = 1,
        GHOSTTY_TERMINAL_DATA_ROWS = 2,
        GHOSTTY_TERMINAL_DATA_CURSOR_X = 3,
        GHOSTTY_TERMINAL_DATA_CURSOR_Y = 4,
        GHOSTTY_TERMINAL_DATA_CURSOR_PENDING_WRAP = 5,
        GHOSTTY_TERMINAL_DATA_ACTIVE_SCREEN = 6,
        GHOSTTY_TERMINAL_DATA_CURSOR_VISIBLE = 7,
        GHOSTTY_TERMINAL_DATA_KITTY_KEYBOARD_FLAGS = 8,
        GHOSTTY_TERMINAL_DATA_SCROLLBAR = 9,
        GHOSTTY_TERMINAL_DATA_CURSOR_STYLE = 10,
        GHOSTTY_TERMINAL_DATA_MOUSE_TRACKING = 11,
        GHOSTTY_TERMINAL_DATA_TITLE = 12,
        GHOSTTY_TERMINAL_DATA_PWD = 13,
        GHOSTTY_TERMINAL_DATA_TOTAL_ROWS = 14,
        GHOSTTY_TERMINAL_DATA_SCROLLBACK_ROWS = 15,
        GHOSTTY_TERMINAL_DATA_WIDTH_PX = 16,
        GHOSTTY_TERMINAL_DATA_HEIGHT_PX = 17,
        GHOSTTY_TERMINAL_DATA_COLOR_FOREGROUND = 18,
        GHOSTTY_TERMINAL_DATA_COLOR_BACKGROUND = 19,
        GHOSTTY_TERMINAL_DATA_COLOR_CURSOR = 20,
        GHOSTTY_TERMINAL_DATA_COLOR_PALETTE = 21,
        GHOSTTY_TERMINAL_DATA_COLOR_FOREGROUND_DEFAULT = 22,
        GHOSTTY_TERMINAL_DATA_COLOR_BACKGROUND_DEFAULT = 23,
        GHOSTTY_TERMINAL_DATA_COLOR_CURSOR_DEFAULT = 24,
        GHOSTTY_TERMINAL_DATA_COLOR_PALETTE_DEFAULT = 25,
        GHOSTTY_TERMINAL_DATA_KITTY_IMAGE_STORAGE_LIMIT = 26,
        GHOSTTY_TERMINAL_DATA_KITTY_IMAGE_MEDIUM_FILE = 27,
        GHOSTTY_TERMINAL_DATA_KITTY_IMAGE_MEDIUM_TEMP_FILE = 28,
        GHOSTTY_TERMINAL_DATA_KITTY_IMAGE_MEDIUM_SHARED_MEM = 29,
        GHOSTTY_TERMINAL_DATA_KITTY_GRAPHICS = 30,
        GHOSTTY_TERMINAL_DATA_SELECTION = 31,
        GHOSTTY_TERMINAL_DATA_VIEWPORT_ACTIVE = 32,
        GHOSTTY_TERMINAL_DATA_VT_PROCESSING_ERROR = 33,
        GHOSTTY_TERMINAL_DATA_SCROLLBACK_MAX_BYTES = 34,
        GHOSTTY_TERMINAL_DATA_SCROLLBACK_MAX_LINES = 35,
        GHOSTTY_TERMINAL_DATA_CONTINUATION_MAX_BYTES = 36,
        GHOSTTY_TERMINAL_DATA_MODE = 37,
        GHOSTTY_TERMINAL_DATA_VT_GROUND = 38,
        GHOSTTY_TERMINAL_DATA_CURSOR_AT_PROMPT = 39,
        GHOSTTY_TERMINAL_DATA_CLIPBOARD_WRITE_MAX_BYTES = 40,
        GHOSTTY_TERMINAL_DATA_MAX_VALUE = 2147483647,
    }

    public partial struct GhosttyFormatterScreenExtra
    {
        [NativeTypeName("size_t")]
        public nuint size;

        [NativeTypeName("_Bool")]
        public byte cursor;

        [NativeTypeName("_Bool")]
        public byte style;

        [NativeTypeName("_Bool")]
        public byte hyperlink;

        [NativeTypeName("_Bool")]
        public byte protection;

        [NativeTypeName("_Bool")]
        public byte kitty_keyboard;

        [NativeTypeName("_Bool")]
        public byte charsets;
    }

    public partial struct GhosttyFormatterTerminalExtra
    {
        [NativeTypeName("size_t")]
        public nuint size;

        [NativeTypeName("_Bool")]
        public byte palette;

        [NativeTypeName("_Bool")]
        public byte modes;

        [NativeTypeName("_Bool")]
        public byte scrolling_region;

        [NativeTypeName("_Bool")]
        public byte tabstops;

        [NativeTypeName("_Bool")]
        public byte pwd;

        [NativeTypeName("_Bool")]
        public byte keyboard;

        public GhosttyFormatterScreenExtra screen;
    }

    public unsafe partial struct GhosttyFormatterTerminalOptions
    {
        [NativeTypeName("size_t")]
        public nuint size;

        public GhosttyFormatterFormat emit;

        [NativeTypeName("_Bool")]
        public byte unwrap;

        [NativeTypeName("_Bool")]
        public byte trim;

        public GhosttyFormatterTerminalExtra extra;

        [NativeTypeName("const GhosttySelection *")]
        public GhosttySelection* selection;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyRenderStateDirty : uint
    {
        GHOSTTY_RENDER_STATE_DIRTY_FALSE = 0,
        GHOSTTY_RENDER_STATE_DIRTY_PARTIAL = 1,
        GHOSTTY_RENDER_STATE_DIRTY_FULL = 2,
        GHOSTTY_RENDER_STATE_DIRTY_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyRenderStateCursorVisualStyle : uint
    {
        GHOSTTY_RENDER_STATE_CURSOR_VISUAL_STYLE_BAR = 0,
        GHOSTTY_RENDER_STATE_CURSOR_VISUAL_STYLE_BLOCK = 1,
        GHOSTTY_RENDER_STATE_CURSOR_VISUAL_STYLE_UNDERLINE = 2,
        GHOSTTY_RENDER_STATE_CURSOR_VISUAL_STYLE_BLOCK_HOLLOW = 3,
        GHOSTTY_RENDER_STATE_CURSOR_VISUAL_STYLE_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyRenderStateData : uint
    {
        GHOSTTY_RENDER_STATE_DATA_INVALID = 0,
        GHOSTTY_RENDER_STATE_DATA_COLS = 1,
        GHOSTTY_RENDER_STATE_DATA_ROWS = 2,
        GHOSTTY_RENDER_STATE_DATA_DIRTY = 3,
        GHOSTTY_RENDER_STATE_DATA_ROW_ITERATOR = 4,
        GHOSTTY_RENDER_STATE_DATA_COLOR_BACKGROUND = 5,
        GHOSTTY_RENDER_STATE_DATA_COLOR_FOREGROUND = 6,
        GHOSTTY_RENDER_STATE_DATA_COLOR_CURSOR = 7,
        GHOSTTY_RENDER_STATE_DATA_COLOR_CURSOR_HAS_VALUE = 8,
        GHOSTTY_RENDER_STATE_DATA_COLOR_PALETTE = 9,
        GHOSTTY_RENDER_STATE_DATA_CURSOR_VISUAL_STYLE = 10,
        GHOSTTY_RENDER_STATE_DATA_CURSOR_VISIBLE = 11,
        GHOSTTY_RENDER_STATE_DATA_CURSOR_BLINKING = 12,
        GHOSTTY_RENDER_STATE_DATA_CURSOR_PASSWORD_INPUT = 13,
        GHOSTTY_RENDER_STATE_DATA_CURSOR_VIEWPORT_HAS_VALUE = 14,
        GHOSTTY_RENDER_STATE_DATA_CURSOR_VIEWPORT_X = 15,
        GHOSTTY_RENDER_STATE_DATA_CURSOR_VIEWPORT_Y = 16,
        GHOSTTY_RENDER_STATE_DATA_CURSOR_VIEWPORT_WIDE_TAIL = 17,
        GHOSTTY_RENDER_STATE_DATA_CURSOR = 18,
        GHOSTTY_RENDER_STATE_DATA_COLORS = 19,
        GHOSTTY_RENDER_STATE_DATA_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyRenderStateOption : uint
    {
        GHOSTTY_RENDER_STATE_OPTION_DIRTY = 0,
        GHOSTTY_RENDER_STATE_OPTION_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyRenderStateRowData : uint
    {
        GHOSTTY_RENDER_STATE_ROW_DATA_INVALID = 0,
        GHOSTTY_RENDER_STATE_ROW_DATA_DIRTY = 1,
        GHOSTTY_RENDER_STATE_ROW_DATA_RAW = 2,
        GHOSTTY_RENDER_STATE_ROW_DATA_CELLS = 3,
        GHOSTTY_RENDER_STATE_ROW_DATA_SELECTION = 4,
        GHOSTTY_RENDER_STATE_ROW_DATA_CELLS_RAW = 5,
        GHOSTTY_RENDER_STATE_ROW_DATA_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyRenderStateRowOption : uint
    {
        GHOSTTY_RENDER_STATE_ROW_OPTION_DIRTY = 0,
        GHOSTTY_RENDER_STATE_ROW_OPTION_MAX_VALUE = 2147483647,
    }

    public partial struct GhosttyRenderStateRowSelection
    {
        [NativeTypeName("size_t")]
        public nuint size;

        [NativeTypeName("uint16_t")]
        public ushort start_x;

        [NativeTypeName("uint16_t")]
        public ushort end_x;
    }

    public partial struct GhosttyRenderStateCursor
    {
        [NativeTypeName("size_t")]
        public nuint size;

        [NativeTypeName("_Bool")]
        public byte viewport_has_value;

        [NativeTypeName("uint16_t")]
        public ushort viewport_x;

        [NativeTypeName("uint16_t")]
        public ushort viewport_y;

        [NativeTypeName("_Bool")]
        public byte wide_tail;

        [NativeTypeName("_Bool")]
        public byte visible;

        [NativeTypeName("_Bool")]
        public byte blinking;

        [NativeTypeName("_Bool")]
        public byte password_input;

        public GhosttyRenderStateCursorVisualStyle visual_style;
    }

    public partial struct GhosttyRenderStateColors
    {
        [NativeTypeName("size_t")]
        public nuint size;

        public GhosttyColorRgb background;

        public GhosttyColorRgb foreground;

        public GhosttyColorRgb cursor;

        [NativeTypeName("_Bool")]
        public byte cursor_has_value;

        [NativeTypeName("GhosttyColorRgb[256]")]
        public _palette_e__FixedBuffer palette;

        [InlineArray(256)]
        public partial struct _palette_e__FixedBuffer
        {
            public GhosttyColorRgb e0;
        }
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyRenderStateRowCellsData : uint
    {
        GHOSTTY_RENDER_STATE_ROW_CELLS_DATA_INVALID = 0,
        GHOSTTY_RENDER_STATE_ROW_CELLS_DATA_RAW = 1,
        GHOSTTY_RENDER_STATE_ROW_CELLS_DATA_STYLE = 2,
        GHOSTTY_RENDER_STATE_ROW_CELLS_DATA_GRAPHEMES_LEN = 3,
        GHOSTTY_RENDER_STATE_ROW_CELLS_DATA_GRAPHEMES_BUF = 4,
        GHOSTTY_RENDER_STATE_ROW_CELLS_DATA_BG_COLOR = 5,
        GHOSTTY_RENDER_STATE_ROW_CELLS_DATA_FG_COLOR = 6,
        GHOSTTY_RENDER_STATE_ROW_CELLS_DATA_SELECTED = 7,
        GHOSTTY_RENDER_STATE_ROW_CELLS_DATA_HAS_STYLING = 8,
        GHOSTTY_RENDER_STATE_ROW_CELLS_DATA_GRAPHEMES_UTF8 = 9,
        GHOSTTY_RENDER_STATE_ROW_CELLS_DATA_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyOscCommandType : uint
    {
        GHOSTTY_OSC_COMMAND_INVALID = 0,
        GHOSTTY_OSC_COMMAND_CHANGE_WINDOW_TITLE = 1,
        GHOSTTY_OSC_COMMAND_CHANGE_WINDOW_ICON = 2,
        GHOSTTY_OSC_COMMAND_SEMANTIC_PROMPT = 3,
        GHOSTTY_OSC_COMMAND_CLIPBOARD_CONTENTS = 4,
        GHOSTTY_OSC_COMMAND_REPORT_PWD = 5,
        GHOSTTY_OSC_COMMAND_MOUSE_SHAPE = 6,
        GHOSTTY_OSC_COMMAND_COLOR_OPERATION = 7,
        GHOSTTY_OSC_COMMAND_KITTY_COLOR_PROTOCOL = 8,
        GHOSTTY_OSC_COMMAND_SHOW_DESKTOP_NOTIFICATION = 9,
        GHOSTTY_OSC_COMMAND_HYPERLINK_START = 10,
        GHOSTTY_OSC_COMMAND_HYPERLINK_END = 11,
        GHOSTTY_OSC_COMMAND_CONEMU_SLEEP = 12,
        GHOSTTY_OSC_COMMAND_CONEMU_SHOW_MESSAGE_BOX = 13,
        GHOSTTY_OSC_COMMAND_CONEMU_CHANGE_TAB_TITLE = 14,
        GHOSTTY_OSC_COMMAND_CONEMU_PROGRESS_REPORT = 15,
        GHOSTTY_OSC_COMMAND_CONEMU_WAIT_INPUT = 16,
        GHOSTTY_OSC_COMMAND_CONEMU_GUIMACRO = 17,
        GHOSTTY_OSC_COMMAND_CONEMU_RUN_PROCESS = 18,
        GHOSTTY_OSC_COMMAND_CONEMU_OUTPUT_ENVIRONMENT_VARIABLE = 19,
        GHOSTTY_OSC_COMMAND_CONEMU_XTERM_EMULATION = 20,
        GHOSTTY_OSC_COMMAND_CONEMU_COMMENT = 21,
        GHOSTTY_OSC_COMMAND_KITTY_TEXT_SIZING = 22,
        GHOSTTY_OSC_COMMAND_KITTY_CLIPBOARD_PROTOCOL = 23,
        GHOSTTY_OSC_COMMAND_KITTY_DND_PROTOCOL = 24,
        GHOSTTY_OSC_COMMAND_CONTEXT_SIGNAL = 25,
        GHOSTTY_OSC_COMMAND_KITTY_DESKTOP_NOTIFICATION = 26,
        GHOSTTY_OSC_COMMAND_TYPE_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyOscCommandData : uint
    {
        GHOSTTY_OSC_DATA_INVALID = 0,
        GHOSTTY_OSC_DATA_CHANGE_WINDOW_TITLE_STR = 1,
        GHOSTTY_OSC_DATA_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttySgrAttributeTag : uint
    {
        GHOSTTY_SGR_ATTR_UNSET = 0,
        GHOSTTY_SGR_ATTR_UNKNOWN = 1,
        GHOSTTY_SGR_ATTR_BOLD = 2,
        GHOSTTY_SGR_ATTR_RESET_BOLD = 3,
        GHOSTTY_SGR_ATTR_ITALIC = 4,
        GHOSTTY_SGR_ATTR_RESET_ITALIC = 5,
        GHOSTTY_SGR_ATTR_FAINT = 6,
        GHOSTTY_SGR_ATTR_UNDERLINE = 7,
        GHOSTTY_SGR_ATTR_UNDERLINE_COLOR = 8,
        GHOSTTY_SGR_ATTR_UNDERLINE_COLOR_256 = 9,
        GHOSTTY_SGR_ATTR_RESET_UNDERLINE_COLOR = 10,
        GHOSTTY_SGR_ATTR_OVERLINE = 11,
        GHOSTTY_SGR_ATTR_RESET_OVERLINE = 12,
        GHOSTTY_SGR_ATTR_BLINK = 13,
        GHOSTTY_SGR_ATTR_RESET_BLINK = 14,
        GHOSTTY_SGR_ATTR_INVERSE = 15,
        GHOSTTY_SGR_ATTR_RESET_INVERSE = 16,
        GHOSTTY_SGR_ATTR_INVISIBLE = 17,
        GHOSTTY_SGR_ATTR_RESET_INVISIBLE = 18,
        GHOSTTY_SGR_ATTR_STRIKETHROUGH = 19,
        GHOSTTY_SGR_ATTR_RESET_STRIKETHROUGH = 20,
        GHOSTTY_SGR_ATTR_DIRECT_COLOR_FG = 21,
        GHOSTTY_SGR_ATTR_DIRECT_COLOR_BG = 22,
        GHOSTTY_SGR_ATTR_BG_8 = 23,
        GHOSTTY_SGR_ATTR_FG_8 = 24,
        GHOSTTY_SGR_ATTR_RESET_FG = 25,
        GHOSTTY_SGR_ATTR_RESET_BG = 26,
        GHOSTTY_SGR_ATTR_BRIGHT_BG_8 = 27,
        GHOSTTY_SGR_ATTR_BRIGHT_FG_8 = 28,
        GHOSTTY_SGR_ATTR_BG_256 = 29,
        GHOSTTY_SGR_ATTR_FG_256 = 30,
        GHOSTTY_SGR_ATTR_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttySgrUnderline : uint
    {
        GHOSTTY_SGR_UNDERLINE_NONE = 0,
        GHOSTTY_SGR_UNDERLINE_SINGLE = 1,
        GHOSTTY_SGR_UNDERLINE_DOUBLE = 2,
        GHOSTTY_SGR_UNDERLINE_CURLY = 3,
        GHOSTTY_SGR_UNDERLINE_DOTTED = 4,
        GHOSTTY_SGR_UNDERLINE_DASHED = 5,
        GHOSTTY_SGR_UNDERLINE_MAX_VALUE = 2147483647,
    }

    public unsafe partial struct GhosttySgrUnknown
    {
        [NativeTypeName("const uint16_t *")]
        public ushort* full_ptr;

        [NativeTypeName("size_t")]
        public nuint full_len;

        [NativeTypeName("const uint16_t *")]
        public ushort* partial_ptr;

        [NativeTypeName("size_t")]
        public nuint partial_len;
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct GhosttySgrAttributeValue
    {
        [FieldOffset(0)]
        public GhosttySgrUnknown unknown;

        [FieldOffset(0)]
        public GhosttySgrUnderline underline;

        [FieldOffset(0)]
        public GhosttyColorRgb underline_color;

        [FieldOffset(0)]
        [NativeTypeName("GhosttyColorPaletteIndex")]
        public byte underline_color_256;

        [FieldOffset(0)]
        public GhosttyColorRgb direct_color_fg;

        [FieldOffset(0)]
        public GhosttyColorRgb direct_color_bg;

        [FieldOffset(0)]
        [NativeTypeName("GhosttyColorPaletteIndex")]
        public byte bg_8;

        [FieldOffset(0)]
        [NativeTypeName("GhosttyColorPaletteIndex")]
        public byte fg_8;

        [FieldOffset(0)]
        [NativeTypeName("GhosttyColorPaletteIndex")]
        public byte bright_bg_8;

        [FieldOffset(0)]
        [NativeTypeName("GhosttyColorPaletteIndex")]
        public byte bright_fg_8;

        [FieldOffset(0)]
        [NativeTypeName("GhosttyColorPaletteIndex")]
        public byte bg_256;

        [FieldOffset(0)]
        [NativeTypeName("GhosttyColorPaletteIndex")]
        public byte fg_256;

        [FieldOffset(0)]
        [NativeTypeName("uint64_t[8]")]
        public __padding_e__FixedBuffer _padding;

        [InlineArray(8)]
        public partial struct __padding_e__FixedBuffer
        {
            public nuint e0;
        }
    }

    public partial struct GhosttySgrAttribute
    {
        public GhosttySgrAttributeTag tag;

        public GhosttySgrAttributeValue value;
    }

    public unsafe partial struct GhosttySysImage
    {
        [NativeTypeName("uint32_t")]
        public uint width;

        [NativeTypeName("uint32_t")]
        public uint height;

        [NativeTypeName("uint8_t *")]
        public byte* data;

        [NativeTypeName("size_t")]
        public nuint data_len;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttySysLogLevel : uint
    {
        GHOSTTY_SYS_LOG_LEVEL_ERROR = 0,
        GHOSTTY_SYS_LOG_LEVEL_WARNING = 1,
        GHOSTTY_SYS_LOG_LEVEL_INFO = 2,
        GHOSTTY_SYS_LOG_LEVEL_DEBUG = 3,
        GHOSTTY_SYS_LOG_LEVEL_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttySysOption : uint
    {
        GHOSTTY_SYS_OPT_USERDATA = 0,
        GHOSTTY_SYS_OPT_DECODE_PNG = 1,
        GHOSTTY_SYS_OPT_LOG = 2,
        GHOSTTY_SYS_OPT_RANDOM_SECURE = 3,
        GHOSTTY_SYS_OPT_MAX_VALUE = 2147483647,
    }

    public partial struct GhosttyKeyEventImpl
    {
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyKeyAction : uint
    {
        GHOSTTY_KEY_ACTION_RELEASE = 0,
        GHOSTTY_KEY_ACTION_PRESS = 1,
        GHOSTTY_KEY_ACTION_REPEAT = 2,
        GHOSTTY_KEY_ACTION_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyKey : uint
    {
        GHOSTTY_KEY_UNIDENTIFIED = 0,
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
        GHOSTTY_KEY_MAX_VALUE = 2147483647,
    }

    public partial struct GhosttyKeyEncoderImpl
    {
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyOptionAsAlt : uint
    {
        GHOSTTY_OPTION_AS_ALT_FALSE = 0,
        GHOSTTY_OPTION_AS_ALT_TRUE = 1,
        GHOSTTY_OPTION_AS_ALT_LEFT = 2,
        GHOSTTY_OPTION_AS_ALT_RIGHT = 3,
        GHOSTTY_OPTION_AS_ALT_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyKeyEncoderOption : uint
    {
        GHOSTTY_KEY_ENCODER_OPT_CURSOR_KEY_APPLICATION = 0,
        GHOSTTY_KEY_ENCODER_OPT_KEYPAD_KEY_APPLICATION = 1,
        GHOSTTY_KEY_ENCODER_OPT_IGNORE_KEYPAD_WITH_NUMLOCK = 2,
        GHOSTTY_KEY_ENCODER_OPT_ALT_ESC_PREFIX = 3,
        GHOSTTY_KEY_ENCODER_OPT_MODIFY_OTHER_KEYS_STATE_2 = 4,
        GHOSTTY_KEY_ENCODER_OPT_KITTY_FLAGS = 5,
        GHOSTTY_KEY_ENCODER_OPT_MACOS_OPTION_AS_ALT = 6,
        GHOSTTY_KEY_ENCODER_OPT_BACKARROW_KEY_MODE = 7,
        GHOSTTY_KEY_ENCODER_OPT_MAX_VALUE = 2147483647,
    }

    public partial struct GhosttyMouseEventImpl
    {
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyMouseAction : uint
    {
        GHOSTTY_MOUSE_ACTION_PRESS = 0,
        GHOSTTY_MOUSE_ACTION_RELEASE = 1,
        GHOSTTY_MOUSE_ACTION_MOTION = 2,
        GHOSTTY_MOUSE_ACTION_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyMouseButton : uint
    {
        GHOSTTY_MOUSE_BUTTON_UNKNOWN = 0,
        GHOSTTY_MOUSE_BUTTON_LEFT = 1,
        GHOSTTY_MOUSE_BUTTON_RIGHT = 2,
        GHOSTTY_MOUSE_BUTTON_MIDDLE = 3,
        GHOSTTY_MOUSE_BUTTON_FOUR = 4,
        GHOSTTY_MOUSE_BUTTON_FIVE = 5,
        GHOSTTY_MOUSE_BUTTON_SIX = 6,
        GHOSTTY_MOUSE_BUTTON_SEVEN = 7,
        GHOSTTY_MOUSE_BUTTON_EIGHT = 8,
        GHOSTTY_MOUSE_BUTTON_NINE = 9,
        GHOSTTY_MOUSE_BUTTON_TEN = 10,
        GHOSTTY_MOUSE_BUTTON_ELEVEN = 11,
        GHOSTTY_MOUSE_BUTTON_MAX_VALUE = 2147483647,
    }

    public partial struct GhosttyMousePosition
    {
        public float x;

        public float y;
    }

    public partial struct GhosttyMouseEncoderImpl
    {
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyMouseTrackingMode : uint
    {
        GHOSTTY_MOUSE_TRACKING_NONE = 0,
        GHOSTTY_MOUSE_TRACKING_X10 = 1,
        GHOSTTY_MOUSE_TRACKING_NORMAL = 2,
        GHOSTTY_MOUSE_TRACKING_BUTTON = 3,
        GHOSTTY_MOUSE_TRACKING_ANY = 4,
        GHOSTTY_MOUSE_TRACKING_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyMouseFormat : uint
    {
        GHOSTTY_MOUSE_FORMAT_X10 = 0,
        GHOSTTY_MOUSE_FORMAT_UTF8 = 1,
        GHOSTTY_MOUSE_FORMAT_SGR = 2,
        GHOSTTY_MOUSE_FORMAT_URXVT = 3,
        GHOSTTY_MOUSE_FORMAT_SGR_PIXELS = 4,
        GHOSTTY_MOUSE_FORMAT_MAX_VALUE = 2147483647,
    }

    public partial struct GhosttyMouseEncoderSize
    {
        [NativeTypeName("size_t")]
        public nuint size;

        [NativeTypeName("uint32_t")]
        public uint screen_width;

        [NativeTypeName("uint32_t")]
        public uint screen_height;

        [NativeTypeName("uint32_t")]
        public uint cell_width;

        [NativeTypeName("uint32_t")]
        public uint cell_height;

        [NativeTypeName("uint32_t")]
        public uint padding_top;

        [NativeTypeName("uint32_t")]
        public uint padding_bottom;

        [NativeTypeName("uint32_t")]
        public uint padding_right;

        [NativeTypeName("uint32_t")]
        public uint padding_left;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyMouseEncoderOption : uint
    {
        GHOSTTY_MOUSE_ENCODER_OPT_EVENT = 0,
        GHOSTTY_MOUSE_ENCODER_OPT_FORMAT = 1,
        GHOSTTY_MOUSE_ENCODER_OPT_SIZE = 2,
        GHOSTTY_MOUSE_ENCODER_OPT_ANY_BUTTON_PRESSED = 3,
        GHOSTTY_MOUSE_ENCODER_OPT_TRACK_LAST_CELL = 4,
        GHOSTTY_MOUSE_ENCODER_OPT_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttyPasteSource : uint
    {
        GHOSTTY_PASTE_SOURCE_CLIPBOARD = 0,
        GHOSTTY_PASTE_SOURCE_TEXT = 1,
        GHOSTTY_PASTE_SOURCE_MAX_VALUE = 2147483647,
    }

    public unsafe partial struct GhosttyPaste
    {
        [NativeTypeName("size_t")]
        public nuint size;

        public GhosttyClipboardLocation location;

        public GhosttyPasteSource source;

        [NativeTypeName("const GhosttyString *")]
        public GhosttyString* mimes;

        [NativeTypeName("size_t")]
        public nuint mimes_len;

        public GhosttyMimeReader reader;

        [NativeTypeName("_Bool")]
        public byte allow_unsafe;
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttySearchStatus : uint
    {
        GHOSTTY_SEARCH_STATUS_RUNNING = 0,
        GHOSTTY_SEARCH_STATUS_FEED_REQUIRED = 1,
        GHOSTTY_SEARCH_STATUS_COMPLETE = 2,
        GHOSTTY_SEARCH_STATUS_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttySearchScroll : uint
    {
        GHOSTTY_SEARCH_SCROLL_IF_NEEDED = 0,
        GHOSTTY_SEARCH_SCROLL_NONE = 1,
        GHOSTTY_SEARCH_SCROLL_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttySearchData : uint
    {
        GHOSTTY_SEARCH_DATA_STATUS = 0,
        GHOSTTY_SEARCH_DATA_NEEDLE = 1,
        GHOSTTY_SEARCH_DATA_TOTAL_MATCHES = 2,
        GHOSTTY_SEARCH_DATA_SELECTED_INDEX = 3,
        GHOSTTY_SEARCH_DATA_SELECTED_MATCH = 4,
        GHOSTTY_SEARCH_DATA_MATCHES = 5,
        GHOSTTY_SEARCH_DATA_VIEWPORT_MATCHES = 6,
        GHOSTTY_SEARCH_DATA_SELECT_SCROLL = 7,
        GHOSTTY_SEARCH_DATA_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttySearchOption : uint
    {
        GHOSTTY_SEARCH_OPT_NEEDLE = 0,
        GHOSTTY_SEARCH_OPT_SELECT_NEXT = 1,
        GHOSTTY_SEARCH_OPT_SELECT_PREV = 2,
        GHOSTTY_SEARCH_OPT_SELECT_SCROLL = 3,
        GHOSTTY_SEARCH_OPT_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttySnapshotDecoderOption : uint
    {
        GHOSTTY_SNAPSHOT_DECODER_OPT_MAX_CONTINUATION_BYTES = 0,
        GHOSTTY_SNAPSHOT_DECODER_OPT_RETAIN_CONTINUATION = 1,
        GHOSTTY_SNAPSHOT_DECODER_OPT_MAX_VALUE = 2147483647,
    }

    [NativeTypeName("unsigned int")]
    public enum GhosttySnapshotDecoderData : uint
    {
        GHOSTTY_SNAPSHOT_DECODER_DATA_INVALID = 0,
        GHOSTTY_SNAPSHOT_DECODER_DATA_MAX_CONTINUATION_BYTES = 1,
        GHOSTTY_SNAPSHOT_DECODER_DATA_SOURCE_OFFSET = 2,
        GHOSTTY_SNAPSHOT_DECODER_DATA_HISTORY_ROWS_PRIMARY = 3,
        GHOSTTY_SNAPSHOT_DECODER_DATA_HISTORY_ROWS_ALTERNATE = 4,
        GHOSTTY_SNAPSHOT_DECODER_DATA_PROGRESS_SCREEN = 5,
        GHOSTTY_SNAPSHOT_DECODER_DATA_PROGRESS_ROWS = 6,
        GHOSTTY_SNAPSHOT_DECODER_DATA_PROGRESS_REMAINING = 7,
        GHOSTTY_SNAPSHOT_DECODER_DATA_RETAIN_CONTINUATION = 8,
        GHOSTTY_SNAPSHOT_DECODER_DATA_MAX_VALUE = 2147483647,
    }
using System;
using System.Diagnostics;

namespace Ghostty.Vt.Native
{
    /// <summary>Defines the type of a member as it was used in the native signature.</summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = true)]
    [Conditional("DEBUG")]
    internal sealed partial class NativeTypeNameAttribute : Attribute
    {
        private readonly string _name;

        /// <summary>Initializes a new instance of the <see cref="NativeTypeNameAttribute" /> class.</summary>
        /// <param name="name">The name of the type that was used in the native signature.</param>
        public NativeTypeNameAttribute(string name)
        {
            _name = name;
        }

        /// <summary>Gets the name of the type that was used in the native signature.</summary>
        public string Name => _name;
    }
}
using System;
using System.Diagnostics;

namespace Ghostty.Vt.Native
{
    /// <summary>Defines the annotation found in a native declaration.</summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    [Conditional("DEBUG")]
    internal sealed partial class NativeAnnotationAttribute : Attribute
    {
        private readonly string _annotation;

        /// <summary>Initializes a new instance of the <see cref="NativeAnnotationAttribute" /> class.</summary>
        /// <param name="annotation">The annotation that was used in the native declaration.</param>
        public NativeAnnotationAttribute(string annotation)
        {
            _annotation = annotation;
        }

        /// <summary>Gets the annotation that was used in the native declaration.</summary>
        public string Annotation => _annotation;
    }
}

    public static unsafe partial class NativeMethods
    {
        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* ghostty_type_json();

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint8_t *")]
        public static extern byte* ghostty_alloc([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("size_t")] nuint len);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_free([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("uint8_t *")] byte* ptr, [NativeTypeName("size_t")] nuint len);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_build_info(GhosttyBuildInfo data, void* @out);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_color_rgb_get([NativeTypeName("const GhosttyColorRgb *")] GhosttyColorRgb* color, [NativeTypeName("uint8_t *")] byte* r, [NativeTypeName("uint8_t *")] byte* g, [NativeTypeName("uint8_t *")] byte* b);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_color_parse_x11([NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint len, GhosttyColorRgb* @out);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_color_parse([NativeTypeName("const char *")] sbyte* value, [NativeTypeName("size_t")] nuint len, GhosttyColorRgb* @out);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_color_parse_palette_entry([NativeTypeName("const char *")] sbyte* value, [NativeTypeName("size_t")] nuint len, [NativeTypeName("uint8_t *")] byte* out_index, GhosttyColorRgb* out_rgb);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_color_palette_default(GhosttyColorRgb* @out);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_color_palette_generate([NativeTypeName("const GhosttyColorRgb *")] GhosttyColorRgb* @base, [NativeTypeName("const GhosttyColorPaletteMask *")] GhosttyColorPaletteMask* skip, [NativeTypeName("const GhosttyColorRgb *")] GhosttyColorRgb* bg, [NativeTypeName("const GhosttyColorRgb *")] GhosttyColorRgb* fg, [NativeTypeName("_Bool")] byte harmonious, GhosttyColorRgb* @out);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern double ghostty_color_luminance([NativeTypeName("const GhosttyColorRgb *")] GhosttyColorRgb* color);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern double ghostty_color_perceived_luminance([NativeTypeName("const GhosttyColorRgb *")] GhosttyColorRgb* color);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern double ghostty_color_contrast([NativeTypeName("const GhosttyColorRgb *")] GhosttyColorRgb* a, [NativeTypeName("const GhosttyColorRgb *")] GhosttyColorRgb* b);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const GhosttyColorX11Entry *")]
        public static extern GhosttyColorX11Entry* ghostty_color_x11_names();

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint ghostty_color_x11_name_count();

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_color_scheme_report_encode(GhosttyColorScheme scheme, [NativeTypeName("char *")] sbyte* buf, [NativeTypeName("size_t")] nuint buf_len, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_focus_encode(GhosttyFocusEvent @event, [NativeTypeName("char *")] sbyte* buf, [NativeTypeName("size_t")] nuint buf_len, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_cell_get([NativeTypeName("GhosttyCell")] nuint cell, GhosttyCellData data, void* @out);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_cell_get_multi([NativeTypeName("GhosttyCell")] nuint cell, [NativeTypeName("size_t")] nuint count, [NativeTypeName("const GhosttyCellData *")] GhosttyCellData* keys, void** values, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_row_get([NativeTypeName("GhosttyRow")] nuint row, GhosttyRowData data, void* @out);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_row_get_multi([NativeTypeName("GhosttyRow")] nuint row, [NativeTypeName("size_t")] nuint count, [NativeTypeName("const GhosttyRowData *")] GhosttyRowData* keys, void** values, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_style_default(GhosttyStyle* style);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte ghostty_style_is_default([NativeTypeName("const GhosttyStyle *")] GhosttyStyle* style);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_grid_ref_cell([NativeTypeName("const GhosttyGridRef *")] GhosttyGridRef* @ref, [NativeTypeName("GhosttyCell *")] nuint* out_cell);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_grid_ref_row([NativeTypeName("const GhosttyGridRef *")] GhosttyGridRef* @ref, [NativeTypeName("GhosttyRow *")] nuint* out_row);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_grid_ref_graphemes([NativeTypeName("const GhosttyGridRef *")] GhosttyGridRef* @ref, [NativeTypeName("uint32_t *")] uint* buf, [NativeTypeName("size_t")] nuint buf_len, [NativeTypeName("size_t *")] nuint* out_len);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_grid_ref_hyperlink_uri([NativeTypeName("const GhosttyGridRef *")] GhosttyGridRef* @ref, [NativeTypeName("uint8_t *")] byte* buf, [NativeTypeName("size_t")] nuint buf_len, [NativeTypeName("size_t *")] nuint* out_len);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_grid_ref_style([NativeTypeName("const GhosttyGridRef *")] GhosttyGridRef* @ref, GhosttyStyle* out_style);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_selection_gesture_event_new([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("GhosttySelectionGestureEvent *")] GhosttySelectionGestureEventImpl** out_event, GhosttySelectionGestureEventType type);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_selection_gesture_event_free([NativeTypeName("GhosttySelectionGestureEvent")] GhosttySelectionGestureEventImpl* @event);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_selection_gesture_event_set([NativeTypeName("GhosttySelectionGestureEvent")] GhosttySelectionGestureEventImpl* @event, GhosttySelectionGestureEventOption option, [NativeTypeName("const void *")] void* value);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_selection_gesture_event([NativeTypeName("GhosttySelectionGesture")] GhosttySelectionGestureImpl* gesture, [NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("GhosttySelectionGestureEvent")] GhosttySelectionGestureEventImpl* @event, GhosttySelection* out_selection);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_selection_gesture_new([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("GhosttySelectionGesture *")] GhosttySelectionGestureImpl** out_gesture);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_selection_gesture_free([NativeTypeName("GhosttySelectionGesture")] GhosttySelectionGestureImpl* gesture, [NativeTypeName("GhosttyTerminal")] nint terminal);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_selection_gesture_reset([NativeTypeName("GhosttySelectionGesture")] GhosttySelectionGestureImpl* gesture, [NativeTypeName("GhosttyTerminal")] nint terminal);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_selection_gesture_get([NativeTypeName("GhosttySelectionGesture")] GhosttySelectionGestureImpl* gesture, [NativeTypeName("GhosttyTerminal")] nint terminal, GhosttySelectionGestureData data, void* value);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_selection_gesture_get_multi([NativeTypeName("GhosttySelectionGesture")] GhosttySelectionGestureImpl* gesture, [NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("size_t")] nuint count, [NativeTypeName("const GhosttySelectionGestureData *")] GhosttySelectionGestureData* keys, void** values, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_select_word([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("const GhosttyTerminalSelectWordOptions *")] GhosttyTerminalSelectWordOptions* options, GhosttySelection* out_selection);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_select_word_between([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("const GhosttyTerminalSelectWordBetweenOptions *")] GhosttyTerminalSelectWordBetweenOptions* options, GhosttySelection* out_selection);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_select_line([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("const GhosttyTerminalSelectLineOptions *")] GhosttyTerminalSelectLineOptions* options, GhosttySelection* out_selection);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_select_all([NativeTypeName("GhosttyTerminal")] nint terminal, GhosttySelection* out_selection);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_select_output([NativeTypeName("GhosttyTerminal")] nint terminal, GhosttyGridRef @ref, GhosttySelection* out_selection);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_selection_format_buf([NativeTypeName("GhosttyTerminal")] nint terminal, GhosttyTerminalSelectionFormatOptions options, [NativeTypeName("uint8_t *")] byte* buf, [NativeTypeName("size_t")] nuint buf_len, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_selection_format_alloc([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, GhosttyTerminalSelectionFormatOptions options, [NativeTypeName("uint8_t **")] byte** out_ptr, [NativeTypeName("size_t *")] nuint* out_len);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_selection_adjust([NativeTypeName("GhosttyTerminal")] nint terminal, GhosttySelection* selection, GhosttySelectionAdjust adjustment);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_selection_order([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("const GhosttySelection *")] GhosttySelection* selection, GhosttySelectionOrder* out_order);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_selection_ordered([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("const GhosttySelection *")] GhosttySelection* selection, GhosttySelectionOrder desired, GhosttySelection* out_selection);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_selection_contains([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("const GhosttySelection *")] GhosttySelection* selection, GhosttyPoint point, [NativeTypeName("_Bool *")] bool* out_contains);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_selection_equal([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("const GhosttySelection *")] GhosttySelection* a, [NativeTypeName("const GhosttySelection *")] GhosttySelection* b, [NativeTypeName("_Bool *")] bool* out_equal);

        [return: NativeTypeName("GhosttyMode")]
        public static ushort ghostty_mode_new([NativeTypeName("uint16_t")] ushort value, [NativeTypeName("_Bool")] bool ansi)
        {
            return (ushort)(unchecked((value & 0x7FFF) | ((ushort)(ansi) << 15)));
        }

        [return: NativeTypeName("uint16_t")]
        public static ushort ghostty_mode_value([NativeTypeName("GhosttyMode")] ushort mode)
        {
            return mode & 0x7FFF;
        }

        [return: NativeTypeName("_Bool")]
        public static bool ghostty_mode_ansi([NativeTypeName("GhosttyMode")] ushort mode)
        {
            return ((mode >> 15) != 0) != 0;
        }

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_mode_report_encode([NativeTypeName("GhosttyMode")] ushort mode, GhosttyModeReportState state, [NativeTypeName("char *")] sbyte* buf, [NativeTypeName("size_t")] nuint buf_len, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_size_report_encode(GhosttySizeReportStyle style, GhosttySizeReportSize size, [NativeTypeName("char *")] sbyte* buf, [NativeTypeName("size_t")] nuint buf_len, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_kitty_graphics_get([NativeTypeName("GhosttyKittyGraphics")] nint graphics, GhosttyKittyGraphicsData data, void* @out);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("GhosttyKittyGraphicsImage")]
        public static extern nint ghostty_kitty_graphics_image([NativeTypeName("GhosttyKittyGraphics")] nint graphics, [NativeTypeName("uint32_t")] uint image_id);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_kitty_graphics_image_get([NativeTypeName("GhosttyKittyGraphicsImage")] nint image, GhosttyKittyGraphicsImageData data, void* @out);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_kitty_graphics_image_get_multi([NativeTypeName("GhosttyKittyGraphicsImage")] nint image, [NativeTypeName("size_t")] nuint count, [NativeTypeName("const GhosttyKittyGraphicsImageData *")] GhosttyKittyGraphicsImageData* keys, void** values, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_kitty_graphics_placement_iterator_new([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("GhosttyKittyGraphicsPlacementIterator *")] nint* out_iterator);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_kitty_graphics_placement_iterator_free([NativeTypeName("GhosttyKittyGraphicsPlacementIterator")] nint iterator);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_kitty_graphics_placement_iterator_set([NativeTypeName("GhosttyKittyGraphicsPlacementIterator")] nint iterator, GhosttyKittyGraphicsPlacementIteratorOption option, [NativeTypeName("const void *")] void* value);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte ghostty_kitty_graphics_placement_next([NativeTypeName("GhosttyKittyGraphicsPlacementIterator")] nint iterator);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_kitty_graphics_placement_get([NativeTypeName("GhosttyKittyGraphicsPlacementIterator")] nint iterator, GhosttyKittyGraphicsPlacementData data, void* @out);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_kitty_graphics_placement_get_multi([NativeTypeName("GhosttyKittyGraphicsPlacementIterator")] nint iterator, [NativeTypeName("size_t")] nuint count, [NativeTypeName("const GhosttyKittyGraphicsPlacementData *")] GhosttyKittyGraphicsPlacementData* keys, void** values, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_kitty_graphics_placement_rect([NativeTypeName("GhosttyKittyGraphicsPlacementIterator")] nint iterator, [NativeTypeName("GhosttyKittyGraphicsImage")] nint image, [NativeTypeName("GhosttyTerminal")] nint terminal, GhosttySelection* out_selection);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_kitty_graphics_placement_pixel_size([NativeTypeName("GhosttyKittyGraphicsPlacementIterator")] nint iterator, [NativeTypeName("GhosttyKittyGraphicsImage")] nint image, [NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("uint32_t *")] uint* out_width, [NativeTypeName("uint32_t *")] uint* out_height);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_kitty_graphics_placement_grid_size([NativeTypeName("GhosttyKittyGraphicsPlacementIterator")] nint iterator, [NativeTypeName("GhosttyKittyGraphicsImage")] nint image, [NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("uint32_t *")] uint* out_cols, [NativeTypeName("uint32_t *")] uint* out_rows);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_kitty_graphics_placement_viewport_pos([NativeTypeName("GhosttyKittyGraphicsPlacementIterator")] nint iterator, [NativeTypeName("GhosttyKittyGraphicsImage")] nint image, [NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("int32_t *")] int* out_col, [NativeTypeName("int32_t *")] int* out_row);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_kitty_graphics_placement_source_rect([NativeTypeName("GhosttyKittyGraphicsPlacementIterator")] nint iterator, [NativeTypeName("GhosttyKittyGraphicsImage")] nint image, [NativeTypeName("uint32_t *")] uint* out_x, [NativeTypeName("uint32_t *")] uint* out_y, [NativeTypeName("uint32_t *")] uint* out_width, [NativeTypeName("uint32_t *")] uint* out_height);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_kitty_graphics_placement_render_info([NativeTypeName("GhosttyKittyGraphicsPlacementIterator")] nint iterator, [NativeTypeName("GhosttyKittyGraphicsImage")] nint image, [NativeTypeName("GhosttyTerminal")] nint terminal, GhosttyKittyGraphicsPlacementRenderInfo* out_info);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_new([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("GhosttyTerminal *")] nint* terminal, [NativeTypeName("uint16_t")] ushort cols, [NativeTypeName("uint16_t")] ushort rows);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_terminal_free([NativeTypeName("GhosttyTerminal")] nint terminal);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_terminal_reset([NativeTypeName("GhosttyTerminal")] nint terminal);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_resize([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("uint16_t")] ushort cols, [NativeTypeName("uint16_t")] ushort rows, [NativeTypeName("uint32_t")] uint cell_width_px, [NativeTypeName("uint32_t")] uint cell_height_px);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_set([NativeTypeName("GhosttyTerminal")] nint terminal, GhosttyTerminalOption option, [NativeTypeName("const void *")] void* value);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_terminal_vt_write([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("const uint8_t *")] byte* data, [NativeTypeName("size_t")] nuint len);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_vt_write_until_ground([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("const uint8_t *")] byte* data, [NativeTypeName("size_t")] nuint len, [NativeTypeName("size_t *")] nuint* out_consumed);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_continuation_write([NativeTypeName("GhosttyTerminal")] nint terminal, GhosttyWriter writer);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_continuation_buf([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("uint8_t *")] byte* buf, [NativeTypeName("size_t")] nuint buf_len, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_continuation_alloc([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("uint8_t **")] byte** out_ptr, [NativeTypeName("size_t *")] nuint* out_len);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_terminal_scroll_viewport([NativeTypeName("GhosttyTerminal")] nint terminal, GhosttyTerminalScrollViewport behavior);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_compression_activity([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("uint64_t *")] nuint* out_activity);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_compress([NativeTypeName("GhosttyTerminal")] nint terminal, GhosttyTerminalCompressionMode mode, GhosttyTerminalCompressionResult* out_result);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_get([NativeTypeName("GhosttyTerminal")] nint terminal, GhosttyTerminalData data, void* @out);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_get_multi([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("size_t")] nuint count, [NativeTypeName("const GhosttyTerminalData *")] GhosttyTerminalData* keys, void** values, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_grid_ref([NativeTypeName("GhosttyTerminal")] nint terminal, GhosttyPoint point, GhosttyGridRef* out_ref);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_grid_ref_track([NativeTypeName("GhosttyTerminal")] nint terminal, GhosttyPoint point, [NativeTypeName("GhosttyTrackedGridRef *")] GhosttyTrackedGridRefImpl** out_ref);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_point_from_grid_ref([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("const GhosttyGridRef *")] GhosttyGridRef* @ref, GhosttyPointTag tag, GhosttyPointCoordinate* @out);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_formatter_terminal_new([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("GhosttyFormatter *")] nint* formatter, [NativeTypeName("GhosttyTerminal")] nint terminal, GhosttyFormatterTerminalOptions options);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_formatter_format([NativeTypeName("GhosttyFormatter")] nint formatter, GhosttyWriter writer);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_formatter_format_buf([NativeTypeName("GhosttyFormatter")] nint formatter, [NativeTypeName("uint8_t *")] byte* buf, [NativeTypeName("size_t")] nuint buf_len, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_formatter_format_alloc([NativeTypeName("GhosttyFormatter")] nint formatter, [NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("uint8_t **")] byte** out_ptr, [NativeTypeName("size_t *")] nuint* out_len);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_formatter_free([NativeTypeName("GhosttyFormatter")] nint formatter);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_render_state_new([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("GhosttyRenderState *")] nint* state);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_render_state_free([NativeTypeName("GhosttyRenderState")] nint state);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_render_state_update([NativeTypeName("GhosttyRenderState")] nint state, [NativeTypeName("GhosttyTerminal")] nint terminal);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_render_state_begin_update([NativeTypeName("GhosttyRenderState")] nint state, [NativeTypeName("GhosttyTerminal")] nint terminal);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_render_state_end_update([NativeTypeName("GhosttyRenderState")] nint state);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_render_state_clean([NativeTypeName("GhosttyRenderState")] nint state);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_render_state_get([NativeTypeName("GhosttyRenderState")] nint state, GhosttyRenderStateData data, void* @out);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_render_state_get_multi([NativeTypeName("GhosttyRenderState")] nint state, [NativeTypeName("size_t")] nuint count, [NativeTypeName("const GhosttyRenderStateData *")] GhosttyRenderStateData* keys, void** values, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_render_state_set([NativeTypeName("GhosttyRenderState")] nint state, GhosttyRenderStateOption option, [NativeTypeName("const void *")] void* value);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_render_state_row_iterator_new([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("GhosttyRenderStateRowIterator *")] nint* out_iterator);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_render_state_row_iterator_free([NativeTypeName("GhosttyRenderStateRowIterator")] nint iterator);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte ghostty_render_state_row_iterator_next([NativeTypeName("GhosttyRenderStateRowIterator")] nint iterator);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte ghostty_render_state_row_iterator_next_dirty([NativeTypeName("GhosttyRenderStateRowIterator")] nint iterator, [NativeTypeName("uint16_t *")] ushort* out_y);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_render_state_row_get([NativeTypeName("GhosttyRenderStateRowIterator")] nint iterator, GhosttyRenderStateRowData data, void* @out);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_render_state_row_get_multi([NativeTypeName("GhosttyRenderStateRowIterator")] nint iterator, [NativeTypeName("size_t")] nuint count, [NativeTypeName("const GhosttyRenderStateRowData *")] GhosttyRenderStateRowData* keys, void** values, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_render_state_row_set([NativeTypeName("GhosttyRenderStateRowIterator")] nint iterator, GhosttyRenderStateRowOption option, [NativeTypeName("const void *")] void* value);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_render_state_row_cells_new([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("GhosttyRenderStateRowCells *")] nint* out_cells);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte ghostty_render_state_row_cells_next([NativeTypeName("GhosttyRenderStateRowCells")] nint cells);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_render_state_row_cells_select([NativeTypeName("GhosttyRenderStateRowCells")] nint cells, [NativeTypeName("uint16_t")] ushort x);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_render_state_row_cells_get([NativeTypeName("GhosttyRenderStateRowCells")] nint cells, GhosttyRenderStateRowCellsData data, void* @out);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_render_state_row_cells_get_multi([NativeTypeName("GhosttyRenderStateRowCells")] nint cells, [NativeTypeName("size_t")] nuint count, [NativeTypeName("const GhosttyRenderStateRowCellsData *")] GhosttyRenderStateRowCellsData* keys, void** values, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_render_state_row_cells_free([NativeTypeName("GhosttyRenderStateRowCells")] nint cells);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_tracked_grid_ref_free([NativeTypeName("GhosttyTrackedGridRef")] GhosttyTrackedGridRefImpl* @ref);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte ghostty_tracked_grid_ref_has_value([NativeTypeName("GhosttyTrackedGridRef")] GhosttyTrackedGridRefImpl* @ref);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_tracked_grid_ref_point([NativeTypeName("GhosttyTrackedGridRef")] GhosttyTrackedGridRefImpl* @ref, GhosttyPointTag tag, GhosttyPointCoordinate* out_point);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_tracked_grid_ref_set([NativeTypeName("GhosttyTrackedGridRef")] GhosttyTrackedGridRefImpl* @ref, [NativeTypeName("GhosttyTerminal")] nint terminal, GhosttyPoint point);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_tracked_grid_ref_snapshot([NativeTypeName("GhosttyTrackedGridRef")] GhosttyTrackedGridRefImpl* @ref, GhosttyGridRef* out_ref);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_osc_new([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("GhosttyOscParser *")] nint* parser);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_osc_free([NativeTypeName("GhosttyOscParser")] nint parser);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_osc_reset([NativeTypeName("GhosttyOscParser")] nint parser);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_osc_next([NativeTypeName("GhosttyOscParser")] nint parser, [NativeTypeName("uint8_t")] byte @byte);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("GhosttyOscCommand")]
        public static extern GhosttyOscCommandImpl* ghostty_osc_end([NativeTypeName("GhosttyOscParser")] nint parser, [NativeTypeName("uint8_t")] byte terminator);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyOscCommandType ghostty_osc_command_type([NativeTypeName("GhosttyOscCommand")] GhosttyOscCommandImpl* command);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte ghostty_osc_command_data([NativeTypeName("GhosttyOscCommand")] GhosttyOscCommandImpl* command, GhosttyOscCommandData data, void* @out);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_sgr_new([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("GhosttySgrParser *")] nint* parser);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_sgr_free([NativeTypeName("GhosttySgrParser")] nint parser);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_sgr_reset([NativeTypeName("GhosttySgrParser")] nint parser);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_sgr_set_params([NativeTypeName("GhosttySgrParser")] nint parser, [NativeTypeName("const uint16_t *")] ushort* @params, [NativeTypeName("const char *")] sbyte* separators, [NativeTypeName("size_t")] nuint len);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte ghostty_sgr_next([NativeTypeName("GhosttySgrParser")] nint parser, GhosttySgrAttribute* attr);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint ghostty_sgr_unknown_full(GhosttySgrUnknown unknown, [NativeTypeName("const uint16_t **")] ushort** ptr);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint ghostty_sgr_unknown_partial(GhosttySgrUnknown unknown, [NativeTypeName("const uint16_t **")] ushort** ptr);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttySgrAttributeTag ghostty_sgr_attribute_tag(GhosttySgrAttribute attr);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttySgrAttributeValue* ghostty_sgr_attribute_value(GhosttySgrAttribute* attr);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_sys_set(GhosttySysOption option, [NativeTypeName("const void *")] void* value);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_sys_log_stderr(void* userdata, GhosttySysLogLevel level, [NativeTypeName("const uint8_t *")] byte* scope, [NativeTypeName("size_t")] nuint scope_len, [NativeTypeName("const uint8_t *")] byte* message, [NativeTypeName("size_t")] nuint message_len);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_key_event_new([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("GhosttyKeyEvent *")] GhosttyKeyEventImpl** @event);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_key_event_free([NativeTypeName("GhosttyKeyEvent")] GhosttyKeyEventImpl* @event);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_key_event_set_action([NativeTypeName("GhosttyKeyEvent")] GhosttyKeyEventImpl* @event, GhosttyKeyAction action);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyKeyAction ghostty_key_event_get_action([NativeTypeName("GhosttyKeyEvent")] GhosttyKeyEventImpl* @event);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_key_event_set_key([NativeTypeName("GhosttyKeyEvent")] GhosttyKeyEventImpl* @event, GhosttyKey key);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyKey ghostty_key_event_get_key([NativeTypeName("GhosttyKeyEvent")] GhosttyKeyEventImpl* @event);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_key_event_set_mods([NativeTypeName("GhosttyKeyEvent")] GhosttyKeyEventImpl* @event, [NativeTypeName("GhosttyMods")] ushort mods);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("GhosttyMods")]
        public static extern ushort ghostty_key_event_get_mods([NativeTypeName("GhosttyKeyEvent")] GhosttyKeyEventImpl* @event);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_key_event_set_consumed_mods([NativeTypeName("GhosttyKeyEvent")] GhosttyKeyEventImpl* @event, [NativeTypeName("GhosttyMods")] ushort consumed_mods);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("GhosttyMods")]
        public static extern ushort ghostty_key_event_get_consumed_mods([NativeTypeName("GhosttyKeyEvent")] GhosttyKeyEventImpl* @event);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_key_event_set_composing([NativeTypeName("GhosttyKeyEvent")] GhosttyKeyEventImpl* @event, [NativeTypeName("_Bool")] byte composing);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte ghostty_key_event_get_composing([NativeTypeName("GhosttyKeyEvent")] GhosttyKeyEventImpl* @event);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_key_event_set_utf8([NativeTypeName("GhosttyKeyEvent")] GhosttyKeyEventImpl* @event, [NativeTypeName("const char *")] sbyte* utf8, [NativeTypeName("size_t")] nuint len);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* ghostty_key_event_get_utf8([NativeTypeName("GhosttyKeyEvent")] GhosttyKeyEventImpl* @event, [NativeTypeName("size_t *")] nuint* len);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_key_event_set_unshifted_codepoint([NativeTypeName("GhosttyKeyEvent")] GhosttyKeyEventImpl* @event, [NativeTypeName("uint32_t")] uint codepoint);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint ghostty_key_event_get_unshifted_codepoint([NativeTypeName("GhosttyKeyEvent")] GhosttyKeyEventImpl* @event);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_key_encoder_new([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("GhosttyKeyEncoder *")] nint* encoder);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_key_encoder_free([NativeTypeName("GhosttyKeyEncoder")] nint encoder);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_key_encoder_setopt([NativeTypeName("GhosttyKeyEncoder")] nint encoder, GhosttyKeyEncoderOption option, [NativeTypeName("const void *")] void* value);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_key_encoder_setopt_from_terminal([NativeTypeName("GhosttyKeyEncoder")] nint encoder, [NativeTypeName("GhosttyTerminal")] nint terminal);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_key_encoder_encode([NativeTypeName("GhosttyKeyEncoder")] nint encoder, [NativeTypeName("GhosttyKeyEvent")] GhosttyKeyEventImpl* @event, [NativeTypeName("char *")] sbyte* out_buf, [NativeTypeName("size_t")] nuint out_buf_size, [NativeTypeName("size_t *")] nuint* out_len);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_mouse_event_new([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("GhosttyMouseEvent *")] GhosttyMouseEventImpl** @event);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_mouse_event_free([NativeTypeName("GhosttyMouseEvent")] GhosttyMouseEventImpl* @event);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_mouse_event_set_action([NativeTypeName("GhosttyMouseEvent")] GhosttyMouseEventImpl* @event, GhosttyMouseAction action);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyMouseAction ghostty_mouse_event_get_action([NativeTypeName("GhosttyMouseEvent")] GhosttyMouseEventImpl* @event);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_mouse_event_set_button([NativeTypeName("GhosttyMouseEvent")] GhosttyMouseEventImpl* @event, GhosttyMouseButton button);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_mouse_event_clear_button([NativeTypeName("GhosttyMouseEvent")] GhosttyMouseEventImpl* @event);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte ghostty_mouse_event_get_button([NativeTypeName("GhosttyMouseEvent")] GhosttyMouseEventImpl* @event, GhosttyMouseButton* out_button);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_mouse_event_set_mods([NativeTypeName("GhosttyMouseEvent")] GhosttyMouseEventImpl* @event, [NativeTypeName("GhosttyMods")] ushort mods);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("GhosttyMods")]
        public static extern ushort ghostty_mouse_event_get_mods([NativeTypeName("GhosttyMouseEvent")] GhosttyMouseEventImpl* @event);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_mouse_event_set_position([NativeTypeName("GhosttyMouseEvent")] GhosttyMouseEventImpl* @event, GhosttyMousePosition position);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyMousePosition ghostty_mouse_event_get_position([NativeTypeName("GhosttyMouseEvent")] GhosttyMouseEventImpl* @event);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_mouse_encoder_new([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("GhosttyMouseEncoder *")] nint* encoder);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_mouse_encoder_free([NativeTypeName("GhosttyMouseEncoder")] nint encoder);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_mouse_encoder_setopt([NativeTypeName("GhosttyMouseEncoder")] nint encoder, GhosttyMouseEncoderOption option, [NativeTypeName("const void *")] void* value);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_mouse_encoder_setopt_from_terminal([NativeTypeName("GhosttyMouseEncoder")] nint encoder, [NativeTypeName("GhosttyTerminal")] nint terminal);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_mouse_encoder_reset([NativeTypeName("GhosttyMouseEncoder")] nint encoder);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_mouse_encoder_encode([NativeTypeName("GhosttyMouseEncoder")] nint encoder, [NativeTypeName("GhosttyMouseEvent")] GhosttyMouseEventImpl* @event, [NativeTypeName("char *")] sbyte* out_buf, [NativeTypeName("size_t")] nuint out_buf_size, [NativeTypeName("size_t *")] nuint* out_len);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_terminal_paste([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("const GhosttyPaste *")] GhosttyPaste* paste, [NativeTypeName("_Bool *")] bool* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte ghostty_paste_is_safe([NativeTypeName("const char *")] sbyte* data, [NativeTypeName("size_t")] nuint len);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_paste_encode([NativeTypeName("char *")] sbyte* data, [NativeTypeName("size_t")] nuint data_len, [NativeTypeName("_Bool")] byte bracketed, [NativeTypeName("char *")] sbyte* buf, [NativeTypeName("size_t")] nuint buf_len, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_search_new([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("GhosttySearch *")] GhosttySearchImpl** out_search, [NativeTypeName("GhosttyTerminal")] nint terminal);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_search_free([NativeTypeName("GhosttySearch")] GhosttySearchImpl* search);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_search_tick([NativeTypeName("GhosttySearch")] GhosttySearchImpl* search, GhosttySearchStatus* out_status);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_search_feed([NativeTypeName("GhosttySearch")] GhosttySearchImpl* search);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_search_run([NativeTypeName("GhosttySearch")] GhosttySearchImpl* search);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_search_set([NativeTypeName("GhosttySearch")] GhosttySearchImpl* search, GhosttySearchOption option, [NativeTypeName("const void *")] void* value);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_search_get([NativeTypeName("GhosttySearch")] GhosttySearchImpl* search, GhosttySearchData data, void* value);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_search_get_multi([NativeTypeName("GhosttySearch")] GhosttySearchImpl* search, [NativeTypeName("size_t")] nuint count, [NativeTypeName("const GhosttySearchData *")] GhosttySearchData* keys, void** values, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_snapshot_encode([NativeTypeName("GhosttyTerminal")] nint terminal, GhosttyWriter writer);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_snapshot_encode_buf([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("uint8_t *")] byte* buf, [NativeTypeName("size_t")] nuint buf_len, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_snapshot_encode_alloc([NativeTypeName("GhosttyTerminal")] nint terminal, [NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("uint8_t **")] byte** out_ptr, [NativeTypeName("size_t *")] nuint* out_len);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_snapshot_decoder_new([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("GhosttySnapshotDecoder *")] GhosttySnapshotDecoderImpl** decoder, GhosttyReader reader);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_snapshot_decoder_new_buf([NativeTypeName("const GhosttyAllocator *")] GhosttyAllocator* allocator, [NativeTypeName("GhosttySnapshotDecoder *")] GhosttySnapshotDecoderImpl** decoder, [NativeTypeName("const uint8_t *")] byte* ptr, [NativeTypeName("size_t")] nuint len);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ghostty_snapshot_decoder_free([NativeTypeName("GhosttySnapshotDecoder")] GhosttySnapshotDecoderImpl* decoder);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_snapshot_decoder_set([NativeTypeName("GhosttySnapshotDecoder")] GhosttySnapshotDecoderImpl* decoder, GhosttySnapshotDecoderOption option, [NativeTypeName("const void *")] void* value);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_snapshot_decoder_ready([NativeTypeName("GhosttySnapshotDecoder")] GhosttySnapshotDecoderImpl* decoder, [NativeTypeName("GhosttyTerminal *")] nint* terminal);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_snapshot_decoder_next([NativeTypeName("GhosttySnapshotDecoder")] GhosttySnapshotDecoderImpl* decoder);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_snapshot_decoder_decode([NativeTypeName("GhosttySnapshotDecoder")] GhosttySnapshotDecoderImpl* decoder, [NativeTypeName("GhosttyTerminal *")] nint* terminal);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_snapshot_decoder_get([NativeTypeName("GhosttySnapshotDecoder")] GhosttySnapshotDecoderImpl* decoder, GhosttySnapshotDecoderData data, void* @out);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern GhosttyResult ghostty_snapshot_decoder_get_multi([NativeTypeName("GhosttySnapshotDecoder")] GhosttySnapshotDecoderImpl* decoder, [NativeTypeName("size_t")] nuint count, [NativeTypeName("const GhosttySnapshotDecoderData *")] GhosttySnapshotDecoderData* keys, void** values, [NativeTypeName("size_t *")] nuint* out_written);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint8_t")]
        public static extern byte ghostty_unicode_codepoint_width([NativeTypeName("uint32_t")] uint cp);

        [DllImport("libghostty-vt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint ghostty_unicode_grapheme_width([NativeTypeName("const uint32_t *")] uint* cps, [NativeTypeName("size_t")] nuint len, [NativeTypeName("uint8_t *")] byte* width);

        [NativeTypeName("#define GHOSTTY_ENUM_MAX_VALUE INT_MAX")]
        public const int GHOSTTY_ENUM_MAX_VALUE = 2147483647;

        [NativeTypeName("#define GHOSTTY_COLOR_NAMED_BLACK 0")]
        public const int GHOSTTY_COLOR_NAMED_BLACK = 0;

        [NativeTypeName("#define GHOSTTY_COLOR_NAMED_RED 1")]
        public const int GHOSTTY_COLOR_NAMED_RED = 1;

        [NativeTypeName("#define GHOSTTY_COLOR_NAMED_GREEN 2")]
        public const int GHOSTTY_COLOR_NAMED_GREEN = 2;

        [NativeTypeName("#define GHOSTTY_COLOR_NAMED_YELLOW 3")]
        public const int GHOSTTY_COLOR_NAMED_YELLOW = 3;

        [NativeTypeName("#define GHOSTTY_COLOR_NAMED_BLUE 4")]
        public const int GHOSTTY_COLOR_NAMED_BLUE = 4;

        [NativeTypeName("#define GHOSTTY_COLOR_NAMED_MAGENTA 5")]
        public const int GHOSTTY_COLOR_NAMED_MAGENTA = 5;

        [NativeTypeName("#define GHOSTTY_COLOR_NAMED_CYAN 6")]
        public const int GHOSTTY_COLOR_NAMED_CYAN = 6;

        [NativeTypeName("#define GHOSTTY_COLOR_NAMED_WHITE 7")]
        public const int GHOSTTY_COLOR_NAMED_WHITE = 7;

        [NativeTypeName("#define GHOSTTY_COLOR_NAMED_BRIGHT_BLACK 8")]
        public const int GHOSTTY_COLOR_NAMED_BRIGHT_BLACK = 8;

        [NativeTypeName("#define GHOSTTY_COLOR_NAMED_BRIGHT_RED 9")]
        public const int GHOSTTY_COLOR_NAMED_BRIGHT_RED = 9;

        [NativeTypeName("#define GHOSTTY_COLOR_NAMED_BRIGHT_GREEN 10")]
        public const int GHOSTTY_COLOR_NAMED_BRIGHT_GREEN = 10;

        [NativeTypeName("#define GHOSTTY_COLOR_NAMED_BRIGHT_YELLOW 11")]
        public const int GHOSTTY_COLOR_NAMED_BRIGHT_YELLOW = 11;

        [NativeTypeName("#define GHOSTTY_COLOR_NAMED_BRIGHT_BLUE 12")]
        public const int GHOSTTY_COLOR_NAMED_BRIGHT_BLUE = 12;

        [NativeTypeName("#define GHOSTTY_COLOR_NAMED_BRIGHT_MAGENTA 13")]
        public const int GHOSTTY_COLOR_NAMED_BRIGHT_MAGENTA = 13;

        [NativeTypeName("#define GHOSTTY_COLOR_NAMED_BRIGHT_CYAN 14")]
        public const int GHOSTTY_COLOR_NAMED_BRIGHT_CYAN = 14;

        [NativeTypeName("#define GHOSTTY_COLOR_NAMED_BRIGHT_WHITE 15")]
        public const int GHOSTTY_COLOR_NAMED_BRIGHT_WHITE = 15;

        [NativeTypeName("#define GHOSTTY_DA_CONFORMANCE_VT100 1")]
        public const int GHOSTTY_DA_CONFORMANCE_VT100 = 1;

        [NativeTypeName("#define GHOSTTY_DA_CONFORMANCE_VT101 1")]
        public const int GHOSTTY_DA_CONFORMANCE_VT101 = 1;

        [NativeTypeName("#define GHOSTTY_DA_CONFORMANCE_VT102 6")]
        public const int GHOSTTY_DA_CONFORMANCE_VT102 = 6;

        [NativeTypeName("#define GHOSTTY_DA_CONFORMANCE_VT125 12")]
        public const int GHOSTTY_DA_CONFORMANCE_VT125 = 12;

        [NativeTypeName("#define GHOSTTY_DA_CONFORMANCE_VT131 7")]
        public const int GHOSTTY_DA_CONFORMANCE_VT131 = 7;

        [NativeTypeName("#define GHOSTTY_DA_CONFORMANCE_VT132 4")]
        public const int GHOSTTY_DA_CONFORMANCE_VT132 = 4;

        [NativeTypeName("#define GHOSTTY_DA_CONFORMANCE_VT220 62")]
        public const int GHOSTTY_DA_CONFORMANCE_VT220 = 62;

        [NativeTypeName("#define GHOSTTY_DA_CONFORMANCE_VT240 62")]
        public const int GHOSTTY_DA_CONFORMANCE_VT240 = 62;

        [NativeTypeName("#define GHOSTTY_DA_CONFORMANCE_VT320 63")]
        public const int GHOSTTY_DA_CONFORMANCE_VT320 = 63;

        [NativeTypeName("#define GHOSTTY_DA_CONFORMANCE_VT340 63")]
        public const int GHOSTTY_DA_CONFORMANCE_VT340 = 63;

        [NativeTypeName("#define GHOSTTY_DA_CONFORMANCE_VT420 64")]
        public const int GHOSTTY_DA_CONFORMANCE_VT420 = 64;

        [NativeTypeName("#define GHOSTTY_DA_CONFORMANCE_VT510 65")]
        public const int GHOSTTY_DA_CONFORMANCE_VT510 = 65;

        [NativeTypeName("#define GHOSTTY_DA_CONFORMANCE_VT520 65")]
        public const int GHOSTTY_DA_CONFORMANCE_VT520 = 65;

        [NativeTypeName("#define GHOSTTY_DA_CONFORMANCE_VT525 65")]
        public const int GHOSTTY_DA_CONFORMANCE_VT525 = 65;

        [NativeTypeName("#define GHOSTTY_DA_CONFORMANCE_LEVEL_2 62")]
        public const int GHOSTTY_DA_CONFORMANCE_LEVEL_2 = 62;

        [NativeTypeName("#define GHOSTTY_DA_CONFORMANCE_LEVEL_3 63")]
        public const int GHOSTTY_DA_CONFORMANCE_LEVEL_3 = 63;

        [NativeTypeName("#define GHOSTTY_DA_CONFORMANCE_LEVEL_4 64")]
        public const int GHOSTTY_DA_CONFORMANCE_LEVEL_4 = 64;

        [NativeTypeName("#define GHOSTTY_DA_CONFORMANCE_LEVEL_5 65")]
        public const int GHOSTTY_DA_CONFORMANCE_LEVEL_5 = 65;

        [NativeTypeName("#define GHOSTTY_DA_FEATURE_COLUMNS_132 1")]
        public const int GHOSTTY_DA_FEATURE_COLUMNS_132 = 1;

        [NativeTypeName("#define GHOSTTY_DA_FEATURE_PRINTER 2")]
        public const int GHOSTTY_DA_FEATURE_PRINTER = 2;

        [NativeTypeName("#define GHOSTTY_DA_FEATURE_REGIS 3")]
        public const int GHOSTTY_DA_FEATURE_REGIS = 3;

        [NativeTypeName("#define GHOSTTY_DA_FEATURE_SIXEL 4")]
        public const int GHOSTTY_DA_FEATURE_SIXEL = 4;

        [NativeTypeName("#define GHOSTTY_DA_FEATURE_SELECTIVE_ERASE 6")]
        public const int GHOSTTY_DA_FEATURE_SELECTIVE_ERASE = 6;

        [NativeTypeName("#define GHOSTTY_DA_FEATURE_USER_DEFINED_KEYS 8")]
        public const int GHOSTTY_DA_FEATURE_USER_DEFINED_KEYS = 8;

        [NativeTypeName("#define GHOSTTY_DA_FEATURE_NATIONAL_REPLACEMENT 9")]
        public const int GHOSTTY_DA_FEATURE_NATIONAL_REPLACEMENT = 9;

        [NativeTypeName("#define GHOSTTY_DA_FEATURE_TECHNICAL_CHARACTERS 15")]
        public const int GHOSTTY_DA_FEATURE_TECHNICAL_CHARACTERS = 15;

        [NativeTypeName("#define GHOSTTY_DA_FEATURE_LOCATOR 16")]
        public const int GHOSTTY_DA_FEATURE_LOCATOR = 16;

        [NativeTypeName("#define GHOSTTY_DA_FEATURE_TERMINAL_STATE 17")]
        public const int GHOSTTY_DA_FEATURE_TERMINAL_STATE = 17;

        [NativeTypeName("#define GHOSTTY_DA_FEATURE_WINDOWING 18")]
        public const int GHOSTTY_DA_FEATURE_WINDOWING = 18;

        [NativeTypeName("#define GHOSTTY_DA_FEATURE_HORIZONTAL_SCROLLING 21")]
        public const int GHOSTTY_DA_FEATURE_HORIZONTAL_SCROLLING = 21;

        [NativeTypeName("#define GHOSTTY_DA_FEATURE_ANSI_COLOR 22")]
        public const int GHOSTTY_DA_FEATURE_ANSI_COLOR = 22;

        [NativeTypeName("#define GHOSTTY_DA_FEATURE_RECTANGULAR_EDITING 28")]
        public const int GHOSTTY_DA_FEATURE_RECTANGULAR_EDITING = 28;

        [NativeTypeName("#define GHOSTTY_DA_FEATURE_ANSI_TEXT_LOCATOR 29")]
        public const int GHOSTTY_DA_FEATURE_ANSI_TEXT_LOCATOR = 29;

        [NativeTypeName("#define GHOSTTY_DA_FEATURE_CLIPBOARD 52")]
        public const int GHOSTTY_DA_FEATURE_CLIPBOARD = 52;

        [NativeTypeName("#define GHOSTTY_DA_DEVICE_TYPE_VT100 0")]
        public const int GHOSTTY_DA_DEVICE_TYPE_VT100 = 0;

        [NativeTypeName("#define GHOSTTY_DA_DEVICE_TYPE_VT220 1")]
        public const int GHOSTTY_DA_DEVICE_TYPE_VT220 = 1;

        [NativeTypeName("#define GHOSTTY_DA_DEVICE_TYPE_VT240 2")]
        public const int GHOSTTY_DA_DEVICE_TYPE_VT240 = 2;

        [NativeTypeName("#define GHOSTTY_DA_DEVICE_TYPE_VT330 18")]
        public const int GHOSTTY_DA_DEVICE_TYPE_VT330 = 18;

        [NativeTypeName("#define GHOSTTY_DA_DEVICE_TYPE_VT340 19")]
        public const int GHOSTTY_DA_DEVICE_TYPE_VT340 = 19;

        [NativeTypeName("#define GHOSTTY_DA_DEVICE_TYPE_VT320 24")]
        public const int GHOSTTY_DA_DEVICE_TYPE_VT320 = 24;

        [NativeTypeName("#define GHOSTTY_DA_DEVICE_TYPE_VT382 32")]
        public const int GHOSTTY_DA_DEVICE_TYPE_VT382 = 32;

        [NativeTypeName("#define GHOSTTY_DA_DEVICE_TYPE_VT420 41")]
        public const int GHOSTTY_DA_DEVICE_TYPE_VT420 = 41;

        [NativeTypeName("#define GHOSTTY_DA_DEVICE_TYPE_VT510 61")]
        public const int GHOSTTY_DA_DEVICE_TYPE_VT510 = 61;

        [NativeTypeName("#define GHOSTTY_DA_DEVICE_TYPE_VT520 64")]
        public const int GHOSTTY_DA_DEVICE_TYPE_VT520 = 64;

        [NativeTypeName("#define GHOSTTY_DA_DEVICE_TYPE_VT525 65")]
        public const int GHOSTTY_DA_DEVICE_TYPE_VT525 = 65;

        [NativeTypeName("#define GHOSTTY_MODE_KAM (ghostty_mode_new(2, true))")]
        public static readonly ushort GHOSTTY_MODE_KAM = (ghostty_mode_new(2, (1) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_INSERT (ghostty_mode_new(4, true))")]
        public static readonly ushort GHOSTTY_MODE_INSERT = (ghostty_mode_new(4, (1) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_SRM (ghostty_mode_new(12, true))")]
        public static readonly ushort GHOSTTY_MODE_SRM = (ghostty_mode_new(12, (1) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_LINEFEED (ghostty_mode_new(20, true))")]
        public static readonly ushort GHOSTTY_MODE_LINEFEED = (ghostty_mode_new(20, (1) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_DECCKM (ghostty_mode_new(1, false))")]
        public static readonly ushort GHOSTTY_MODE_DECCKM = (ghostty_mode_new(1, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_132_COLUMN (ghostty_mode_new(3, false))")]
        public static readonly ushort GHOSTTY_MODE_132_COLUMN = (ghostty_mode_new(3, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_SLOW_SCROLL (ghostty_mode_new(4, false))")]
        public static readonly ushort GHOSTTY_MODE_SLOW_SCROLL = (ghostty_mode_new(4, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_REVERSE_COLORS (ghostty_mode_new(5, false))")]
        public static readonly ushort GHOSTTY_MODE_REVERSE_COLORS = (ghostty_mode_new(5, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_ORIGIN (ghostty_mode_new(6, false))")]
        public static readonly ushort GHOSTTY_MODE_ORIGIN = (ghostty_mode_new(6, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_WRAPAROUND (ghostty_mode_new(7, false))")]
        public static readonly ushort GHOSTTY_MODE_WRAPAROUND = (ghostty_mode_new(7, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_AUTOREPEAT (ghostty_mode_new(8, false))")]
        public static readonly ushort GHOSTTY_MODE_AUTOREPEAT = (ghostty_mode_new(8, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_X10_MOUSE (ghostty_mode_new(9, false))")]
        public static readonly ushort GHOSTTY_MODE_X10_MOUSE = (ghostty_mode_new(9, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_CURSOR_BLINKING (ghostty_mode_new(12, false))")]
        public static readonly ushort GHOSTTY_MODE_CURSOR_BLINKING = (ghostty_mode_new(12, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_CURSOR_VISIBLE (ghostty_mode_new(25, false))")]
        public static readonly ushort GHOSTTY_MODE_CURSOR_VISIBLE = (ghostty_mode_new(25, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_ENABLE_MODE_3 (ghostty_mode_new(40, false))")]
        public static readonly ushort GHOSTTY_MODE_ENABLE_MODE_3 = (ghostty_mode_new(40, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_REVERSE_WRAP (ghostty_mode_new(45, false))")]
        public static readonly ushort GHOSTTY_MODE_REVERSE_WRAP = (ghostty_mode_new(45, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_ALT_SCREEN_LEGACY (ghostty_mode_new(47, false))")]
        public static readonly ushort GHOSTTY_MODE_ALT_SCREEN_LEGACY = (ghostty_mode_new(47, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_KEYPAD_KEYS (ghostty_mode_new(66, false))")]
        public static readonly ushort GHOSTTY_MODE_KEYPAD_KEYS = (ghostty_mode_new(66, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_BACKARROW_KEY_MODE (ghostty_mode_new(67, false))")]
        public static readonly ushort GHOSTTY_MODE_BACKARROW_KEY_MODE = (ghostty_mode_new(67, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_LEFT_RIGHT_MARGIN (ghostty_mode_new(69, false))")]
        public static readonly ushort GHOSTTY_MODE_LEFT_RIGHT_MARGIN = (ghostty_mode_new(69, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_NORMAL_MOUSE (ghostty_mode_new(1000, false))")]
        public static readonly ushort GHOSTTY_MODE_NORMAL_MOUSE = (ghostty_mode_new(1000, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_BUTTON_MOUSE (ghostty_mode_new(1002, false))")]
        public static readonly ushort GHOSTTY_MODE_BUTTON_MOUSE = (ghostty_mode_new(1002, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_ANY_MOUSE (ghostty_mode_new(1003, false))")]
        public static readonly ushort GHOSTTY_MODE_ANY_MOUSE = (ghostty_mode_new(1003, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_FOCUS_EVENT (ghostty_mode_new(1004, false))")]
        public static readonly ushort GHOSTTY_MODE_FOCUS_EVENT = (ghostty_mode_new(1004, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_UTF8_MOUSE (ghostty_mode_new(1005, false))")]
        public static readonly ushort GHOSTTY_MODE_UTF8_MOUSE = (ghostty_mode_new(1005, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_SGR_MOUSE (ghostty_mode_new(1006, false))")]
        public static readonly ushort GHOSTTY_MODE_SGR_MOUSE = (ghostty_mode_new(1006, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_ALT_SCROLL (ghostty_mode_new(1007, false))")]
        public static readonly ushort GHOSTTY_MODE_ALT_SCROLL = (ghostty_mode_new(1007, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_URXVT_MOUSE (ghostty_mode_new(1015, false))")]
        public static readonly ushort GHOSTTY_MODE_URXVT_MOUSE = (ghostty_mode_new(1015, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_SGR_PIXELS_MOUSE (ghostty_mode_new(1016, false))")]
        public static readonly ushort GHOSTTY_MODE_SGR_PIXELS_MOUSE = (ghostty_mode_new(1016, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_NUMLOCK_KEYPAD (ghostty_mode_new(1035, false))")]
        public static readonly ushort GHOSTTY_MODE_NUMLOCK_KEYPAD = (ghostty_mode_new(1035, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_ALT_ESC_PREFIX (ghostty_mode_new(1036, false))")]
        public static readonly ushort GHOSTTY_MODE_ALT_ESC_PREFIX = (ghostty_mode_new(1036, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_ALT_SENDS_ESC (ghostty_mode_new(1039, false))")]
        public static readonly ushort GHOSTTY_MODE_ALT_SENDS_ESC = (ghostty_mode_new(1039, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_REVERSE_WRAP_EXT (ghostty_mode_new(1045, false))")]
        public static readonly ushort GHOSTTY_MODE_REVERSE_WRAP_EXT = (ghostty_mode_new(1045, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_ALT_SCREEN (ghostty_mode_new(1047, false))")]
        public static readonly ushort GHOSTTY_MODE_ALT_SCREEN = (ghostty_mode_new(1047, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_SAVE_CURSOR (ghostty_mode_new(1048, false))")]
        public static readonly ushort GHOSTTY_MODE_SAVE_CURSOR = (ghostty_mode_new(1048, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_ALT_SCREEN_SAVE (ghostty_mode_new(1049, false))")]
        public static readonly ushort GHOSTTY_MODE_ALT_SCREEN_SAVE = (ghostty_mode_new(1049, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_BRACKETED_PASTE (ghostty_mode_new(2004, false))")]
        public static readonly ushort GHOSTTY_MODE_BRACKETED_PASTE = (ghostty_mode_new(2004, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_SYNC_OUTPUT (ghostty_mode_new(2026, false))")]
        public static readonly ushort GHOSTTY_MODE_SYNC_OUTPUT = (ghostty_mode_new(2026, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_GRAPHEME_CLUSTER (ghostty_mode_new(2027, false))")]
        public static readonly ushort GHOSTTY_MODE_GRAPHEME_CLUSTER = (ghostty_mode_new(2027, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_COLOR_SCHEME_REPORT (ghostty_mode_new(2031, false))")]
        public static readonly ushort GHOSTTY_MODE_COLOR_SCHEME_REPORT = (ghostty_mode_new(2031, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_VISIBILITY_REPORT (ghostty_mode_new(2033, false))")]
        public static readonly ushort GHOSTTY_MODE_VISIBILITY_REPORT = (ghostty_mode_new(2033, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_IN_BAND_RESIZE (ghostty_mode_new(2048, false))")]
        public static readonly ushort GHOSTTY_MODE_IN_BAND_RESIZE = (ghostty_mode_new(2048, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODE_PASTE_EVENTS (ghostty_mode_new(5522, false))")]
        public static readonly ushort GHOSTTY_MODE_PASTE_EVENTS = (ghostty_mode_new(5522, (0) != 0));

        [NativeTypeName("#define GHOSTTY_MODS_SHIFT (1 << 0)")]
        public const int GHOSTTY_MODS_SHIFT = (1 << 0);

        [NativeTypeName("#define GHOSTTY_MODS_CTRL (1 << 1)")]
        public const int GHOSTTY_MODS_CTRL = (1 << 1);

        [NativeTypeName("#define GHOSTTY_MODS_ALT (1 << 2)")]
        public const int GHOSTTY_MODS_ALT = (1 << 2);

        [NativeTypeName("#define GHOSTTY_MODS_SUPER (1 << 3)")]
        public const int GHOSTTY_MODS_SUPER = (1 << 3);

        [NativeTypeName("#define GHOSTTY_MODS_CAPS_LOCK (1 << 4)")]
        public const int GHOSTTY_MODS_CAPS_LOCK = (1 << 4);

        [NativeTypeName("#define GHOSTTY_MODS_NUM_LOCK (1 << 5)")]
        public const int GHOSTTY_MODS_NUM_LOCK = (1 << 5);

        [NativeTypeName("#define GHOSTTY_MODS_SHIFT_SIDE (1 << 6)")]
        public const int GHOSTTY_MODS_SHIFT_SIDE = (1 << 6);

        [NativeTypeName("#define GHOSTTY_MODS_CTRL_SIDE (1 << 7)")]
        public const int GHOSTTY_MODS_CTRL_SIDE = (1 << 7);

        [NativeTypeName("#define GHOSTTY_MODS_ALT_SIDE (1 << 8)")]
        public const int GHOSTTY_MODS_ALT_SIDE = (1 << 8);

        [NativeTypeName("#define GHOSTTY_MODS_SUPER_SIDE (1 << 9)")]
        public const int GHOSTTY_MODS_SUPER_SIDE = (1 << 9);

        [NativeTypeName("#define GHOSTTY_KITTY_KEY_DISABLED 0")]
        public const int GHOSTTY_KITTY_KEY_DISABLED = 0;

        [NativeTypeName("#define GHOSTTY_KITTY_KEY_DISAMBIGUATE (1 << 0)")]
        public const int GHOSTTY_KITTY_KEY_DISAMBIGUATE = (1 << 0);

        [NativeTypeName("#define GHOSTTY_KITTY_KEY_REPORT_EVENTS (1 << 1)")]
        public const int GHOSTTY_KITTY_KEY_REPORT_EVENTS = (1 << 1);

        [NativeTypeName("#define GHOSTTY_KITTY_KEY_REPORT_ALTERNATES (1 << 2)")]
        public const int GHOSTTY_KITTY_KEY_REPORT_ALTERNATES = (1 << 2);

        [NativeTypeName("#define GHOSTTY_KITTY_KEY_REPORT_ALL (1 << 3)")]
        public const int GHOSTTY_KITTY_KEY_REPORT_ALL = (1 << 3);

        [NativeTypeName("#define GHOSTTY_KITTY_KEY_REPORT_ASSOCIATED (1 << 4)")]
        public const int GHOSTTY_KITTY_KEY_REPORT_ASSOCIATED = (1 << 4);

        [NativeTypeName("#define GHOSTTY_KITTY_KEY_ALL (GHOSTTY_KITTY_KEY_DISAMBIGUATE | GHOSTTY_KITTY_KEY_REPORT_EVENTS | GHOSTTY_KITTY_KEY_REPORT_ALTERNATES | GHOSTTY_KITTY_KEY_REPORT_ALL | GHOSTTY_KITTY_KEY_REPORT_ASSOCIATED)")]
        public const int GHOSTTY_KITTY_KEY_ALL = ((1 << 0) | (1 << 1) | (1 << 2) | (1 << 3) | (1 << 4));
    }
}
