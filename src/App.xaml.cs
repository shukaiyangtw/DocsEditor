/// -----------------------------------------------------------------------------------------------
/// <summary>
///  全域物件
/// </summary>
/// <remarks>
///  這個衍生自 Application 的類別用來儲存這個程式的全域物件，主要是目前編輯中的 Project 物件。
/// </remarks>
/// <history>
///  2026/10/23 by Shu-Kai Yang (skyang@csie.nctu.edu.tw)
/// </history>
/// -----------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace DocsEditor
{
    public partial class App : Application
    {
        /// 這個唯讀的屬性供程式中其他部位判斷目前的 UI 介面是否為中文版:
        private Boolean m_uiIsChinese = false;
        public Boolean UiIsChinese {  get {  return m_uiIsChinese;  }  }

        /// 其他程式選項:
        private Boolean m_enableHtmlTable = true;
        public Boolean EnableHtmlTable {  get {  return m_enableHtmlTable;  }  }

        /// Assets 所在的目錄:
        private String m_assetDir = String.Empty;
        public String AssetDir {  get {  return m_assetDir;  }  }

        /// 目前的工作目錄。
        public String PreferredDir = String.Empty;
        public String WorkDir = String.Empty;

        /// 目前編輯中的專案:
        public DocProject Project = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            /// 取得目前軟體的 Assets 目錄:
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileInfo exeFileInfo = new FileInfo(assembly.Location);
            m_assetDir = Path.Combine(exeFileInfo.Directory.FullName, "Assets");

            /// 偵測目前作業系統所使用的語言:
            if (Thread.CurrentThread.CurrentCulture.Name == "zh-TW")
            {   m_uiIsChinese = true;  }

            String[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; ++i)
            {
                if (args[i].Equals("-en")) /// 基於測試目的，將整個程式切換成英文版:
                {
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                    m_uiIsChinese = false;

                    FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                    new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
                }
                else if (args[i].Equals("-notable")) /// 停用 HTML 表格功能:
                {
                    m_enableHtmlTable = false;
                }
                else
                {
                    /// 取得 docs.tocx 檔案的所在路徑作為初始目錄:
                    String pathName = args[i];
                    if (pathName.IndexOf(':') == -1)
                    {   pathName = Path.Combine(Directory.GetCurrentDirectory(), pathName);  }

                    PreferredDir = Path.GetDirectoryName(pathName);
                }
            }
        }

        /// <summary>
        ///  檢查檔案是否需要重新產製，srcPathName 是原始資料檔案、例如 .xaml 檔案，genPathName 是
        ///  生成檔案，例如 .html 檔案，當 srcPathName 的日期較新或 genPathName 根本不存在的時候，
        ///  傳回 true。
        /// </summary>
        static public Boolean ShouldGenFile(String srcPathName, String genPathName)
        {
            if (File.Exists(genPathName) == false) {  return true; }

            DateTime dt1 = File.GetLastWriteTime(srcPathName);

         /* 除非徹底地刪除舊檔案再重建，否則檔案重新覆寫的時候，creation time 仍然不會變，因此檢查檔案版本應
          * 該以 modified time (last-write time) 為準，然而作業系統基於自己的最佳化原則，不一定會在每一次
          * 寫入檔案的時候立即寫入正確的 modified time，此外，複製檔案的時候，產生的新檔案之 creation time
          * 是複製檔案的時間，可是 modified time 卻仍然是舊檔案的最後修改時間，因此會產生 modified time 比
          * creation time 還早的奇怪現象，因此要盡可能正確地比較檔案版本，要取兩者的最新值。 */
            DateTime dt = File.GetCreationTime(srcPathName);
            if (DateTime.Compare(dt, dt1) > 0) {  dt1 = dt;  }

            DateTime dt2 = File.GetLastWriteTime(genPathName);
            dt = File.GetCreationTime(genPathName);
            if (DateTime.Compare(dt, dt2) > 0) {  dt2 = dt;  }

            if (DateTime.Compare(dt1, dt2) > 0) {  return true; }
            return false;
        }
    }
}
