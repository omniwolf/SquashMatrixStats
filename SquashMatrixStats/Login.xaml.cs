using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SquashMatrixStats {
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window {

        MatrixInterface mi;

        public Login(MatrixInterface _mi) {
            InitializeComponent();
            mi = _mi;
        }

        private void button_Click(object sender, RoutedEventArgs e) {
            if (UserInput.Text.Length > 0 && PassInput.Text.Length > 0) {
                mi.setUser(UserInput.Text);
                mi.setPass(PassInput.Text);
                this.Close();
            }
            else {
                MessageBox.Show("gotta type something in..");
            }
        }
    }
}
