/// -----------------------------------------------------------------------------------------------
/// <summary>
///  文件專案
/// </summary>
/// <remarks>
///  文件專案(project)維護了一個目錄 Toc，當中包括若干個中英文頁面物件 TocItem 的集合體，檔案格式如下:
///
///  <xml version=1.0 encoding=utf-8 >
///  <doc>
///     <home lang=en url=網址>首頁英文標題</home>
///     <home lane=zh url=網址>首頁中文標題</home>
///     <toc>
///        <chapter name=主檔名>
///            <title lang=en>篇章英文標題</title>
///            <title lang=zh>篇章中文標題</title>
///         </chapter>
///     </toc>
///  </doc>
/// </remarks>
/// <history>
///  2026/10/23 by Shu-Kai Yang (skyang@csie.nctu.edu.tw)
/// </history>
/// -----------------------------------------------------------------------------------------------

using Markdig;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml;

namespace DocsEditor
{
    public sealed class DocProject
    {
        /// <summary>
        ///  返回按鈕的標題與網址。
        /// </summary>
        public String HomeTitleEn = "Home";
        public String HomeTitleZh = "首頁";
        public String HomeUrlEn = "https://shukaiyangtw.github.io/index_en.htm";
        public String HomeUrlZh = "https://shukaiyangtw.github.io/index_zh.htm";

        /// 文件專案中的所有頁面:
        public ObservableCollection<TocItem> Toc = null;

        /// 為了避免建立同檔名的項目，提供查詢功能:
        public TocItem GetItemByName(String name)
        {
            foreach (TocItem item in Toc)
            {   if (item.Name.Equals(name)) {  return item; }  }
            return null;
        }

        /// 目前編輯中的頁面項目:
        public TocItem CurItem = null;
        public int CurItemIndex = -1;

        /// 這個文件是否被更動過:
        public Boolean IsModified = false;

        /// 如果存取函式傳回 false，由此查詢最後發生的錯誤訊息:
        public String LastError = String.Empty;

        /// 沒事可做的建構式:
        public DocProject()
        {   Toc = new ObservableCollection<TocItem>();  }

        #region 存取 docs.tox 檔案.
        /// <summary>
        ///  從指定的檔案中載入專案內容並建立 Toc 項目。
        /// </summary>
        public Boolean LoadXml()
        {
            App app = Application.Current as App;
            String pathName = Path.Combine(app.WorkDir, "docs.tocx");
         /* if (File.Exists(pathName) == false) {  return false;  } */

            XmlDocument doc = new XmlDocument();
            try {  doc.Load(pathName);  }
            catch (Exception ex) {  LastError = ex.Message;  return false;  }

            LastError = String.Empty;
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    XmlElement element = node as XmlElement;

                    if (element.Name.Equals("home"))
                    {
                        if (element.GetAttribute("lang").Equals("zh"))
                        {
                            HomeUrlZh = element.GetAttribute("url");
                            HomeTitleZh = element.InnerText;
                        }
                        else
                        {
                            HomeUrlEn = element.GetAttribute("url");
                            HomeTitleEn = element.InnerText;
                        }
                    }
                    else if (element.Name.Equals("toc"))
                    {
                        foreach (XmlNode child in element.ChildNodes)
                        {
                            if (child.NodeType == XmlNodeType.Element)
                            {
                                XmlElement ele = child as XmlElement;
                                if (ele.Name.Equals("chapter"))
                                {
                                    TocItem item = new TocItem(this);
                                    item.Name = ele.GetAttribute("name");

                                    foreach (XmlNode grandson in ele.ChildNodes)
                                    {
                                        if (grandson.NodeType == XmlNodeType.Element)
                                        {
                                            XmlElement ele2 = grandson as XmlElement;
                                            if (ele2.Name.Equals("title"))
                                            {
                                                if (ele2.GetAttribute("lang").Equals("zh"))
                                                {  item.TitleZh = ele2.InnerText;  }
                                                else
                                                {  item.TitleEn = ele2.InnerText;  }
                                            }
                                        }
                                    }

                                    Toc.Add(item);
                                }
                            }
                        }
                    }
                }
            }

