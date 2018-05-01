using System.Windows.Controls;


namespace localshare
{
    /// <summary>
    /// Logica di interazione per UserList.xaml
    /// </summary>
    public partial class UserList : UserControl
    {
        Button SelectAllButton;

        public UserList(Button SelectAllButton)
        {
            InitializeComponent();

            this.SelectAllButton = SelectAllButton;
        }

        /// <summary>
        /// Event handler for the selection changed event, change the text of the SelectAllButton
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UsersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.UsersListView.SelectedItems.Count == this.UsersListView.Items.Count)
                this.SelectAllButton.Content = "DESELECT ALL";
        }
    }
}
