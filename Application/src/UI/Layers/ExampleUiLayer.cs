using System.Numerics;
using ImGuiNET;
using RlImGuiApp.LG;
using RlImGuiApp.Model;

namespace RlImGuiApp.UI.Layers;

public class ExampleUiLayer : UiLayer
{
    public override void Attach()
    {
        Console.WriteLine("Attached layer");
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
            ImGui.Text($"TOC Offset: {WorldRepManager.DbFile?.Header.TocOffset}");
            ImGui.Text($"DB Version: {WorldRepManager.DbFile?.Header.Version}");
            ImGui.Text($"Deadbeef: {WorldRepManager.DbFile?.Header.Deadbeef}");
            ImGui.End();
        }

        if (ImGui.Begin("LGDB Table of Contents", ref isOpen) && WorldRepManager.DbFile?.TableOfContents != null)
        {
            var toc = WorldRepManager.DbFile.TableOfContents;
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
            ImGui.Text($"Type: {WorldRepManager.WorldRep?.Chunk?.Header.Name}");
            ImGui.Text($"Version: {WorldRepManager.WorldRep?.Chunk?.Header.Version}");
            ImGui.Text($"Cell count: {WorldRepManager.WorldRep?.Header.CellCount}");
            ImGui.Text($"Data size: {WorldRepManager.WorldRep?.Header.DataSize}");
            ImGui.End();
        }

        if (ImGui.Begin("File List"))
        {
            ImGui.BeginListBox("Files", ImGui.GetContentRegionAvail());
            var selected = WorldRepManager.SelectedFile;
            for (int i = 0; i < WorldRepManager.Files.Length; i++)
            {
                var isSelected = i == selected;
                if (ImGui.Selectable($"{WorldRepManager.Files[i].Name}", isSelected))
                    WorldRepManager.LoadFile(i);
                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndListBox();
            ImGui.End();
        }

        Open = isOpen;
    }

    public override void Update()
    {
    }
}