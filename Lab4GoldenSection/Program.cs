using Lab4GoldenSection;
using System;
using System.Windows.Forms;

namespace GoldenSection {
  internal static class Program {
    [STAThread]
    static void Main() {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new Form1());
    }
  }
}
