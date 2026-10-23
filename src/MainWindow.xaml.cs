
/// -----------------------------------------------------------------------------------------------
/// <summary>
///  主要視窗
/// </summary>
/// <remarks>
///  這個視窗以 TextBox 控制項和 Markdown 語法編輯網站頁面，並提供關閉時自動存檔的功能。資料內容放在
///  DocProject 和 TocItem 結構體當中，因此程式的邏輯重點在於判斷什麼時候應該把資料從視窗上的控制項
///  寫回 DocProject 與 TocItem 結構體，又什麼時候應該把結構體的資料寫回檔案中。
/// </remarks>
/// <history>
///  2026/10/21 by Shu-Kai Yang (skyang@csie.nctu.edu.tw)
/// </history>
/// -----------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DocsEditor.Properties;

namespace DocsEditor
{
    public partial class MainWindow : Window
    {
        /// 目前正在編輯中文版還是英文版:
        public Boolean EditorIsChinese = false;

        #region 追蹤使用者對畫面內容的更動.
        /// 被拖曳的 item index:
        private int m_fromItemIndex = -1;
        private bool m_isDragging = false;

        /// 在使用者更動了 TextBox 和 RichTextBox 的文字的時候，這些旗標會被設為 true，而 DocProject
        /// 和 TocItem 內部的 IsModified 旗標則用來只是記憶體中的文件是否需要存檔。
        public Boolean TextBoxesAreModified = false;
        public Boolean ContextTextBoxeIsModified = false;

        private void TextBoxes_TextChanged(object sender, TextChangedEventArgs e)
        {   TextBoxesAreModified = true;  }

        private void ContextTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {   ContextTextBoxeIsModified = true;  }
        #endregion

        #region 初始化視窗並載入檔案
        public MainWindow()
        {
            App app = Application.Current as App;
            InitializeComponent();

            /// 初始化了 XAML 上的元件以後，載入先前儲存的視窗尺寸與位置。
            if (Settings.Default.WindowPos != null)
            {
                this.Left = Settings.Default.WindowPos.X;
                this.Top = Settings.Default.WindowPos.Y;
            }

            if (Settings.Default.WindowSize != null)
            {
                this.Width = Settings.Default.WindowSize.Width;
                this.Height = Settings.Default.WindowSize.Height;
            }

            /// 嘗試載入上次的工作目錄，如果不存在則建立預設的資料目錄:
            if (String.IsNullOrEmpty(app.PreferredDir) == false)
            {   app.WorkDir = app.PreferredDir;  }
            else if (String.IsNullOrEmpty(Settings.Default.LastDir) == false)
            {
                /// 檢查上次的工作目錄是否仍存在:
                if (Directory.Exists(Settings.Default.LastDir) == true)
                {   app.WorkDir = Settings.Default.LastDir;  }
            }

            if (String.IsNullOrEmpty(app.WorkDir) == true)
            {
                /// 如果未曾指定工作目錄，或該目錄不存在，則使用預設目錄:
                String homeDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                app.WorkDir = Path.Combine(homeDir, "docs");
            }
        }

