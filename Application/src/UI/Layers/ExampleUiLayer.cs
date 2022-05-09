using System.Numerics;
using ImGuiNET;
using RlImGuiApp.LG;
using RlImGuiApp.Model;

namespace RlImGuiApp.UI.Layers;

public class ExampleUiLayer : UiLayer
{
    private DbFile? _dbFile;
    private WorldRep? _worldRep;
    public WorldRepMesh? WorldRepMesh;

    public override void Attach()
    {
        Console.WriteLine("Attached layer");

        _dbFile = new DbFile("../../../../Data/miss1.mis");
        _worldRep = new WorldRep(_dbFile);
        WorldRepMesh = new WorldRepMesh(_worldRep);
    }

    public override void Detach()
    {
        Console.WriteLine("Detached layer");
    }

    public override void Render()
    {
        bool isOpen = Open;
        if (!isOpen) return;

        if (ImGui.Begin("LGDB Header Info", ref isOpen))
        {
            ImGui.Text($"TOC Offset: {_dbFile?.Header.TocOffset}");
            ImGui.Text($"DB Version: {_dbFile?.Header.Version}");
            ImGui.Text($"Deadbeef: {_dbFile?.Header.Deadbeef}");
            ImGui.End();
        }

        if (ImGui.Begin("LGDB Table of Contents", ref isOpen))
        {
            var toc = _dbFile!.TableOfContents;
            var itemCount = (int) toc.ItemCount;
            var keys = toc.Items.Keys.ToArray();
            ImGui.Text($"Item count: {itemCount}");
            ImGui.BeginListBox("Items", ImGui.GetContentRegionAvail());
            for (int i = 0; i < itemCount; i++)
                ImGui.Selectable($"{keys[i]}");
            ImGui.EndListBox();
            ImGui.End();
        }

        if (ImGui.Begin("World Rep"))
        {
            ImGui.Text($"Type: {_worldRep?.Chunk?.Header.Name}");
            ImGui.Text($"Version: {_worldRep?.Chunk?.Header.Version}");
            ImGui.Text($"Cell count: {_worldRep?.Header.CellCount}");
            ImGui.Text($"Data size: {_worldRep?.Header.DataSize}");
            ImGui.End();
        }

        Open = isOpen;
    }

    public override void Update()
    {
    }
}