            /// 預設選擇第一個項目:
            if (Toc.Count > 0)
            {  CurItem = Toc[0];  CurItemIndex = 0;  }

            /// 中英文版的顯示預設與系統介面一致:
            IsModified = false;
            return true;
        }

        /// ///////////////////////////////////////////////////////////////////////////////////////
        /// 儲存 docs.tocx 檔案。
        public Boolean SaveXml()
        {
            App app = Application.Current as App;
            XmlDocument doc = new XmlDocument();
            XmlDeclaration declaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(declaration);

            XmlElement root = doc.CreateElement("doc");
            doc.AppendChild(root);

            /// 組織 XML document:
            XmlElement element = doc.CreateElement("home");
            element.SetAttribute("lang", "en");
            element.SetAttribute("url", HomeUrlEn);
            XmlText text = doc.CreateTextNode(HomeTitleEn);
            element.AppendChild(text);
            root.AppendChild(element);

            element = doc.CreateElement("home");
            element.SetAttribute("lang", "zh");
            element.SetAttribute("url", HomeUrlZh);
            text = doc.CreateTextNode(HomeTitleZh);
            element.AppendChild(text);
            root.AppendChild(element);

            element = doc.CreateElement("toc");
            foreach (TocItem item in  Toc)
            {
                XmlElement child = doc.CreateElement("chapter");
                child.SetAttribute("name", item.Name);

                XmlElement grandson = doc.CreateElement("title");
                grandson.SetAttribute("lang", "en");
                text = doc.CreateTextNode(item.TitleEn);
                grandson.AppendChild(text);
                child.AppendChild(grandson);

                grandson = doc.CreateElement("title");
                grandson.SetAttribute("lang", "zh");
                text = doc.CreateTextNode(item.TitleZh);
                grandson.AppendChild(text);
                child.AppendChild(grandson);

                element.AppendChild(child);
            }

            root.AppendChild(element);

            /// 把 XML document 寫入docs.tocx:
            String pathName = Path.Combine(app.WorkDir, "docs.tocx");
            LastError = String.Empty;

            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;

                FileStream stream = File.Create(pathName);
                XmlWriter writer = XmlWriter.Create(stream, settings);
                doc.Save(writer);
                stream.Dispose();
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }

            /// 目錄經過修改，所以刪除已生成的 .htm 檔案:
            foreach (TocItem item in Toc)
            {
                String htmFileName = item.Name + "_en.htm";
                String htmPathName = Path.Combine(app.WorkDir, htmFileName);
                if (File.Exists(htmPathName))
                {   try {  File.Delete(htmPathName);  } catch { }  }

                htmFileName = item.Name + "_zh.htm";
                htmPathName = Path.Combine(app.WorkDir, htmFileName);
                if (File.Exists(htmPathName))
                {   try {  File.Delete(htmPathName);  } catch { }  }
            }

            return true;
        }
        #endregion

        #region 生成 HTML 檔案.
        public Boolean SaveHtmlFile(TocItem item, Boolean IsZh)
        {
            App app = Application.Current as App;

            /// 先判斷要不要儲存檔案:
            if (item.SaveMarkdownFiles() == false) {  return false;  }

            /// 先根據 IsZh 獲得檔案名稱:
            String mdFileName =  item.Name + "_en.md";
            if (IsZh == true) {  mdFileName = item.Name + "_zh.md";  }
            String mdPathName = Path.Combine(app.WorkDir, mdFileName);

            String htmFileName = item.Name + "_en.htm";
            if (IsZh == true) {  htmFileName = item.Name + "_zh.htm";  }
            String htmPathName = Path.Combine(app.WorkDir, htmFileName);

            /// 判斷兩個檔案的版本，是否需要重新產生:
            if (App.ShouldGenFile(mdPathName, htmPathName) == false) {  return true;  }

            /// 準備重新產生檔案，將 markdown 語法的 Context 轉換為 HTML 內容:
            String htmContext = String.Empty;
            if (app.EnableHtmlTable)
            {
                /// 要能夠生成 HTML 表格，必須建立 pipeline 並且令 Makedig 在生成 HTML 內容時要經過它:
                MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UsePipeTables().Build();

                if (IsZh == true)
                {   htmContext = Markdown.ToHtml(item.ContextZh, pipeline);  }
                else
                {   htmContext = Markdown.ToHtml(item.ContextEn, pipeline);  }
            }
            else
            {
                //// Makedig 預設不啟用 HTML 表格的生成:
                if (IsZh == true)
                {   htmContext = Markdown.ToHtml(item.ContextZh);  }
                else
                {   htmContext = Markdown.ToHtml(item.ContextEn);  }
            }

            /// ///////////////////////////////////////////////////////////////////////////////////
            /// 先組織網站目錄的部分:
            StringBuilder sb = new StringBuilder("        <div id=\"toc_div\"><ul>\n");
            if (IsZh == true)
            {
                foreach (TocItem tocItem in Toc)
                {
                    String tocFileName = tocItem.Name + "_zh.htm";
                    if (tocFileName.Equals(htmFileName) == true)
                    {   sb.Append("            <li><a class=\"toc_current\" href=\"");  }
                    else {  sb.Append("            <li><a href=\"");  }
                    sb.Append(tocFileName);
                    sb.Append("\">");
                    sb.Append(tocItem.TitleZh);
                    sb.Append("</a></li>\n\n");
                }
            }
            else
            { 
                foreach (TocItem tocItem in Toc)
                {
                    String tocFileName = tocItem.Name + "_en.htm";
                    if (tocFileName.Equals(htmFileName) == true)
                    {   sb.Append("            <li><a class=\"toc_current\" href=\"");  }
                    else {  sb.Append("            <li><a href=\"");  }
                    sb.Append(tocFileName);
                    sb.Append("\">");
                    sb.Append(tocItem.TitleEn);
                    sb.Append("</a></li>\n");
                }
            }

            sb.Append("        </ul></div>\n\n");
            String tocContext = sb.ToString();

            /// ///////////////////////////////////////////////////////////////////////////////////
            /// 組織網頁主要框架，先準備 head 的部分:
            sb = new StringBuilder("<!doctype html>\n");
            if (IsZh == true) {  sb.Append("<html lang=\"zh-Hant-TW\">\n");  }
            else {  sb.Append("<html lang=\"en\">\n<head>\n");  }

            sb.Append("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />\n");
            sb.Append("    <meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />\n");
            if (IsZh == true)
            {   sb.Append("    <meta http-equiv=\"Content-Language\" content=\"zh-tw\" />\n");  }
            else
            {   sb.Append("    <meta http-equiv=\"Content-Language\" content=\"en-us\" />\n");  }

            sb.Append("    <title>");
            if (IsZh == true) {  sb.Append(item.TitleZh);  }
            else {  sb.Append(item.TitleEn);  }
            sb.Append(" - shukaiyangtw.github.io</title>\n");

            sb.Append("    <link rel=\"stylesheet\" href=\"style.css\" type=\"text/css\" />\n");
            sb.Append("    <link rel=\"stylesheet\" href=\"style_toc.css\" type=\"text/css\" />\n");

            /// 準備切換目錄顯示與否的 JavaScript 程式碼:
            sb.Append("    <script type=\"text/javascript\">\n");
            sb.Append("        function onTocClicked() {\n");
            sb.Append("             let toc = document.getElementById(\"toc_div\");\n");
            sb.Append("             if (toc.style.display == \"block\") {  toc.style.display = \"none\";  }\n");
            sb.Append("             else {  toc.style.display = \"block\";  }\n");
            sb.Append("        }\n");
            sb.Append("    </script>\n</head>\n\n");

            /// ///////////////////////////////////////////////////////////////////////////////////
            /// 組織網頁 body，先組織 action bar 的部分:
            sb.Append("<body>\n<div id=\"page_container\">\n");
            sb.Append("    <div id=\"action_bar_row\">\n");
            sb.Append("        <div id=\"action_bar\">\n");

            sb.Append("            <div class=\"action_bar_button\">\n");
            if (IsZh == true)
            {
                sb.Append("                <a href=\"");
                sb.Append(HomeUrlZh);
                sb.Append("\"><img src=\"images/back.png\" /></a>\n");

                sb.Append("                <a class=\"action_bar_textlink\" href=\"");
                sb.Append(HomeUrlZh);
                sb.Append("\">");
                sb.Append(HomeTitleZh);
                sb.Append("</a>\n");
            }
            else
            {
                sb.Append("                <a href=\"");
                sb.Append(HomeUrlEn);
                sb.Append("\"><img src=\"images/back.png\" /></a>\n");

                sb.Append("                <a class=\"action_bar_textlink\" href=\"");
                sb.Append(HomeUrlEn);
                sb.Append("\">");
                sb.Append(HomeTitleEn);
                sb.Append("</a>\n");
            }
            sb.Append("            </div>\n\n");

            sb.Append("            <div id=\"toc_button\" class=\"action_bar_button\">\n");
            sb.Append("                <a href=\"javascript: onTocClicked();\"><img src=\"images/toc.png\" /></a>\n");
            sb.Append("                <a class=\"action_bar_textlink\" href=\"javascript: onTocClicked();\">");
            if (IsZh == true) {  sb.Append("目錄</a>\n");  } else {  sb.Append("Topics</a>\n");  }
            sb.Append("            </div>\n\n");

            sb.Append("            <div class=\"action_bar_title\">\n                ");
            if (IsZh == true) {  sb.Append(item.TitleZh);  }
            else {  sb.Append(item.TitleEn);  }
            sb.Append("</div>\n\n");

            sb.Append("            <div class=\"action_bar_button\">\n");
            if (IsZh == true)
            {
                sb.Append("                <a href=\"");
                sb.Append(item.Name);
                sb.Append("_en.htm\"><img src=\"images/lang.png\" /></a>\n");

                sb.Append("                <a class=\"action_bar_textlink\" href=\"");
                sb.Append(item.Name);
                sb.Append("_en.htm\">English</a>\n");
            }
            else
            {
                sb.Append("                <a href=\"");
                sb.Append(item.Name);
                sb.Append("_zh.htm\"><img src=\"images/lang.png\" /></a>\n");

                sb.Append("                <a class=\"action_bar_textlink\" href=\"");
                sb.Append(item.Name);
                sb.Append("_zh.htm\">中文</a>\n");
            }
            sb.Append("            </div>\n");
            sb.Append("        </div>\n");
            sb.Append("    </div>\n\n");

            /// ///////////////////////////////////////////////////////////////////////////////////
            /// 組織網站目錄的區塊:
            sb.Append("    <div id=\"context_row\">\n");
            sb.Append("        <div class=\"context_wrapper\" style=\"position: relative\">\n\n");
            sb.Append(tocContext);

            /// 組織網頁內文的區塊:
            sb.Append("        <div class=\"content_div\">\n\n");
            sb.Append(htmContext);
            sb.Append("\n\n");

            /// 建立下一頁按鈕:
            int index = Toc.IndexOf(item);
            if (index != Toc.Count - 1)
            {
                TocItem nextItem = Toc[index + 1];
                sb.Append("            <p style=\"text-align:center\"><a class=\"next_button\" href=\"");
                if (IsZh == true)
                {
                    sb.Append(nextItem.Name);
                    sb.Append("_zh.htm\">下一頁: ");
                    sb.Append(nextItem.TitleZh);
                }
                else
                {
                    sb.Append(nextItem.Name);
                    sb.Append("_en.htm\">Next: ");
                    sb.Append(nextItem.TitleEn);
                }
                sb.Append("</a></p>\n");
            }

            sb.Append("        </div>\n");
            sb.Append("    </div>\n");
            sb.Append("</div>\n</body>\n</html>\n");

            /// 將網頁內容以 UTF-8 編碼寫入檔案中:
            String fileContext = sb.ToString();
            try {  File.WriteAllText(htmPathName, fileContext, Encoding.UTF8);  }
            catch (Exception ex) {  LastError = ex.Message;  return false;  }
            return true;
        }
        #endregion
    }
}
