using Panoptes.ViewModels.NewSession;
using System.Windows.Controls;

namespace Panoptes.View.NewSession
{
    /// <summary>
    /// Interaction logic for NewMongoSessionControl.xaml
    /// </summary>
    public partial class NewMongoSessionControl : UserControl
    {
        public NewMongoSessionControl()
        {
            InitializeComponent();
        }

        private void PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext != null)
            {

                ((NewMongoSessionViewModel)DataContext).Password = ((PasswordBox)sender).Password; //.SecurePassword;
            }
        }
    }
}
