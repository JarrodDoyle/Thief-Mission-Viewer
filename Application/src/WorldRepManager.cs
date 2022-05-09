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
        if (index < 0 || index >= Files.Length) return;
        var file = Files[index];
        DbFile = new LG.DbFile(file.FullName);
        WorldRep = new LG.WorldRep(DbFile);
        _worldRepMesh = new Model.WorldRepMesh(WorldRep);
        SelectedFile = index;
    }

    public static void Render()
    {
        _worldRepMesh?.Render();
    }
}