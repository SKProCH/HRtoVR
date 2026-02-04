namespace HRtoVRChat_OSC;

public static class ParamsManager {
    public enum BoolCheckType {
        HeartBeat
    }

    public static List<HRParameter> Parameters = new();

    public static void InitParams() {
        Parameters.Add(new IntParameter(hro => hro.ones, ConfigManager.LoadedConfig.ParameterNames["onesHR"],
            "onesHR"));
        Parameters.Add(new IntParameter(hro => hro.tens, ConfigManager.LoadedConfig.ParameterNames["tensHR"],
            "tensHR"));
        Parameters.Add(new IntParameter(hro => hro.hundreds,
            ConfigManager.LoadedConfig.ParameterNames["hundredsHR"], "hundredsHR"));
        Parameters.Add(new IntParameter(hro => {
            var HRstring = $"{hro.hundreds}{hro.tens}{hro.ones}";
            var HR = 0;
            try {
                HR = Convert.ToInt32(HRstring);
            }
            catch (Exception) {
            }

            if (HR > 255)
                HR = 255;
            if (HR < 0)
                HR = 0;
            return HR;
        }, ConfigManager.LoadedConfig.ParameterNames["HR"], "HR"));
        Parameters.Add(new FloatParameter(hro => {
            var targetFloat = 0f;
            var maxhr = (float)ConfigManager.LoadedConfig.MaxHR;
            var minhr = (float)ConfigManager.LoadedConfig.MinHR;
            var HR = (float)hro.HR;
            if (HR > maxhr)
                targetFloat = 1;
            else if (HR < minhr)
                targetFloat = 0;
            else
                targetFloat = (HR - minhr) / (maxhr - minhr);
            return targetFloat;
        }, ConfigManager.LoadedConfig.ParameterNames["HRPercent"], "HRPercent"));
        Parameters.Add(new FloatParameter(hro => {
            var targetFloat = 0f;
            var maxhr = (float)ConfigManager.LoadedConfig.MaxHR;
            var minhr = (float)ConfigManager.LoadedConfig.MinHR;
            var HR = (float)hro.HR;
            if (HR > maxhr)
                targetFloat = 1;
            else if (HR < minhr)
                targetFloat = 0;
            else
                targetFloat = (HR - minhr) / (maxhr - minhr);
            return 2f * targetFloat - 1f;
        }, ConfigManager.LoadedConfig.ParameterNames["FullHRPercent"], "FullHRPercent"));
        Parameters.Add(new BoolParameter(hro => hro.isActive,
            ConfigManager.LoadedConfig.ParameterNames["isHRActive"], "isHRActive"));
        Parameters.Add(new BoolParameter(hro => hro.isConnected,
            ConfigManager.LoadedConfig.ParameterNames["isHRConnected"], "isHRConnected"));
        Parameters.Add(
            new BoolParameter(BoolCheckType.HeartBeat, ConfigManager.LoadedConfig.ParameterNames["isHRBeat"]));
    }

    public static void ResetParams() {
        var paramcount = Parameters.Count;
        foreach (var hrParameter in Parameters)
            hrParameter.UpdateParameter(true);
        Parameters.Clear();
        LogHelper.Debug($"Cleared {paramcount} parameters!");
    }

    public static void UpdateHRValues(HROutput hro) {
        foreach (var parameter in Parameters) {
            parameter.Update(hro);
        }
    }

    public static void UpdateHeartBeat(bool isHeartBeat) {
        foreach (var parameter in Parameters) {
            if (parameter is BoolParameter boolParam) {
                boolParam.UpdateHeartBeat(isHeartBeat);
            }
        }
    }

    public class IntParameter : HRParameter {
        private Func<HROutput, int> _getVal;

        public IntParameter(Func<HROutput, int> getVal, string parameterName, string original) {
            OriginalParameterName = original;
            ParameterName = parameterName;
            _getVal = getVal;
            LogHelper.Debug($"IntParameter with ParameterName: {parameterName}, has been created!");
        }

        public string OriginalParameterName { get; set; }
        public string ParameterName { get; set; }
        public string ParamValue { get; set; }

        public string DefaultValue {
            get => "0";
        }

        public void Update(HROutput hro) {
             var valueToSet = _getVal.Invoke(hro);
             ParamValue = valueToSet.ToString();
             UpdateParameter();
        }

        public void UpdateParameter(bool fromReset = false) {
            var val = ParamValue;
            if (fromReset)
                val = DefaultValue;
            OSCManager.SendMessage("/avatar/parameters/" + ParameterName, Convert.ToInt32(val));
        }
    }

    public class BoolParameter : HRParameter {
        private Func<HROutput, bool> _getVal;
        private BoolCheckType? _bct;

        public BoolParameter(Func<HROutput, bool> getVal, string parameterName, string original) {
            OriginalParameterName = original;
            ParameterName = parameterName;
            _getVal = getVal;
            LogHelper.Debug($"BoolParameter with ParameterName: {parameterName}, has been created!");
        }

        public BoolParameter(BoolCheckType bct, string parameterName) {
            switch (bct) {
                case BoolCheckType.HeartBeat:
                    OriginalParameterName = "isHRBeat";
                    break;
            }
            _bct = bct;
            ParameterName = parameterName;
            LogHelper.Debug(
                $"BoolParameter with ParameterName: {parameterName} and BoolCheckType of: {bct}, has been created!");
        }

        public string OriginalParameterName { get; set; }
        public string ParameterName { get; set; }
        public string ParamValue { get; set; }

        public string DefaultValue {
            get => "false";
        }

        public void Update(HROutput hro) {
            if (_getVal != null) {
                var valueToSet = _getVal.Invoke(hro);
                ParamValue = valueToSet.ToString();
                UpdateParameter();
            }
        }

        public void UpdateHeartBeat(bool isHeartBeat) {
            if (_bct == BoolCheckType.HeartBeat) {
                ParamValue = isHeartBeat.ToString();
                UpdateParameter();
            }
        }

        public void UpdateParameter(bool fromReset = false) {
            var val = ParamValue;
            if (fromReset)
                val = DefaultValue;
            OSCManager.SendMessage("/avatar/parameters/" + ParameterName, Convert.ToBoolean(val));
        }
    }

    public class FloatParameter : HRParameter {
        private Func<HROutput, float> _getVal;

        public FloatParameter(Func<HROutput, float> getVal, string parameterName, string original) {
            OriginalParameterName = original;
            ParameterName = parameterName;
            _getVal = getVal;
            LogHelper.Debug($"FloatParameter with ParameterName: {parameterName} has been created!");
        }

        public string OriginalParameterName { get; set; }
        public string ParameterName { get; set; }
        public string ParamValue { get; set; }

        public string DefaultValue {
            get => "0";
        }

        public void Update(HROutput hro) {
            var targetValue = _getVal.Invoke(hro);
            ParamValue = targetValue.ToString();
            UpdateParameter();
        }

        public void UpdateParameter(bool fromReset = false) {
            var val = ParamValue;
            if (fromReset)
                val = DefaultValue;
            OSCManager.SendMessage("/avatar/parameters/" + ParameterName, (float)Convert.ToDouble(val));
        }
    }

    public class HROutput {
        public int HR;
        public int hundreds;
        public bool isActive;
        public bool isConnected;
        public int ones;
        public int tens;
    }

    public interface HRParameter {
        string OriginalParameterName { get; set; }
        string ParameterName { get; set; }
        string ParamValue { get; set; }
        string DefaultValue { get; }
        void Update(HROutput hro);
        void UpdateParameter(bool fromReset = false);
    }
}