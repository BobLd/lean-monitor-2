using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Panoptes.Avalonia.Views.NewSession
{
    public partial class NewFileSessionControl : UserControl
    {
        public NewFileSessionControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
