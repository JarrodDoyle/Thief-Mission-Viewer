namespace RlImGuiApp;

public static class WorldRepManager
{
    public static FileInfo[] Files { get; }
    public static int SelectedFile { get; private set; }
    public static LG.DbFile? DbFile;
    public static LG.WorldRep? WorldRep;
    private static string DataDir { get; }
    private static Model.WorldRepMesh? _worldRepMesh;

    static WorldRepManager()
    {
        DataDir = "../../../../Data/";
        Files = new DirectoryInfo(DataDir).GetFiles();
        SelectedFile = -1;
    }

    public static void LoadFile(int index)
    {
        // TODO: Need to dispose of any currently loaded resources
        if (index < 0 || index >= Files.Length || index == SelectedFile) return;
        var file = Files[index];
        var t0 = DateTime.Now;
        DbFile = new LG.DbFile(file.FullName);
        var t1 = DateTime.Now;
        WorldRep = new LG.WorldRep(DbFile);
        var t2 = DateTime.Now;
        _worldRepMesh = new Model.WorldRepMesh(WorldRep);
        var t3 = DateTime.Now;
        SelectedFile = index;

        Console.WriteLine($"File Load: {t1 - t0}");
        Console.WriteLine($"WR Load: {t2 - t1}");
        Console.WriteLine($"WR Mesh Build: {t3 - t2}");
        Console.WriteLine($"Total: {t3 - t0}");
    }

    public static void Render()
    {
        _worldRepMesh?.Render();
    }
}