using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Raylib_cs;

namespace RlImGuiApp;

public static class ImGuiController
{
    private static IntPtr _imGuiContext = IntPtr.Zero;
    private static ImGuiMouseCursor _currentMouseCursor = ImGuiMouseCursor.COUNT;
    private static Dictionary<ImGuiMouseCursor, MouseCursor> _mouseCursorMap = null!;
    private static KeyboardKey[] _keyEnumMap = null!;
    private static Texture2D _fontTexture;

    public static void Setup()
    {
        _mouseCursorMap = new Dictionary<ImGuiMouseCursor, MouseCursor>();
        _keyEnumMap = Enum.GetValues(typeof(KeyboardKey)) as KeyboardKey[] ?? throw new InvalidOperationException();
        _fontTexture.id = 0;
        _imGuiContext = ImGui.CreateContext();

        ImGui.SetCurrentContext(_imGuiContext);
        ImGui.StyleColorsDark();

        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.AddFontDefault();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        SetupMouseCursors();
        SetupKeymap();
        ReloadFonts();
    }

    public static void Shutdown()
    {
        Raylib.UnloadTexture(_fontTexture);
        ImGui.DestroyContext(_imGuiContext);
    }

    public static void Begin()
    {
        ImGui.SetCurrentContext(_imGuiContext);

        NewFrame();
        FrameEvents();
        ImGui.NewFrame();
    }

    public static void End()
    {
        ImGui.SetCurrentContext(_imGuiContext);
        ImGui.Render();
        RenderData();
    }

    private static void SetupMouseCursors()
    {
        _mouseCursorMap.Clear();
        _mouseCursorMap[ImGuiMouseCursor.Arrow] = MouseCursor.MOUSE_CURSOR_ARROW;
        _mouseCursorMap[ImGuiMouseCursor.TextInput] = MouseCursor.MOUSE_CURSOR_IBEAM;
        _mouseCursorMap[ImGuiMouseCursor.Hand] = MouseCursor.MOUSE_CURSOR_POINTING_HAND;
        _mouseCursorMap[ImGuiMouseCursor.ResizeAll] = MouseCursor.MOUSE_CURSOR_RESIZE_ALL;
        _mouseCursorMap[ImGuiMouseCursor.ResizeEW] = MouseCursor.MOUSE_CURSOR_RESIZE_EW;
        _mouseCursorMap[ImGuiMouseCursor.ResizeNESW] = MouseCursor.MOUSE_CURSOR_RESIZE_NESW;
        _mouseCursorMap[ImGuiMouseCursor.ResizeNS] = MouseCursor.MOUSE_CURSOR_RESIZE_NS;
        _mouseCursorMap[ImGuiMouseCursor.ResizeNWSE] = MouseCursor.MOUSE_CURSOR_RESIZE_NWSE;
        _mouseCursorMap[ImGuiMouseCursor.NotAllowed] = MouseCursor.MOUSE_CURSOR_NOT_ALLOWED;
    }

