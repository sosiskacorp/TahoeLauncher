using System;
using System.Windows.Media;

namespace MacOSTahoe.Models
{
    public class SpotlightResultItem
    {
        public string Title;
        public string Subtitle;
        public string Badge;
        public ImageSource Icon;
        public Action OnExecute;
        public Action OnAdminExecute;
        public string FileLocation;
    }
}
