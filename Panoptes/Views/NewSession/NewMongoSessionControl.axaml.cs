using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Panoptes.Views.NewSession
{
    public partial class NewMongoSessionControl : UserControl
    {
        public NewMongoSessionControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
