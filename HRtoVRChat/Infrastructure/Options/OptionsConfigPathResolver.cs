namespace HRtoVRChat.Infrastructure.Options;

internal record OptionsConfigPathResolver<T> {
    public OptionsConfigPathResolver() {
        Path = typeof(T).Name;
    }
    
    public OptionsConfigPathResolver(string Path) {
        this.Path = Path;
    }

    public string Path { get; init; }
}