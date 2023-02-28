using IntelligentCoder;
using System.Windows.Forms;

namespace WinFormsApp1
{
    [AsyncMethodPoster(Deep = 100)]
    public partial class Form1 : Form
    {
        public Form1()
        {
            this.InitializeComponent(); //OnAutoScaleModeChanged
            //this.OnClosedAsync(new EventArgs());
        }

        //protected Task CloseAsync()
        //{
        //    return Task.Run(() =>
        //    {
        //        this.Close();
        //    });
        //}
    }
}