using Panoptes.Model;
using System;

namespace Panoptes.ViewModels.Panels
{
    public sealed class LogPanelItemViewModel
    {
        public LogItemType EntryType { get; set; }
        public DateTime DateTime { get; set; }
        public string Message { get; set; }
    }
}
