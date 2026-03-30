using MaterialDesignThemes.Wpf;
using System.Collections.Generic;

namespace Enjaz.Models
{
    public class HelpContent
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Abstract { get; set; } = string.Empty;
        public string ContentSimple { get; set; } = string.Empty;
        public string ContentAdvanced { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string IconKind { get; set; } = "HelpCircleOutline";
        public string Keywords { get; set; } = string.Empty;
        public string RelatedView { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        
        // UI Helper properties
        public PackIconKind Icon => System.Enum.TryParse<PackIconKind>(IconKind, out var kind) ? kind : PackIconKind.HelpCircleOutline;
        public bool HasVideo => !string.IsNullOrEmpty(VideoUrl);
        
        // Level-specific content retrieval
        public string GetContent(bool isAdvanced) => isAdvanced ? ContentAdvanced : ContentSimple;
    }
}
