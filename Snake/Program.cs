using System.Windows.Forms;

namespace Snake
{
    class Program
    {
        static void Main()
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SnakeForm());
        }
    }
}
