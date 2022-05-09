using Raylib_cs;
using ImGuiNET;

namespace RlImGuiApp;

internal static class Program
{
    private static void InitWindow(int width, int height, string title)
    {
        Raylib.SetConfigFlags(ConfigFlags.FLAG_MSAA_4X_HINT | ConfigFlags.FLAG_VSYNC_HINT |
                              ConfigFlags.FLAG_WINDOW_RESIZABLE);
        Raylib.SetTraceLogLevel(TraceLogLevel.LOG_WARNING);
        Raylib.InitWindow(width, height, title);
        Raylib.SetWindowMinSize(640, 480);
    }

    private static void Main(string[] args)
    {
        InitWindow(1280, 720, "TMV");
        
        UI.ImGuiController.Setup();
        var uiLayers = new List<UI.UiLayer> {new UI.Layers.ExampleUiLayer {Open = true}};
        foreach (UI.UiLayer layer in uiLayers)
            layer.Attach();

        while (!Raylib.WindowShouldClose())
        {
            foreach (UI.UiLayer layer in uiLayers)
                layer.Update();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RAYWHITE);

            UI.ImGuiController.Begin();
            ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);
            ImGui.ShowDemoWindow();
            foreach (UI.UiLayer layer in uiLayers)
                layer.Render();
            UI.ImGuiController.End();

            Raylib.EndDrawing();
        }

        UI.ImGuiController.Shutdown();
        Raylib.CloseWindow();
    }
}