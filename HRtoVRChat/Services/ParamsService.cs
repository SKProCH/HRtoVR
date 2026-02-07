using System;
using System.Collections.Generic;

namespace HRtoVRChat.Services;

public interface IParamsService
{
    void InitParams();
    void ResetParams();
    void UpdateHRValues(HROutput hro);
    void UpdateHeartBeat(bool isHeartBeat);
}

public class ParamsService : IParamsService
{
    private readonly IConfigService _configService;
    private readonly IOSCService _oscService;

    public List<IHRParameter> Parameters = new();

    public ParamsService(IConfigService configService, IOSCService oscService)
    {
        _configService = configService;
        _oscService = oscService;
    }

    public void InitParams()
    {
        Parameters.Add(new IntParameter(hro => hro.ones, _configService.LoadedConfig.ParameterNames["onesHR"],
            "onesHR", _oscService));
        Parameters.Add(new IntParameter(hro => hro.tens, _configService.LoadedConfig.ParameterNames["tensHR"],
            "tensHR", _oscService));
        Parameters.Add(new IntParameter(hro => hro.hundreds,
            _configService.LoadedConfig.ParameterNames["hundredsHR"], "hundredsHR", _oscService));
        Parameters.Add(new IntParameter(hro =>
        {
            var HRstring = $"{hro.hundreds}{hro.tens}{hro.ones}";
            var HR = 0;
            try
            {
                HR = Convert.ToInt32(HRstring);
            }
            catch (Exception)
            {
            }

            if (HR > 255)
                HR = 255;
            if (HR < 0)
                HR = 0;
            return HR;
        }, _configService.LoadedConfig.ParameterNames["HR"], "HR", _oscService));
        Parameters.Add(new FloatParameter(hro =>
        {
            var targetFloat = 0f;
            var maxhr = (float)_configService.LoadedConfig.MaxHR;
            var minhr = (float)_configService.LoadedConfig.MinHR;
            var HR = (float)hro.HR;
            if (HR > maxhr)
                targetFloat = 1;
            else if (HR < minhr)
                targetFloat = 0;
            else
                targetFloat = (HR - minhr) / (maxhr - minhr);
            return targetFloat;
        }, _configService.LoadedConfig.ParameterNames["HRPercent"], "HRPercent", _configService, _oscService));
        Parameters.Add(new FloatParameter(hro =>
        {
            var targetFloat = 0f;
            var maxhr = (float)_configService.LoadedConfig.MaxHR;
            var minhr = (float)_configService.LoadedConfig.MinHR;
            var HR = (float)hro.HR;
            if (HR > maxhr)
                targetFloat = 1;
            else if (HR < minhr)
                targetFloat = 0;
            else
                targetFloat = (HR - minhr) / (maxhr - minhr);
            return 2f * targetFloat - 1f;
        }, _configService.LoadedConfig.ParameterNames["FullHRPercent"], "FullHRPercent", _configService, _oscService));
        Parameters.Add(new BoolParameter(hro => hro.isActive,
            _configService.LoadedConfig.ParameterNames["isHRActive"], "isHRActive", _oscService));
        Parameters.Add(new BoolParameter(hro => hro.isConnected,
            _configService.LoadedConfig.ParameterNames["isHRConnected"], "isHRConnected", _oscService));
        Parameters.Add(
            new BoolParameter(BoolCheckType.HeartBeat, _configService.LoadedConfig.ParameterNames["isHRBeat"], _oscService));
    }

    public void ResetParams()
    {
        var paramcount = Parameters.Count;
        foreach (var hrParameter in Parameters)
            hrParameter.UpdateParameter(true);
        Parameters.Clear();
        LogHelper.Debug($"Cleared {paramcount} parameters!");
    }

    public void UpdateHRValues(HROutput hro)
    {
        foreach (var parameter in Parameters)
        {
            parameter.Update(hro);
        }
    }

    public void UpdateHeartBeat(bool isHeartBeat)
    {
        foreach (var parameter in Parameters)
        {
            if (parameter is BoolParameter boolParam)
            {
                boolParam.UpdateHeartBeat(isHeartBeat);
            }
        }
    }

    public class IntParameter : IHRParameter
    {
        private Func<HROutput, int> _getVal;
        private readonly IOSCService _oscService;

