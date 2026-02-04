using System;
using System.Collections.Generic;

namespace HRtoVRChat.ViewModels
{
    public record HRTypeExtraInfo(string name, string description, string example, Type to)
    {
        public string AppliedValue { get; set; } = "";
    }

    public record HRTypeSelector(string Name)
    {
        public List<HRTypeExtraInfo> ExtraInfos { get; init; } = new();
    }
}
