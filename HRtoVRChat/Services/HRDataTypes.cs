namespace HRtoVRChat.Services;

public class HROutput {
    public int HR;
    public int hundreds;
    public bool isActive;
    public bool isConnected;
    public int ones;
    public int tens;
}

public enum BoolCheckType {
    HeartBeat
}

public interface IHRParameter {
    string OriginalParameterName { get; set; }
    string ParameterName { get; set; }
    string ParamValue { get; set; }
    string DefaultValue { get; }
    void Update(HROutput hro);
    void UpdateParameter(bool fromReset = false);
}