        public IntParameter(Func<HROutput, int> getVal, string parameterName, string original, IOSCService oscService)
        {
            OriginalParameterName = original;
            ParameterName = parameterName;
            _getVal = getVal;
            _oscService = oscService;
            ParamValue = "0";
            LogHelper.Debug($"IntParameter with ParameterName: {parameterName}, has been created!");
        }

        public string OriginalParameterName { get; set; }
        public string ParameterName { get; set; }
        public string ParamValue { get; set; }

        public string DefaultValue
        {
            get => "0";
        }

        public void Update(HROutput hro)
        {
            var val = _getVal.Invoke(hro);
            if (ParamValue != val.ToString())
            {
                ParamValue = val.ToString();
                UpdateParameter();
            }
        }

        public void UpdateParameter(bool fromReset = false)
        {
            _oscService.SendMessage(ParameterName, fromReset ? Convert.ToInt32(DefaultValue) : Convert.ToInt32(ParamValue));
        }
    }

    public class BoolParameter : IHRParameter
    {
        private Func<HROutput, bool>? _getVal;
        private BoolCheckType? _bct;
        private readonly IOSCService _oscService;

        public BoolParameter(Func<HROutput, bool> getVal, string parameterName, string original, IOSCService oscService)
        {
            OriginalParameterName = original;
            ParameterName = parameterName;
            _getVal = getVal;
            _oscService = oscService;
            ParamValue = "false";
            LogHelper.Debug($"BoolParameter with ParameterName: {parameterName}, has been created!");
        }

        public BoolParameter(BoolCheckType bct, string parameterName, IOSCService oscService)
        {
            switch (bct)
            {
                case BoolCheckType.HeartBeat:
                    OriginalParameterName = "isHRBeat";
                    break;
                default:
                    OriginalParameterName = parameterName;
                    break;
            }

            _bct = bct;
            ParameterName = parameterName;
            _oscService = oscService;
            ParamValue = "false";
            LogHelper.Debug(
                $"BoolParameter with ParameterName: {parameterName} and BoolCheckType of: {bct}, has been created!");
        }

        public string OriginalParameterName { get; set; }
        public string ParameterName { get; set; }
        public string ParamValue { get; set; }

        public string DefaultValue
        {
            get => "false";
        }

        public void Update(HROutput hro)
        {
            if (_getVal != null)
            {
                var val = _getVal.Invoke(hro);
                if (ParamValue != val.ToString().ToLower())
                {
                    ParamValue = val.ToString().ToLower();
                    UpdateParameter();
                }
            }
        }

        public void UpdateHeartBeat(bool isHeartBeat)
        {
            if (_bct == BoolCheckType.HeartBeat)
            {
                ParamValue = isHeartBeat.ToString().ToLower();
                UpdateParameter();
            }
        }

        public void UpdateParameter(bool fromReset = false)
        {
            _oscService.SendMessage(ParameterName, fromReset ? Convert.ToBoolean(DefaultValue) : Convert.ToBoolean(ParamValue));
        }
    }

    public class FloatParameter : IHRParameter
    {
        private Func<HROutput, float> _getVal;
        private readonly IConfigService _configService;
        private readonly IOSCService _oscService;

        public FloatParameter(Func<HROutput, float> getVal, string parameterName, string original, IConfigService configService, IOSCService oscService)
        {
            OriginalParameterName = original;
            ParameterName = parameterName;
            _getVal = getVal;
            _configService = configService;
            _oscService = oscService;
            ParamValue = "0";
            LogHelper.Debug($"FloatParameter with ParameterName: {parameterName} has been created!");
        }

        public string OriginalParameterName { get; set; }
        public string ParameterName { get; set; }
        public string ParamValue { get; set; }

        public string DefaultValue
        {
            get => "0";
        }

        public void Update(HROutput hro)
        {
            var val = _getVal.Invoke(hro);
            if (ParamValue != val.ToString())
            {
                ParamValue = val.ToString();
                UpdateParameter();
            }
        }

        public void UpdateParameter(bool fromReset = false)
        {
            _oscService.SendMessage(ParameterName, fromReset ? Convert.ToSingle(DefaultValue) : Convert.ToSingle(ParamValue));
        }
    }
}