        /// <summary>
        ///  將必要的檔案複製到 destDir 並設為目前的工作目錄。
        /// </summary>
        public Boolean InitializeDirectory(String destDir)
        {
            App app = Application.Current as App;
            String[] fileList = { "index.html", "style.css", "style_toc.css" };
            String[] imageFileList = { "papertex.jpg", "woodtex.gif", "back.png", "lang.png", "toc.png" };

            try
            {
                /// 如果指定的目錄不存在，則建立之:
                if (Directory.Exists(destDir) == false)
                {   Directory.CreateDirectory(destDir);  }

                /// 從程式的 Assets 中複製初始的檔案到 destDir 中:
                foreach (String fileName in fileList)
                {
                    String srcPathName = Path.Combine(app.AssetDir, fileName);
                    String destPathName = Path.Combine(destDir, fileName);
                    if (App.ShouldGenFile(srcPathName, destPathName) == true)
                    {   File.Copy(srcPathName, destPathName, true);  }
                }

                String imageDir = Path.Combine(destDir, "images");
                if (Directory.Exists(imageDir) == false)
                {   Directory.CreateDirectory(imageDir);  }

                foreach (String fileName in imageFileList)
                {
                    String srcPathName = Path.Combine(app.AssetDir, fileName);
                    String destPathName = Path.Combine(imageDir, fileName);
                    if (App.ShouldGenFile(srcPathName, destPathName) == true)
                    {   File.Copy(srcPathName, destPathName, true);  }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                MessageLabel.Text = ex.Message;
                return false;
            }

            /// 將 destDir 設定為目前應用程式的工作目錄:
            Directory.SetCurrentDirectory(destDir);
            return true;
        }

        /// <summary>
        ///  嘗試從 app.WorkDir 載入 docs.tocx 和 .md 檔案，並顯示於視窗。
        /// </summary>
        public void LoadProjectFromDirectory()
        {
            App app = Application.Current as App;
            app.Project = new DocProject();
            Boolean projectIsLoaded = false;

            /// 嘗試載入 docs.tocx:
            String pathName = Path.Combine(app.WorkDir, "docs.tocx");
            if (File.Exists(pathName) == true)
            {
                projectIsLoaded = app.Project.LoadXml();
                if (projectIsLoaded == false)
                {
                    MessageBox.Show(app.Project.LastError);
                    MessageLabel.Text = app.Project.LastError;
                }

                /// 嘗試載入 .md 檔案:
                foreach (TocItem item in app.Project.Toc)
                {
                    if (item.LoadMarkdownFiles() == false)
                    {
                        MessageBox.Show(app.Project.LastError);
                        MessageLabel.Text = app.Project.LastError;
                    }
                }
            }

            /// 無法成功地載入 docs.tocx，產生預設的內容:
            if (projectIsLoaded == false)
            {
                TocItem item = new TocItem(app.Project);
                app.Project.Toc.Add(item);
                app.Project.CurItem = item;
                app.Project.CurItemIndex = 0;
                app.Project.IsModified = true;
            }

            /// 將 app.Project 的內容抄錄到視窗上並重置相關旗標:
            HomeTitleEnTextBox.Text = app.Project.HomeTitleEn;
            HomeUrlEnTextBox.Text = app.Project.HomeUrlEn;
            HomeTitleZhTextBox.Text = app.Project.HomeTitleZh;
            HomeUrlZhTextBox.Text = app.Project.HomeUrlZh;

            ChapterListView.ItemsSource = app.Project.Toc;
            ChapterListView.SelectedItem = app.Project.CurItem;
            TextBoxesAreModified = false;

            if (EditorIsChinese == true)
            {   ContextTextBox.Text = app.Project.CurItem.ContextZh;  }
            else { ContextTextBox.Text = app.Project.CurItem.ContextEn;  }

            ContextTextBoxeIsModified = false;
        }

        /// <summary>
        ///  載入或初始化指定的工作目錄。
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            EditorIsChinese = app.UiIsChinese;

            /// 初始化指定的工作視窗(如果需要的話):
            if (InitializeDirectory(app.WorkDir) == false) {  return;  }

            /// 嘗試載入 docs.tocx 和 .xaml 檔案，並顯示於視窗
            LoadProjectFromDirectory();
        }

        /// <summary>
        ///  開啟 FolderBrowserDialog 來選擇新的工作目錄。
        /// </summary>
        private void OpenDirButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;

            /// 無論如何先儲存目前編輯中的內容:
            SaveCurrentProject();

