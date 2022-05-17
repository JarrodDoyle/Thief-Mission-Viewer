using System.Numerics;
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
        var exampleLayer = new UI.Layers.ExampleUiLayer {Open = true};
        var uiLayers = new List<UI.UiLayer> {exampleLayer};
        foreach (UI.UiLayer layer in uiLayers)
            layer.Attach();

        Camera3D camera = new Camera3D();
        camera.position = new Vector3(0, 10, 10);
        camera.target = Vector3.Zero;
        camera.up = Vector3.UnitY;
        camera.fovy = 60;
        camera.projection = CameraProjection.CAMERA_PERSPECTIVE;

        Raylib.SetCameraMode(camera, CameraMode.CAMERA_FREE);

        while (!Raylib.WindowShouldClose())
        {
            foreach (var layer in uiLayers)
                layer.Update();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.BLACK);
            Raylib.UpdateCamera(ref camera);

            Raylib.BeginMode3D(camera);
            WorldRepManager.Render();
            Raylib.EndMode3D();

            UI.ImGuiController.Begin();
            ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);
            ImGui.ShowDemoWindow();
            foreach (var layer in uiLayers)
                layer.Render();
            UI.ImGuiController.End();

            Raylib.DrawFPS(0, 0);
            Raylib.EndDrawing();
        }

        UI.ImGuiController.Shutdown();
        Raylib.CloseWindow();
    }
}