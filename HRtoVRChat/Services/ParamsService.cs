using System;
using System.Collections.Generic;
using HRtoVRChat.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRtoVRChat.Services;

public interface IParamsService
{
    void InitParams();
    void ResetParams();
    void UpdateHRValues(HROutput hro);
    void UpdateHeartBeat(bool isHeartBeat);
    void ForceResendValues();
}

public class ParamsService : IParamsService
{
    private readonly IOptionsMonitor<AppOptions> _appOptions;
    private readonly IOSCService _oscService;
    private readonly ILogger<ParamsService> _logger;

    public List<IHRParameter> Parameters = new();

    public ParamsService(IOptionsMonitor<AppOptions> appOptions, IOSCService oscService, ILogger<ParamsService> logger)
    {
        _appOptions = appOptions;
        _oscService = oscService;
        _logger = logger;
    }

    public void InitParams()
    {
        Parameters.Add(new IntParameter(hro => hro.ones, _appOptions.CurrentValue.ParameterNames.OnesHR,
            "onesHR", _oscService, _logger));
        Parameters.Add(new IntParameter(hro => hro.tens, _appOptions.CurrentValue.ParameterNames.TensHR,
            "tensHR", _oscService, _logger));
        Parameters.Add(new IntParameter(hro => hro.hundreds,
            _appOptions.CurrentValue.ParameterNames.HundredsHR, "hundredsHR", _oscService, _logger));
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
        }, _appOptions.CurrentValue.ParameterNames.HR, "HR", _oscService, _logger));
        Parameters.Add(new FloatParameter(hro =>
        {
            var targetFloat = 0f;
            var maxhr = (float)_appOptions.CurrentValue.MaxHR;
            var minhr = (float)_appOptions.CurrentValue.MinHR;
            var HR = (float)hro.HR;
            if (HR > maxhr)
                targetFloat = 1;
            else if (HR < minhr)
                targetFloat = 0;
            else
                targetFloat = (HR - minhr) / (maxhr - minhr);
            return targetFloat;
        }, _appOptions.CurrentValue.ParameterNames.HRPercent, "HRPercent", _appOptions, _oscService, _logger));
        Parameters.Add(new FloatParameter(hro =>
        {
            var targetFloat = 0f;
            var maxhr = (float)_appOptions.CurrentValue.MaxHR;
            var minhr = (float)_appOptions.CurrentValue.MinHR;
            var HR = (float)hro.HR;
            if (HR > maxhr)
                targetFloat = 1;
            else if (HR < minhr)
                targetFloat = 0;
            else
                targetFloat = (HR - minhr) / (maxhr - minhr);
            return 2f * targetFloat - 1f;
        }, _appOptions.CurrentValue.ParameterNames.FullHRPercent, "FullHRPercent", _appOptions, _oscService, _logger));
        Parameters.Add(new BoolParameter(hro => hro.isActive,
            _appOptions.CurrentValue.ParameterNames.IsHRActive, "isHRActive", _oscService, _logger));
        Parameters.Add(new BoolParameter(hro => hro.isConnected,
            _appOptions.CurrentValue.ParameterNames.IsHRConnected, "isHRConnected", _oscService, _logger));
        Parameters.Add(
            new BoolParameter(BoolCheckType.HeartBeat, _appOptions.CurrentValue.ParameterNames.IsHRBeat, _oscService, _logger));
    }

    public void ResetParams()
    {
        var paramcount = Parameters.Count;
        foreach (var hrParameter in Parameters)
            hrParameter.UpdateParameter(true);
        Parameters.Clear();
        _logger.LogDebug("Cleared {ParamCount} parameters!", paramcount);
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

    public void ForceResendValues()
    {
        foreach (var parameter in Parameters)
        {
            parameter.UpdateParameter();
        }
    }

    public class IntParameter : IHRParameter
    {
        private Func<HROutput, int> _getVal;
        private readonly IOSCService _oscService;
        private readonly ILogger _logger;

        public IntParameter(Func<HROutput, int> getVal, string parameterName, string original, IOSCService oscService, ILogger logger)
        {
            OriginalParameterName = original;
            ParameterName = parameterName;
            _getVal = getVal;
            _oscService = oscService;
            _logger = logger;
            ParamValue = "0";
            _logger.LogDebug("IntParameter with ParameterName: {ParameterName}, has been created!", parameterName);
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
        private readonly ILogger _logger;

        public BoolParameter(Func<HROutput, bool> getVal, string parameterName, string original, IOSCService oscService, ILogger logger)
        {
            OriginalParameterName = original;
            ParameterName = parameterName;
            _getVal = getVal;
            _oscService = oscService;
            _logger = logger;
            ParamValue = "false";
            _logger.LogDebug("BoolParameter with ParameterName: {ParameterName}, has been created!", parameterName);
        }

        public BoolParameter(BoolCheckType bct, string parameterName, IOSCService oscService, ILogger logger)
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
            _logger = logger;
            ParamValue = "false";
            _logger.LogDebug(
                "BoolParameter with ParameterName: {ParameterName} and BoolCheckType of: {Bct}, has been created!", parameterName, bct);
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
        private readonly IOptionsMonitor<AppOptions> _appOptions;
        private readonly IOSCService _oscService;
        private readonly ILogger _logger;

        public FloatParameter(Func<HROutput, float> getVal, string parameterName, string original, IOptionsMonitor<AppOptions> appOptions, IOSCService oscService, ILogger logger)
        {
            OriginalParameterName = original;
            ParameterName = parameterName;
            _getVal = getVal;
            _appOptions = appOptions;
            _oscService = oscService;
            _logger = logger;
            ParamValue = "0";
            _logger.LogDebug("FloatParameter with ParameterName: {ParameterName} has been created!", parameterName);
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
