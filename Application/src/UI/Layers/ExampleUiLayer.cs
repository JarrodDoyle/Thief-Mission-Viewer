using ImGuiNET;
using RlImGuiApp.LG;

namespace RlImGuiApp.UI.Layers;

public class ExampleUiLayer : UiLayer
{
    private DbFile? _dbFile;
    private WorldRep? _worldRep;

    public override void Attach()
    {
        Console.WriteLine("Attached layer");

        _dbFile = new DbFile("../../../../Data/miss1.mis");
        _worldRep = new WorldRep(_dbFile);
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
            var itemCount = (int)toc.ItemCount;
            ImGui.Text($"Item count: {itemCount}");
            ImGui.BeginListBox("Items", ImGui.GetContentRegionAvail());
            for (int i = 0; i < itemCount; i++)
            {
                var item = toc.Items.Values.ElementAt(i);
                ImGui.Selectable($"{item.Name}");
            }
            ImGui.EndListBox();
            ImGui.End();
        }

        if (ImGui.Begin("World Rep"))
        {
            ImGui.Text($"Type: {_worldRep?.Chunk?.Header.Name}");
            ImGui.Text($"Version: {_worldRep?.Chunk?.Header.Version}");
            ImGui.Text($"Cell count: {_worldRep?.Header.CellCount}");
            ImGui.Text($"Data size: {_worldRep?.Header.DataSize}");
            ImGui.Text($"{_worldRep?.Header.LightmapFormat}");
            ImGui.Text($"{_worldRep?.Header.LightmapScale}");
            ImGui.End();
        }

        Open = isOpen;
    }

    public override void Update()
    {
    }
}