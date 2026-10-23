/// -----------------------------------------------------------------------------------------------
/// <summary>
///  文件頁面
/// </summary>
/// <remarks>
///  這個類別包含了某個文件頁面的中英文標題 TitleEn / TitleZh 與文件內容 ContextEn / ContextZh，其
///  中文件都是 Markdown 語法的字串。
/// </remarks>
/// <history>
///  2026/9/5 by Shu-Kai Yang (skyang@csie.nctu.edu.tw)
/// </history>
/// -----------------------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;

namespace DocsEditor
{
    public sealed class TocItem : INotifyPropertyChanged
    {
        /// 這個頁面屬於哪個文件，在建構式中指定以後就不可改變:
        private DocProject m_doc;

        /// 頁面名稱，將會儲存為 Name_en.md 和 Name_zh.md 並用於生成 Name_en.htm 和 Name_zh.htm。
        private String m_name = "index";
        public String Name
        {
            get  {  return m_name;  }
            set  {  m_name = value;  OnPropertyChanged("Name");  }
        }

        /// 頁面的中英文版標題:
        private String m_itleEn = "Home";
        private String m_itleZh = "首頁";

        public String TitleEn
        {
            get  {  return m_itleEn;  }
            set  {  m_itleEn = value;  OnPropertyChanged("Title");  }
        }

        public String TitleZh
        {
            get  {  return m_itleZh;  }
            set  {  m_itleZh = value;  OnPropertyChanged("Title");  }
        }

        public String Title
        {
            get
            {
                App app = Application.Current as App;
                MainWindow wnd = app.MainWindow as MainWindow;
                if (wnd.EditorIsChinese) {  return TitleZh;  }
                return TitleEn;
            }
        }

        /// 頁面的中英文內容:
        public String ContextEn = String.Empty;
        public String ContextZh = String.Empty;

        /// 這個頁面是否被更動過:
        public Boolean ContextEnIsModified = false;
        public Boolean ContextZhIsModified = false;

        /// <summary>
        ///  實作 INotifyPropertyChanged 介面。
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {   handler(this, new PropertyChangedEventArgs(propertyName));  }
        }

        public TocItem(DocProject doc)
        {   m_doc = doc;  }

        /// <summary>
        ///  修改 Name 的時候，同步地更改相關的檔案名稱。
        /// </summary>
        public Boolean Rename(String newName)
        {
            App app = Application.Current as App;

            /// 新舊名稱相同，不做任何事:
            if (newName.Equals(Name)) {  return true;  }

            String oldFileName = Name + "_en.md";
            String oldPathName = Path.Combine(app.WorkDir, oldFileName);

            /// 如果英文版的舊檔案存在，則複製成新檔案:
            if (File.Exists(oldPathName))
            {
                String newFileName = newName + "_en.md";
                String newPathName = Path.Combine(app.WorkDir, newFileName);

                try
                {   File.Move(oldPathName, newPathName);  }
                catch (Exception ex)
                {   m_doc.LastError = ex.Message;  return false;  }
            }

            /// 如果中文版的舊檔案存在，則複製成新檔案:
            oldFileName = Name + "_zh.md";
            oldPathName = Path.Combine(app.WorkDir, oldFileName);

            if (File.Exists(oldPathName))
            {
                String newFileName = newName + "_zh.md";
                String newPathName = Path.Combine(app.WorkDir, newFileName);

                try
                {   File.Move(oldPathName, newPathName);  }
                catch (Exception ex)
                {   m_doc.LastError = ex.Message;  return false;  }
            }

            /// 刪除舊檔案:
            oldFileName = Name + "_en.htm";
            oldPathName = Path.Combine(app.WorkDir, oldFileName);
            if (File.Exists(oldPathName))
            {   try { File.Delete(oldPathName); } catch { }  }

            oldFileName = Name + "_zh.htm";
            oldPathName = Path.Combine(app.WorkDir, oldFileName);
            if (File.Exists(oldPathName))
            {   try { File.Delete(oldPathName); } catch { }  }

            Name = newName;
            m_doc.IsModified = true;
            return true;
        }

        #region 載入與儲存 .md 檔案.
        /// ///////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///  從 .md 檔案中讀取 Markdown 內容到 ContextXX 中。
        /// </summary>
        public Boolean LoadMarkdownFiles()
        {
            App app = Application.Current as App;
            String fileName = Name + "_en.md";
            String pathName = Path.Combine(app.WorkDir, fileName);

            if (File.Exists(pathName))
            {
                /// 以 UTF-8 編碼載入文字檔案到 ContextEn:
                try
                {   ContextEn = File.ReadAllText(pathName, Encoding.UTF8);  }
                catch (Exception ex)
                {
                    m_doc.LastError = ex.Message;
                    return false;
                }

                ContextEnIsModified = false;
            }

            fileName = Name + "_zh.md";
            pathName = Path.Combine(app.WorkDir, fileName);

            if (File.Exists(pathName))
            {
                /// 以 UTF-8 編碼載入文字檔案到 ContextZh:
                try
                {   ContextZh = File.ReadAllText(pathName, Encoding.UTF8);  }
                catch (Exception ex)
                {
                    m_doc.LastError = ex.Message;
                    return false;
                }

                ContextZhIsModified = false;
            }

            return true;
        }

        /// <summary>
        ///  檢查 ContextXXIsModified 旗標並儲存 .md 檔案。
        /// </summary>
        public Boolean SaveMarkdownFiles()
        {
            App app = Application.Current as App;

            if (ContextEnIsModified == true)
            {
                String fileName = Name + "_en.md";
                String pathName = Path.Combine(app.WorkDir, fileName);

                /// 將 ContextEn 的內容以 UTF-8 編碼寫入文字檔案:
                try
                {   File.WriteAllText(pathName, ContextEn, Encoding.UTF8);  }
                catch (Exception ex)
                {
                    m_doc.LastError = ex.Message;
                    return false;
                }

                ContextEnIsModified = false;
            }

            if (ContextZhIsModified == true)
            {
                String fileName = Name + "_zh.md";
                String pathName = Path.Combine(app.WorkDir, fileName);

                /// 將 ContextZh 的內容以 UTF-8 編碼寫入文字檔案:
                try
                {   File.WriteAllText(pathName, ContextZh, Encoding.UTF8);  }
                catch (Exception ex)
                {
                    m_doc.LastError = ex.Message;
                    return false;
                }

                ContextZhIsModified = false;
            }

            return true;
        }
        #endregion
    }
}
