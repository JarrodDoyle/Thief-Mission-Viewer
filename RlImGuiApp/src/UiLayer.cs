namespace RlImGuiApp;

public abstract class UiLayer
{
    public bool Open { get; set; }

    public abstract void Attach();
    public abstract void Detach();
    public abstract void Render();
    public abstract void Update();
}