/// -----------------------------------------------------------------------------------------------
/// <summary>
///  篇章新增或改名對話框
/// </summary>
/// <remarks>
///  這個視窗讓使用者可以新增或修改 TocItem 項目的標題與檔名。
/// </remarks>
/// <history>
///  2026/5/20 by Shu-Kai Yang (skyang@csie.nctu.edu.tw)
/// </history>
/// -----------------------------------------------------------------------------------------------

using System;
using System.Windows;

namespace DocsEditor
{
    public partial class TocItemDialog : Window
    {
        /// <summary>
        ///  頁面上的資料成員。
        /// </summary>
        public String TitleEn = String.Empty;
        public String TitleZh = String.Empty;
        public String FileName = String.Empty;

        public TocItemDialog()
        {   InitializeComponent();  }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TitleEnTextBox.Text = TitleEn;
            TitleZhTextBox.Text = TitleZh;
            FileNameTextBox.Text = FileName;
        }

        /// <summary>
        ///  把頁面上的資料抄錄到資料成員。
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(FileNameTextBox.Text) == true)
            {   MessageLabel.Text = Properties.Resources.BlankNameError;  return;  }

            TitleEn = TitleEnTextBox.Text;
            TitleZh = TitleZhTextBox.Text;
            FileName = FileNameTextBox.Text;
            DialogResult = true;
        }

        /// <summary>
        ///  不做任何事結束。
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {   DialogResult = false;  }
    }
}