            /// 開啟 FolderBrowserDialog 來選擇新的工作目錄
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                /// 準備指定的新工作目錄:
                if (InitializeDirectory(dialog.SelectedPath) == true)
                {
                    /// 嘗試載入 docs.tocx 和 .md 檔案，並顯示於視窗:
                    app.WorkDir = dialog.SelectedPath;
                    LoadProjectFromDirectory();
                }
            }
        }
        #endregion

        #region 切換、新增、刪除篇章
        /// ///////////////////////////////////////////////////////////////////////////////////////
        /// 在視窗標題和狀態列顯示目前編輯中的檔名和中英版本。
        private void ShowFileNameOnWindowTitle()
        {
            App app = Application.Current as App;
            StringBuilder sb = new StringBuilder(Properties.Resources.AppTitle);
            sb.Append(" - ");
            sb.Append(app.WorkDir);
            sb.Append('\\');
            sb.Append(app.Project.CurItem.Name);

            if (EditorIsChinese == true)
            {
                MessageLabel.Text = String.Format(Properties.Resources.EditingChinese, app.Project.CurItem.Name);
                sb.Append("_zh.md");
            }
            else
            {
                MessageLabel.Text = String.Format(Properties.Resources.EditingEnglish, app.Project.CurItem.Name);
                sb.Append("_en.md");
            }

            Title = sb.ToString();
        }

        /// <summary>
        ///  當使用者在 ListView 點選了其他的篇章的時候，將 ContextTextBox.Text 寫回目前編輯中的篇章 CurItem，
        ///  使用者點選的其他篇章 item 設為 CurItem 並載入其內容。
        /// </summary>
        private void ChapterListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App app = Application.Current as App;
            TocItem item = ChapterListView.SelectedItem as TocItem;
            if (item == null) {  return;  }

            if (item.Equals(app.Project.CurItem) == false)
            {
                if (ContextTextBoxeIsModified == true)
                {
                    if (EditorIsChinese == true)
                    {
                        app.Project.CurItem.ContextZh = ContextTextBox.Text;
                        app.Project.CurItem.ContextZhIsModified = true;
                    }
                    else
                    {
                        app.Project.CurItem.ContextEn = ContextTextBox.Text;
                        app.Project.CurItem.ContextEnIsModified = true;
                    }
                }

                /// 將使用者點選的 item 設為 CurItem，並載入其文字到 ContextTextBox:
                app.Project.CurItem = item;
                app.Project.CurItemIndex = app.Project.Toc.IndexOf(item);

                if (EditorIsChinese == true) {  ContextTextBox.Text = item.ContextZh;  }
                else {  ContextTextBox.Text = item.ContextEn;  }

                ContextTextBoxeIsModified = false;
            }

            /// 更新視窗標題和狀態列的顯示:
            ShowFileNameOnWindowTitle();
        }

        /// <summary>
        ///  開啟 TocItemDialog 對話框輸入中英文標題和檔名，並建立新的 TocItem 物件。
        /// </summary>
        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            TocItemDialog dialog = new TocItemDialog();

            if (dialog.ShowDialog() == true)
            {
                TocItem item = app.Project.GetItemByName(dialog.FileName);
                if (item != null)
                {   MessageLabel.Text = Properties.Resources.DuplicatedNameError; return;  }

                /// 產生新的 TocItem 項目並設為 CurItem:
                item = new TocItem(app.Project);
                item.TitleZh = dialog.TitleZh;
                item.TitleEn = dialog.TitleEn;
                item.Name = dialog.FileName;

                app.Project.Toc.Add(item);
                app.Project.IsModified = true;
                ChapterListView.SelectedItem = item;
            }
        }

        /// <summary>
        ///  開啟 TocItemDialog 對話框來修改 CurItem 項目。
        /// </summary>
        private void RenameItemButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            TocItemDialog dialog = new TocItemDialog();

            /// 將 CurItem 的內容抄錄到 dialog:
            dialog.TitleZh = app.Project.CurItem.TitleZh;
            dialog.TitleEn = app.Project.CurItem.TitleEn;
            dialog.FileName = app.Project.CurItem.Name;

            /// 開啟 dialog 修改資料:
            if (dialog.ShowDialog() == true)
            {
                app.Project.CurItem.TitleZh = dialog.TitleZh;
                app.Project.CurItem.TitleEn = dialog.TitleEn;
                app.Project.IsModified = true;

                /// 檔名有改變?
                if (app.Project.CurItem.Name.Equals(dialog.FileName) == false)
                {
                    TocItem item = app.Project.GetItemByName(dialog.FileName);
                    if (item != null) /// 如果有其他 item 已經用了這個名稱，則拒絕之:
                    {   MessageLabel.Text = Properties.Resources.DuplicatedNameError; return;  }

                    if (app.Project.CurItem.Rename(dialog.FileName) == false)
                    {   MessageLabel.Text = app.Project.LastError;  return;  }

                    /// 以防萬一，先儲存資料:
                    app.Project.SaveXml();
                }

                MessageLabel.Text = Properties.Resources.RenameOK;
            }
        }

        /// <summary>
        ///  刪除 CurItem 及其 .md 檔案。
        /// </summary>
        private void DelItemButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            if (MessageBox.Show(Properties.Resources.DeletionConfirm,
                    Properties.Resources.AppTitle, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {   return;  }

            /// 先將 CurItem 由 app.Project.Toc 中移除:
            TocItem toBeDel = app.Project.CurItem;
            int index = app.Project.CurItemIndex;
            app.Project.Toc.Remove(toBeDel);
            app.Project.IsModified = true;

            /// 先讓畫面顯示下一個 item:
            if (app.Project.Toc.Count == 0)
            {
                TocItem item = new TocItem(app.Project);
                app.Project.Toc.Add(item);
            }

            if (index >= app.Project.Toc.Count)
            {   index = app.Project.Toc.Count - 1; }

            TocItem nextItem = app.Project.Toc[index];
            ChapterListView.SelectedItem = nextItem;
            app.Project.CurItem = nextItem;
            app.Project.CurItemIndex = index;

            /// 刪除 toBeDel 的相關檔案，無論是否成功:
            String[] filePostFixes = {  "_en.md", "_en.htm", "_zh.md", "_zh.htm"  };
            foreach (String postfix in filePostFixes)
            {
                String fileName = toBeDel.Name + postfix;
                String pathName = Path.Combine(app.WorkDir, fileName);
                if (File.Exists(pathName))
                {   try {  File.Delete(pathName);  } catch { }  }
            }

            /// 立刻儲存 docs.tocx:
            app.Project.SaveXml();
        }
        #endregion

        #region 拖放篇章的順序
        /// ///////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///  在滑鼠左鍵按下的時候，取得被拖曳段落的位置 m_fromItemIndex。
        /// </summary>
        private void Chapter_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = sender as ListViewItem;
            m_fromItemIndex = ChapterListView.ItemContainerGenerator.IndexFromContainer(item);
            m_isDragging = false;
        }

        /// <summary>
        ///  為了讓點選跟拖曳的動作不要有所衝突，當滑鼠有拖動行為的時候，才開始拖曳。
        /// </summary>
        private void Chapter_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if ((m_fromItemIndex != -1) && (m_isDragging == false))
                {
                    ListViewItem item = ChapterListView.ItemContainerGenerator.ContainerFromIndex(m_fromItemIndex) as ListViewItem;
                    DataObject data = new DataObject("Chapter", m_fromItemIndex);
                    DragDrop.DoDragDrop(item, data, DragDropEffects.Move);
                    m_isDragging = true;
                }
            }
        }

        private void Chapter_DragOver(object sender, DragEventArgs e)
        {   e.Effects = DragDropEffects.Move;  }

        /// <summary>
        ///  在拖曳結束的時候，取得拖曳目標 item 的位置 toIndex，然後執行 Move 的動作。
        /// </summary>
        private void Chapter_Drop(object sender, DragEventArgs e)
        {
            App app = Application.Current as App;
            ListViewItem item = sender as ListViewItem;
            int toIndex = ChapterListView.ItemContainerGenerator.IndexFromContainer(item);

            /// 拖曳的來源是個篇章，執行的動作是將 m_fromItemIndex 的 TocItem 移動到目前的位置:
            if (e.Data.GetDataPresent("Chapter"))
            {
                if (m_fromItemIndex != toIndex)
                {
                    app.Project.Toc.Move(m_fromItemIndex, toIndex);
                    app.Project.IsModified = true;
                }
            }

            m_fromItemIndex = -1;
            m_isDragging = false;
            e.Handled = true;
        }
        #endregion

        #region 為 TextBox 中的文字加上 Markdown 語法。
        /// ///////////////////////////////////////////////////////////////////////////////////////
        /// 這些按鈕為 ContextTextBox.Text 加上 Markdown 語法，如果已經有選擇文字，則修改部分文字，
        /// 如果沒有，就在游標處插入語法範本。
        private void InsertStyleToSelectedText(String mdTag, String templateStr)
        {
            if (String.IsNullOrEmpty(ContextTextBox.SelectedText) == false)
            {   ContextTextBox.SelectedText = mdTag + ContextTextBox.SelectedText + mdTag; }
            else
            {   ContextTextBox.Text = ContextTextBox.Text.Insert(ContextTextBox.CaretIndex, mdTag + templateStr + mdTag);  }

            ContextTextBoxeIsModified = true;
        }

        /// 給選擇的文字前後加上 * 或 + 號，或插入範本文字:
        private void HtmlBoldButton_Click(object sender, RoutedEventArgs e)
        {   InsertStyleToSelectedText("**", "bold text");  }

        private void HtmlItalicButton_Click(object sender, RoutedEventArgs e)
        {   InsertStyleToSelectedText("*", "italic text");  }

        private void HtmlUnderlineButton_Click(object sender, RoutedEventArgs e)
        {   InsertStyleToSelectedText("++", "underlined text");  }

        /// 給文字段落逐行加上 - 號:
        private void HtmlUlButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(ContextTextBox.SelectedText) == false)
            {
                StringBuilder sb = new StringBuilder("\n");
                String[] lines = ContextTextBox.SelectedText.Split(new char[] { '\r', '\n' });

                foreach (String line in lines)
                {
                    if (String.IsNullOrWhiteSpace(line) == false)
                    {
                        sb.Append("- ");
                        sb.Append(line);
                        sb.Append("\n");
                    }
                }

                ContextTextBox.SelectedText = sb.ToString();
                ContextTextBoxeIsModified = true;
            }
        }

        /// 給文字段落逐行加上數字編號:
        private void HtmlOlButton_Click(object sender, RoutedEventArgs e)
        {            
            if (String.IsNullOrEmpty(ContextTextBox.SelectedText) == false)
            {
                StringBuilder sb = new StringBuilder("\n");
                String[] lines = ContextTextBox.SelectedText.Split(new char[] { '\r', '\n' });

                int lineNum = 1;

                foreach (String line in lines)
                {
                    if (String.IsNullOrWhiteSpace(line) == false)
                    {
                        sb.Append(lineNum);
                        sb.Append(" ");
                        sb.Append(line);
                        sb.Append("\n");
                        ++lineNum;
                    }
                }

                ContextTextBox.SelectedText = sb.ToString();
                ContextTextBoxeIsModified = true;
            }
        }

        /// 給段落文字加上主標題:
        private void HtmlH1Button_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(ContextTextBox.SelectedText) == false)
            {
                int len = ContextTextBox.SelectedText.Length;
                if (len < 3) {  len = 3; }

                StringBuilder sb = new StringBuilder("\n");
                sb.Append(ContextTextBox.SelectedText);
                sb.Append("\n");
                sb.Append(new String('=', len));
                sb.Append("\n");
                ContextTextBox.SelectedText = sb.ToString();
            }
            else
            {   ContextTextBox.Text = ContextTextBox.Text.Insert(ContextTextBox.CaretIndex, "\nHeading 1\n========\n");  }

            ContextTextBoxeIsModified = true;
        }

        /// 給段落文字加上副標題:
        private void HtmlH2Button_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(ContextTextBox.SelectedText) == false)
            {
                int len = ContextTextBox.SelectedText.Length;
                if (len < 3) { len = 3; }

                StringBuilder sb = new StringBuilder("\n");
                sb.Append(ContextTextBox.SelectedText);
                sb.Append("\n");
                sb.Append(new String('-', len));
                sb.Append("\n");
                ContextTextBox.SelectedText = sb.ToString();
            }
            else
            {   ContextTextBox.Text = ContextTextBox.Text.Insert(ContextTextBox.CaretIndex, "\nHeading 2\n---------\n");  }

            ContextTextBoxeIsModified = true;
        }

        /// 在段落文字前加上###:
        private void HtmlH3Button_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(ContextTextBox.SelectedText) == false)
            {   ContextTextBox.SelectedText = "\n### " + ContextTextBox.SelectedText + "\n";  }
            else
            {   ContextTextBox.Text = ContextTextBox.Text.Insert(ContextTextBox.CaretIndex, "\n### Heading 3\n");  }

            ContextTextBoxeIsModified = true;
        }

        /// 在游標處插入單行的 ---，但是前一行必須是空白行:
        private void HtmlHrButton_Click(object sender, RoutedEventArgs e)
        {
            ContextTextBox.Text = ContextTextBox.Text.Insert(ContextTextBox.CaretIndex, "\n\n---\n");
            ContextTextBoxeIsModified = true;
        }

        /// 在段落文字前加上```程式類型:
        private void HtmlPreButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(ContextTextBox.SelectedText) == false)
            {   ContextTextBox.SelectedText = "\n```html\n" + ContextTextBox.SelectedText + "\n```\n";  }
            else
            {   ContextTextBox.Text = ContextTextBox.Text.Insert(ContextTextBox.CaretIndex, "\n```html\nplain code here\n```\n");  }

            ContextTextBoxeIsModified = true;
        }

        /// 選取圖片並插入 Markdown 語法到游標位置:
        private void InsertImageButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;

            /// 開啟檔案對話框，選取照片檔案:
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Title = Properties.Resources.SelectFigureFile;
            dialog.Filter = "JPEG files (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG files (*.png)|*.png";
            dialog.InitialDirectory = Path.Combine(app.WorkDir, "images");
            dialog.RestoreDirectory = true;
            dialog.CheckFileExists = true;
            dialog.Multiselect = false;

            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                /* 取得圖片的相對路徑: */
                String imageSrc = dialog.FileName;
                if (imageSrc.StartsWith(app.WorkDir) == true)
                {   imageSrc = imageSrc.Substring(app.WorkDir.Length+1);  }
                imageSrc = imageSrc.Replace('\\', '/');

                /* 將圖片的相對路徑插入到 ContextTextBox.Text: */
                ContextTextBox.Text = ContextTextBox.Text.Insert(ContextTextBox.CaretIndex, "![alter text]("+imageSrc+")");
                ContextTextBoxeIsModified = true;
            }
        }

        /// <summary>
        ///  開啟 HyperlinkDialog 輸入連結文字與網址。
        /// </summary>
        private void InsertLinkButton_Click(object sender, RoutedEventArgs e)
        {
            HyperlinkDialog dialog = new HyperlinkDialog();

            /// 開啟 dialog 修改資料:
            if (dialog.ShowDialog() == true)
            {               
                ContextTextBox.Text = ContextTextBox.Text.Insert(ContextTextBox.CaretIndex, "[" + dialog.Text + "](" + dialog.Url + ")");
                ContextTextBoxeIsModified = true;
            }
        }

        /// <summary>
        ///  切換 EditorIsChinese 的狀態，並且載入 CurItem 不同版本的內容到 ContextTextBox 中。
        /// </summary>
        private void EditorLangButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;

            /// 將目前編輯中的文字寫回 CurItem 物件裡:
            if (ContextTextBoxeIsModified == true)
            {
                if (EditorIsChinese == true)
                {
                    app.Project.CurItem.ContextZh = ContextTextBox.Text;
                    app.Project.CurItem.ContextZhIsModified = true;
                }
                else
                {
                    app.Project.CurItem.ContextEn = ContextTextBox.Text;
                    app.Project.CurItem.ContextEnIsModified = true;
                }
            }

            /// 載入另一個語言版本的文字到 ContextTextBox:
            if (EditorIsChinese == true)
            {
                ContextTextBox.Text = app.Project.CurItem.ContextEn;
                EditorIsChinese = false;
            }
            else
            {
                ContextTextBox.Text = app.Project.CurItem.ContextZh;
                EditorIsChinese = true;
            }

            ContextTextBoxeIsModified = false;

            /// 更新視窗標題與狀態列的顯示:
            ShowFileNameOnWindowTitle();
        }
        #endregion

        #region 儲存所有檔案
        /// ///////////////////////////////////////////////////////////////////////////////////////
        /// 檢查視窗上以及記憶體中的專案內容是否經過修改，並儲存到檔案。
        private void SaveCurrentProject()
        {
            App app = Application.Current as App;
            if (app.Project == null) {  return;  }

            /// 先檢查是否需要把 TextBox 和 TextBox 的內容寫回 DocProject 和 TocItem 結構體:
            if (TextBoxesAreModified == true)
            {
                app.Project.HomeTitleEn = HomeTitleEnTextBox.Text;
                app.Project.HomeUrlEn = HomeUrlEnTextBox.Text;
                app.Project.HomeTitleZh = HomeTitleZhTextBox.Text;
                app.Project.HomeUrlZh = HomeUrlZhTextBox.Text;
                app.Project.IsModified = true;

                MessageLabel.Text = Properties.Resources.ProjectSaved;
                TextBoxesAreModified = false; 
            }

            if (ContextTextBoxeIsModified == true)
            {
                if (EditorIsChinese == true)
                {
                    app.Project.CurItem.ContextZh = ContextTextBox.Text;
                    app.Project.CurItem.ContextZhIsModified = true;
                }
                else
                {
                    app.Project.CurItem.ContextEn = ContextTextBox.Text;
                    app.Project.CurItem.ContextEnIsModified = true;
                }

                MessageLabel.Text = Properties.Resources.ProjectSaved;
                ContextTextBoxeIsModified = false;
            }

            /// 儲存更改過的中英文版篇章內容，如有錯誤則覆寫 MessageLabel:
            foreach (TocItem item in app.Project.Toc)
            {
                if (item.SaveMarkdownFiles() == false)
                {
                    MessageBox.Show(app.Project.LastError);
                    MessageLabel.Text = app.Project.LastError;
                }
            }

            /// 儲存 docs.tocx 檔案，如有錯誤則覆寫 MessageLabel:
            if (app.Project.IsModified)
            {
                if (app.Project.SaveXml() == false)
                {
                    MessageBox.Show(app.Project.LastError);
                    MessageLabel.Text = app.Project.LastError;
                }
            }
        }

        /// <summary>
        ///  在視窗要關閉之前，把更動過的專案內容存檔，並且記錄視窗尺寸與位置。
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            /// 儲存目前的專案內容:
            App app = Application.Current as App;
            SaveCurrentProject();

            /// 紀錄工作目錄:
            if (string.IsNullOrEmpty(app.WorkDir) == false)
            {   Settings.Default.LastDir = app.WorkDir;  }

            /// 紀錄視窗尺寸:
            if (this.WindowState == WindowState.Normal)
            {
                Settings.Default.WindowPos = new System.Drawing.Point((int)this.Left, (int)this.Top);
                Settings.Default.WindowSize = new System.Drawing.Size((int)this.Width, (int)this.Height);
            }
            else
            {
                Settings.Default.WindowPos = new System.Drawing.Point((int)RestoreBounds.Left, (int)RestoreBounds.Top);
                Settings.Default.WindowSize = new System.Drawing.Size((int)RestoreBounds.Size.Width, (int)RestoreBounds.Size.Height);
            }

            Settings.Default.Save();
        }
        #endregion

        #region 輸出 HTML 檔案。
        /// ///////////////////////////////////////////////////////////////////////////////////////
        /// 檢查視窗上以及記憶體中的專案內容是否經過修改，並儲存到檔案。
        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;

            /// 先檢查是否需要把 TextBox 和 RichTextBox 的內容寫回 DocProject 和 TocItem 結構體:
            if (TextBoxesAreModified == true)
            {
                app.Project.HomeTitleEn = HomeTitleEnTextBox.Text;
                app.Project.HomeUrlEn = HomeUrlEnTextBox.Text;
                app.Project.HomeTitleZh = HomeTitleZhTextBox.Text;
                app.Project.HomeUrlZh = HomeUrlZhTextBox.Text;
                TextBoxesAreModified = false;

                /// 目錄經過變更則儲存之(同時刪除已生成的 .htm 檔案):
                app.Project.SaveXml();
            }

            if (ContextTextBoxeIsModified == true)
            {
                if (EditorIsChinese == true)
                {
                    app.Project.CurItem.ContextZh = ContextTextBox.Text;
                    app.Project.CurItem.ContextZhIsModified = true;
                }
                else
                {
                    app.Project.CurItem.ContextEn = ContextTextBox.Text;
                    app.Project.CurItem.ContextEnIsModified = true;
                }

                ContextTextBoxeIsModified = false;
            }

            /// 重新生成單一的 .htm 檔案:
            if (app.Project.SaveHtmlFile(app.Project.CurItem, EditorIsChinese) == false)
            {   MessageLabel.Text = app.Project.LastError;  return;  }
            MessageLabel.Text = Properties.Resources.Done;

            /// 開啟網頁瀏覽器來檢視生成好的檔案:
            String fileName = "index.html";
            if (EditorIsChinese == true)
            {   fileName = app.Project.CurItem.Name + "_zh.htm";  }
            else {  fileName = app.Project.CurItem.Name + "_en.htm";  }

            String pathName = Path.Combine(app.WorkDir, fileName);
            String fileUrl = "file:///" + pathName.Replace('\\', '/');

            try
            {
                ProcessStartInfo info = new ProcessStartInfo("iexplore.exe", "\"" + fileUrl + "\"");
                Process.Start(info);
            }
            catch (Exception ex)
            {
                MessageLabel.Text = ex.Message;
                MessageBox.Show(ex.Message);
            }
        }

        private void ExportHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            /// 儲存目前的專案內容:
            App app = Application.Current as App;
            SaveCurrentProject();

            /// 重新生成所有的 .htm 檔案:
            foreach (TocItem item in app.Project.Toc)
            {
                if (app.Project.SaveHtmlFile(item, true) == false)
                {   MessageLabel.Text = app.Project.LastError;  return;  }

                if (app.Project.SaveHtmlFile(item, false) == false)
                {   MessageLabel.Text = app.Project.LastError;  return;  }
            }

            MessageLabel.Text = Properties.Resources.Done;

            /// 開啟網頁瀏覽器來檢視生成好的檔案:
            String fileName = "index.html";
            if (EditorIsChinese == true)
            {   fileName = app.Project.Toc[0].Name + "_zh.htm";  }
            else {  fileName = app.Project.Toc[0].Name + "_en.htm";  }

            String pathName = Path.Combine(app.WorkDir, fileName);
            String fileUrl = "file:///" + pathName.Replace('\\', '/');

            try
            {
                ProcessStartInfo info = new ProcessStartInfo("iexplore.exe", "\"" + fileUrl + "\"");
                Process.Start(info);
            }
            catch (Exception ex)
            {
                MessageLabel.Text = ex.Message;
                MessageBox.Show(ex.Message);
            }
        }
        #endregion
    }
}
