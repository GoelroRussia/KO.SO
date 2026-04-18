
namespace SteelBar.Models.CopyState
{
    public static class StateClipboard
    {
        public static ViewStateData CopiedState { get; set; } = null!;
        public static bool HasData => true;
    }
}