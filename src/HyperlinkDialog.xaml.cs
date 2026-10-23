/// -----------------------------------------------------------------------------------------------
/// <summary>
///  新增網站連結對話框
/// </summary>
/// <remarks>
///  這個視窗讓使用者輸入網站連結的文字與網址。
/// </remarks>
/// <history>
///  2026/9/9 by Shu-Kai Yang (skyang@csie.nctu.edu.tw)
/// </history>
/// -----------------------------------------------------------------------------------------------

using System;
using System.Windows;

namespace DocsEditor
{
    public partial class HyperlinkDialog : Window
    {
        /// <summary>
        ///  頁面上的資料成員。
        /// </summary>
        public String Text = String.Empty;
        public String Url = String.Empty;

        public HyperlinkDialog()
        {   InitializeComponent();  }

        /// <summary>
        ///  把頁面上的資料抄錄到資料成員。
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(UrlTextBox.Text) == true)
            {   MessageLabel.Text = Properties.Resources.BlankUrlError;  return;  }

            Text = HyperlinkTextBox.Text;
            Url = UrlTextBox.Text;
            if (String.IsNullOrEmpty(Text) == true) {  Text = Url;  }
            DialogResult = true;
        }

        /// <summary>
        ///  不做任何事結束。
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {   DialogResult = false;  }
    }
}