    private static unsafe void ReloadFonts()
    {
        ImGui.SetCurrentContext(_imGuiContext);
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out _);
        Image image = new Image
        {
            data = pixels,
            width = width,
            height = height,
            mipmaps = 1,
            format = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8,
        };
        _fontTexture = Raylib.LoadTextureFromImage(image);
        io.Fonts.SetTexID(new IntPtr(_fontTexture.id));
    }

    private static void SetupKeymap()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.KeyMap[(int) ImGuiKey.Tab] = (int) KeyboardKey.KEY_TAB;
        io.KeyMap[(int) ImGuiKey.LeftArrow] = (int) KeyboardKey.KEY_LEFT;
        io.KeyMap[(int) ImGuiKey.RightArrow] = (int) KeyboardKey.KEY_RIGHT;
        io.KeyMap[(int) ImGuiKey.UpArrow] = (int) KeyboardKey.KEY_UP;
        io.KeyMap[(int) ImGuiKey.DownArrow] = (int) KeyboardKey.KEY_DOWN;
        io.KeyMap[(int) ImGuiKey.PageUp] = (int) KeyboardKey.KEY_PAGE_UP;
        io.KeyMap[(int) ImGuiKey.PageDown] = (int) KeyboardKey.KEY_PAGE_DOWN;
        io.KeyMap[(int) ImGuiKey.Home] = (int) KeyboardKey.KEY_HOME;
        io.KeyMap[(int) ImGuiKey.End] = (int) KeyboardKey.KEY_END;
        io.KeyMap[(int) ImGuiKey.Delete] = (int) KeyboardKey.KEY_DELETE;
        io.KeyMap[(int) ImGuiKey.Backspace] = (int) KeyboardKey.KEY_BACKSPACE;
        io.KeyMap[(int) ImGuiKey.Enter] = (int) KeyboardKey.KEY_ENTER;
        io.KeyMap[(int) ImGuiKey.Escape] = (int) KeyboardKey.KEY_ESCAPE;
        io.KeyMap[(int) ImGuiKey.Space] = (int) KeyboardKey.KEY_SPACE;
        io.KeyMap[(int) ImGuiKey.A] = (int) KeyboardKey.KEY_A;
        io.KeyMap[(int) ImGuiKey.C] = (int) KeyboardKey.KEY_C;
        io.KeyMap[(int) ImGuiKey.V] = (int) KeyboardKey.KEY_V;
        io.KeyMap[(int) ImGuiKey.X] = (int) KeyboardKey.KEY_X;
        io.KeyMap[(int) ImGuiKey.Y] = (int) KeyboardKey.KEY_Y;
        io.KeyMap[(int) ImGuiKey.Z] = (int) KeyboardKey.KEY_Z;
    }

    private static void NewFrame()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
        io.DisplayFramebufferScale = new Vector2(1, 1);
        io.DeltaTime = Raylib.GetFrameTime();
        io.KeyCtrl = Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL) || Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL);
        io.KeyShift = Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SHIFT) || Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT);
        io.KeyAlt = Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_ALT) || Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_ALT);
        io.KeySuper = Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SUPER) || Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SUPER);
        io.MouseDown[0] = Raylib.IsMouseButtonDown(MouseButton.MOUSE_LEFT_BUTTON);
        io.MouseDown[1] = Raylib.IsMouseButtonDown(MouseButton.MOUSE_RIGHT_BUTTON);
        io.MouseDown[2] = Raylib.IsMouseButtonDown(MouseButton.MOUSE_MIDDLE_BUTTON);

        if (io.WantSetMousePos)
            Raylib.SetMousePosition((int) io.MousePos.X, (int) io.MousePos.Y);
        else
            io.MousePos = Raylib.GetMousePosition();

        float mouseWheelMove = Raylib.GetMouseWheelMove();
        if (mouseWheelMove > 0)
            io.MouseWheel += 1;
        else if (mouseWheelMove < 0)
            io.MouseWheel -= 1;

        ImGuiMouseCursor imguiCursor = ImGui.GetMouseCursor();
        if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) != 0) return;
        if (imguiCursor == _currentMouseCursor && !io.MouseDrawCursor) return;

        _currentMouseCursor = imguiCursor;
        if (io.MouseDrawCursor || imguiCursor == ImGuiMouseCursor.None)
            Raylib.HideCursor();
        else
        {
            Raylib.ShowCursor();
            Raylib.SetMouseCursor(_mouseCursorMap.ContainsKey(imguiCursor)
                ? _mouseCursorMap[imguiCursor]
                : MouseCursor.MOUSE_CURSOR_DEFAULT);
        }
    }

    private static void FrameEvents()
    {
        ImGuiIOPtr io = ImGui.GetIO();

        foreach (KeyboardKey key in _keyEnumMap)
            io.KeysDown[(int) key] = Raylib.IsKeyDown(key);

        uint pressed = (uint) Raylib.GetCharPressed();
        while (pressed != 0)
        {
            io.AddInputCharacter(pressed);
            pressed = (uint) Raylib.GetCharPressed();
        }
    }

    private static void EnableScissor(float x, float y, float width, float height)
    {
        Rlgl.rlEnableScissorTest();
        Rlgl.rlScissor((int) x, Raylib.GetScreenHeight() - (int) (y + height), (int) width, (int) height);
    }

    private static void TriangleVert(ImDrawVertPtr idxVert)
    {
        byte[] c = BitConverter.GetBytes(idxVert.col);
        Rlgl.rlColor4ub(c[0], c[1], c[2], c[3]);
        Rlgl.rlTexCoord2f(idxVert.uv.X, idxVert.uv.Y);
        Rlgl.rlVertex2f(idxVert.pos.X, idxVert.pos.Y);
    }

    private static void RenderTriangles(uint count, uint indexStart, ImVector<ushort> indexBuffer,
        ImPtrVector<ImDrawVertPtr> vertBuffer, IntPtr texturePtr)
    {
        if (count < 3) return;

        uint textureId = 0;
        if (texturePtr != IntPtr.Zero)
            textureId = (uint) texturePtr.ToInt32();

        Rlgl.rlBegin(DrawMode.TRIANGLES);
        Rlgl.rlSetTexture(textureId);
        for (int i = 0; i <= (count - 3); i += 3)
        {
            if (Rlgl.rlCheckRenderBatchLimit(3))
            {
                Rlgl.rlBegin(DrawMode.TRIANGLES);
                Rlgl.rlSetTexture(textureId);
            }

            TriangleVert(vertBuffer[indexBuffer[(int) indexStart + i]]);
            TriangleVert(vertBuffer[indexBuffer[(int) indexStart + i + 1]]);
            TriangleVert(vertBuffer[indexBuffer[(int) indexStart + i + 2]]);
        }

        Rlgl.rlEnd();
    }

    private static void RenderData()
    {
        Rlgl.rlDrawRenderBatchActive();
        Rlgl.rlDisableBackfaceCulling();

        var data = ImGui.GetDrawData();
        for (int l = 0; l < data.CmdListsCount; l++)
        {
            ImDrawListPtr commandList = data.CmdListsRange[l];
            for (int cmdIndex = 0; cmdIndex < commandList.CmdBuffer.Size; cmdIndex++)
            {
                var cmd = commandList.CmdBuffer[cmdIndex];

                EnableScissor(cmd.ClipRect.X - data.DisplayPos.X, cmd.ClipRect.Y - data.DisplayPos.Y,
                    cmd.ClipRect.Z - (cmd.ClipRect.X - data.DisplayPos.X),
                    cmd.ClipRect.W - (cmd.ClipRect.Y - data.DisplayPos.Y));
                if (cmd.UserCallback != IntPtr.Zero)
                {
                    Callback cb = Marshal.GetDelegateForFunctionPointer<Callback>(cmd.UserCallback);
                    cb(commandList, cmd);
                    continue;
                }

                RenderTriangles(cmd.ElemCount, cmd.IdxOffset, commandList.IdxBuffer, commandList.VtxBuffer,
                    cmd.TextureId);
                Rlgl.rlDrawRenderBatchActive();
            }
        }

        Rlgl.rlSetTexture(0);
        Rlgl.rlDisableScissorTest();
        Rlgl.rlEnableBackfaceCulling();
    }

    private delegate void Callback(ImDrawListPtr list, ImDrawCmdPtr cmd);
}