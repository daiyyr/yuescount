using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Globalization;

namespace sfvScout
{
    public partial class Form1 : Form
    {
        Thread gAlarm = null;
        string gnrnodeGUID = "";
        string gViewstate = "";
        string gViewStateGenerator = "";
        CookieCollection gCookieContainer = null;
        string user = "dudeea";
        string password = "Dd123456";

        public delegate void setLog(string str1);
        public void setLogT(string s)
        {
            if (logT.InvokeRequired)
            {
                // 实例一个委托，匿名方法，
                setLog sl = new setLog(delegate(string text)
                {
                    logT.AppendText(DateTime.Now.ToString() + " " + text + Environment.NewLine);
                });
                // 把调用权交给创建控件的线程，带上参数
                logT.Invoke(sl, s);
            }
            else
            {
                logT.AppendText(DateTime.Now.ToString() + " " + s + Environment.NewLine);
            }
        }

        public void setLogtRed(string s)
        {
            if (logT.InvokeRequired)
            {
                setLog sl = new setLog(delegate(string text)
                {
                    logT.AppendText(DateTime.Now.ToString() + " " + text + Environment.NewLine);
                    int i = logT.Text.LastIndexOf("\n", logT.Text.Length - 2);
                    if (i > 1)
                    {
                        logT.Select(i, logT.Text.Length);
                        logT.SelectionColor = Color.Red;
                        logT.Select(i, logT.Text.Length);
                        logT.SelectionFont = new Font(Font, FontStyle.Bold);
                    }
                });
                logT.Invoke(sl, s);
            }
            else
            {
                logT.AppendText(DateTime.Now.ToString() + " " + s + Environment.NewLine);
                int i = logT.Text.LastIndexOf("\n", logT.Text.Length - 2);
                if (i > 1)
                {
                    logT.Select(i, logT.Text.Length);
                    logT.SelectionColor = Color.Red;
                    logT.Select(i, logT.Text.Length);
                    logT.SelectionFont = new Font(Font, FontStyle.Bold);
                }
            }
        }

        public Form1()
        {
            InitializeComponent();
            if (File.Exists(System.Environment.CurrentDirectory + "\\" + "urlList"))
            {
                string[] lines = File.ReadAllLines(System.Environment.CurrentDirectory + "\\" + "urlList");
                foreach (string line in lines)
                {
                    urlList.Items.Add(line);
                }
            }
        }

        public void alarm()
        {
            System.Media.SoundPlayer player = new System.Media.SoundPlayer(Properties.Resources.mtl);
            player.Load();
            player.PlayLooping();
        }
        public static string ToUrlEncode(string strCode)
        {
            StringBuilder sb = new StringBuilder();
            byte[] byStr = System.Text.Encoding.UTF8.GetBytes(strCode); //默认是System.Text.Encoding.Default.GetBytes(str)  
            System.Text.RegularExpressions.Regex regKey = new System.Text.RegularExpressions.Regex("^[A-Za-z0-9]+$");
            for (int i = 0; i < byStr.Length; i++)
            {
                string strBy = Convert.ToChar(byStr[i]).ToString();
                if (regKey.IsMatch(strBy))
                {
                    //是字母或者数字则不进行转换    
                    sb.Append(strBy);
                }
                else
                {
                    sb.Append(@"%" + Convert.ToString(byStr[i], 16));
                }
            }
            return (sb.ToString());
        }

        public void writeFile(string file, string content)
        {
            FileStream aFile = new FileStream(file, FileMode.Create);
            StreamWriter sw = new StreamWriter(aFile);
            sw.Write(content);
            sw.Close();
        }

        public int downloadHtml(string url, string html)
        {
            string lastSection = "";
            string P = @"(?<=\/)[^\/]+?(?=$|\/$|\?)";
            Match found = (new Regex(P)).Match(url);
            if (found.Success)
            {
                lastSection = found.Groups[0].Value;
            }
            string fileName = lastSection + System.DateTime.Now.ToString("yyyyMMddHHmmss", DateTimeFormatInfo.InvariantInfo) + ".txt";
            writeFile(System.Environment.CurrentDirectory + "\\" + fileName, "URL:" + url + Environment.NewLine + "HTML:" + Environment.NewLine + html);
            return 1;
        }
        public int HtmlHandler(HttpWebResponse resp)
        {            
            string url = resp.ResponseUri.ToString();
            string html = resp2html(resp);
            if (html.Equals(""))
            {
                return -1;
            }
            string validHtml = "";
            string lastSection = "";
            bool have_APM_DO_NOT_TOUCH = false;
            string P = @"(?<=\/)[^\/]+?(?=$|\/$|\?)";
            Match found = (new Regex(P)).Match(url);
            if (found.Success)
            {
                lastSection = found.Groups[0].Value;
            }
            if (html.Contains("APM_DO_NOT_TOUCH"))//得到的是带JS乱码的页
            {
                have_APM_DO_NOT_TOUCH = true;
                P = @"(?<=</APM_DO_NOT_TOUCH>)[\s\S]+(?=$)";
                found = (new Regex(P)).Match(html);
                if (found.Success)
                {
                    validHtml = found.Groups[0].Value;
                }
            }
            else
            {
                validHtml = html;
            }
            validHtml = Regex.Replace(validHtml, @"<div id=""dateTime"">.+?<\/div>", "");
            DirectoryInfo dir = new DirectoryInfo(System.Environment.CurrentDirectory);
            FileInfo[] allFile = dir.GetFiles();
            bool isNewContent = true;
            bool isNewURL = true;
            foreach (FileInfo fi in allFile)
            {                
                if (!fi.Name.Contains(lastSection))
                {
                    continue;
                }
                else
                {
                    string fileContent = System.IO.File.ReadAllText(fi.FullName);
                    string urlInFile = "";
                    P = @"(?<=URL:).+?(?=\r\n)";
                    found = (new Regex(P)).Match(fileContent);
                    if (found.Success)
                    {
                        urlInFile = found.Groups[0].Value;
                    }
                    if (!urlInFile.Equals(url))
                    {
                        continue;
                    }
                    else//找到url相同的文件
                    {
                        isNewURL = false;
                        string validHtmlInFile = "";
                        if (have_APM_DO_NOT_TOUCH)
                        {
                            P = @"(?<=</APM_DO_NOT_TOUCH>)[\s\S]+(?=$)";
                            found = (new Regex(P)).Match(fileContent);
                            if (found.Success)
                            {
                                validHtmlInFile = found.Groups[0].Value;
                            }
                        }
                        else
                        {
                            P = @"(?<=\r\nHTML:\r\n)[\s\S]+(?=$)";
                            found = (new Regex(P)).Match(fileContent);
                            if (found.Success)
                            {
                                validHtmlInFile = found.Groups[0].Value;
                            }
                        }
                        validHtmlInFile = Regex.Replace(validHtmlInFile, @"<div id=""dateTime"">.+?<\/div>", "");
                        if (validHtmlInFile.Equals(validHtml))//有效内容也相同，则认为页面无变更
                        {
                            isNewContent = false;
                            break;
                        }
                        else //有效内容不同，有可能与早期文件内容不同，与新文件相同，继续遍历
                        {
                            continue;
                        }
                    }
                }
            }
            if (isNewURL)//认为是新增的地址，进行第一次下载
            {
                downloadHtml(url, html);
                setLogT("new url: " + url );
                setLogT("page saved successfully");
                return 1;
            }
            if (isNewContent)//旧URL，且与所有文件内容均不同，下载文件，拉响警报！
            {
                downloadHtml(url, html);
                if (gAlarm != null)
                {
                    //gAlarm.Abort();                   
                }
                else
                {
                    Thread t = new Thread(alarm);
                    t.Start();
                    gAlarm = t;
                }
                setLogtRed("Attention! Page modified on " + url);
                return 2;
            }
            else
            {
                setLogT(url + " is unchanged");
                return 3;
            }
        }

        public void setRequest(HttpWebRequest req)
        {
            req.AllowAutoRedirect = false;
            req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            //req.Accept = "*/*";
            //req.Connection = "keep-alive";
            req.KeepAlive = true;
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:37.0) Gecko/20100101 Firefox/37.0";
            //req.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; InfoPath.3; .NET4.0C; .NET4.0E";
            req.Headers["Accept-Encoding"] = "gzip, deflate";
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            req.Host = "www.immigration.govt.nz";
            req.CookieContainer = new CookieContainer();
            req.CookieContainer.PerDomainCapacity = 40;
            req.ContentType = "application/x-www-form-urlencoded";
        }

        public int writePostData(HttpWebRequest req, string data)
        {
            byte[] postBytes = Encoding.UTF8.GetBytes(data);
            req.ContentLength = postBytes.Length;
            Stream postDataStream = null;
            try
            {
                postDataStream = req.GetRequestStream();
                
            }
            catch (WebException webEx)
            {
                setLogT("GetRequestStream," + webEx.Status.ToString());
                return -1;
            }
            postDataStream.Write(postBytes, 0, postBytes.Length);
            postDataStream.Close();
            return 1;
        }

        public string resp2html(HttpWebResponse resp)
        {
            string respHtml = "";
            char[] cbuffer = new char[256];
            Stream respStream = resp.GetResponseStream();
            StreamReader respStreamReader = new StreamReader(respStream);//respStream,Encoding.UTF8
            int byteRead = 0;
            try
            {
                byteRead = respStreamReader.Read(cbuffer, 0, 256);

            }
            catch (WebException webEx)
            {
                setLogT("respStreamReader, " + webEx.Status.ToString());
                return "";
            }
            while (byteRead != 0)
            {
                string strResp = new string(cbuffer, 0, byteRead);
                respHtml = respHtml + strResp;
                try
                {
                    byteRead = respStreamReader.Read(cbuffer, 0, 256);
                }
                catch (WebException webEx)
                {
                    setLogT("respStreamReader, " + webEx.Status.ToString());
                    return "";
                }
                
            }
            respStreamReader.Close();
            respStream.Close();
            return respHtml;
        }


        public int loginF()
        {
        getLoginHtml:
            setLogT("login1..");
            string LoginUrl = "https://www.immigration.govt.nz/secure/Login+Working+Holiday.htm";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(LoginUrl);
            HttpWebResponse resp = null;
            setRequest(req);
            req.Method = "POST";
            string respHtml = "";
            req.Referer = "https://www.immigration.govt.nz/secure/Login+Working+Holiday.htm";
            req.ContentType = "application/x-www-form-urlencoded";
            if (
                writePostData(req, "TS0120d49b_76=0"
                + "&TS0120d49b_86=0"
                + "&TS0120d49b_cr=08eba48ebbab280005c3feeb3387f42e86443e509e659dd53e4f5f2d0d2c4ae01e32903a43b4077b598b8abd49931b42087c8733fa894800e5c1c4071f1e6ad87a6f25bc8ea65effbf3eb2f42056fe16b0b02dfbc94b881e12d829c30547783fa7788c4cac4afdd01c2fd58baa1fcb77ae19353d2c154926c5bb674877292c83"
                + "&TS0120d49b_ct=0"
                + "&TS0120d49b_id=3"
                + "&TS0120d49b_md=1"
                + "&TS0120d49b_pd=0"
                + "&TS0120d49b_rf=0"
                )
              < 0)
            {
                return -3;
            }

            
            try
            {
                resp = (HttpWebResponse)req.GetResponse();
            }
            catch (WebException webEx)
            {
                if (webEx.Status == WebExceptionStatus.Timeout)
                {
                    setLogT("lgoin1 Timeout..");
                    goto getLoginHtml;
                }
                if (webEx.Status == WebExceptionStatus.NameResolutionFailure)
                {
                    setLogT("lgoin1 NameResolutionFailure..");
                    goto getLoginHtml;
                }
                if (webEx.Status == WebExceptionStatus.UnknownError)
                {
                    setLogT("post UnknownError..");
                    goto getLoginHtml;
                }
                if (webEx.Status == WebExceptionStatus.ConnectFailure)
                {
                    setLogT("post ConnectFailure..");
                    goto getLoginHtml;
                }
                if (webEx.Status == WebExceptionStatus.ConnectionClosed)
                {
                    setLogT("post ConnectionClosed..");
                    goto getLoginHtml;
                }
                else
                {
                    setLogT("other WebExceptions..");
                    return -3;
                }
            }

            if (resp != null)
            {
                respHtml = resp2html(resp);
                if (respHtml.Equals(""))
                {
                    goto getLoginHtml;
                }
            }
            else
            {
                goto getLoginHtml;
            }
            string tokenValP = @"(?<=;NRNODEGUID=).*?(?=&amp;)";
            Match foundTokenVal = (new Regex(tokenValP)).Match(respHtml);
            if (foundTokenVal.Success)
            {
                gnrnodeGUID = foundTokenVal.Groups[0].Value;
                setLogT("got token");
            }

            tokenValP = @"(?<=name=""__VIEWSTATE"" value="").*?(?="" />)";//after@, transfer" by""
            foundTokenVal = (new Regex(tokenValP)).Match(respHtml);
            if (foundTokenVal.Success)
            {
                gViewstate = foundTokenVal.Groups[0].Value;
            }

            tokenValP = @"(?<=__VIEWSTATEGENERATOR"" value="").*?(?="" />)";//after@, transfer" by""
            foundTokenVal = (new Regex(tokenValP)).Match(respHtml);
            if (foundTokenVal.Success)
            {
                gViewStateGenerator = foundTokenVal.Groups[0].Value;
            }

            resp.Close();

        post1:
            //1st post
            setLogT("login2..");
            string postUrl = "https://www.immigration.govt.nz/Templates/Secure/Login.aspx?NRMODE=Published&NRNODEGUID="+ gnrnodeGUID +"&NRORIGINALURL=%2fsecure%2fLogin%2bWorking%2bHoliday%2ehtm&NRCACHEHINT=Guest";
            HttpWebRequest req2 = (HttpWebRequest)WebRequest.Create(postUrl);
            setRequest(req2);
            req2.CookieContainer = req.CookieContainer;
            req2.Method = "POST";
            req2.Referer = "https://www.immigration.govt.nz/secure/Login+Working+Holiday.htm";
            if (
                writePostData(req2, "HeaderCommunityHomepage%3ASearchControl%3AtxtSearchString="
                + "&OnlineServicesLoginStealth%3AVisaLoginControl%3AloginImageButton.x=52"
                + "&OnlineServicesLoginStealth%3AVisaLoginControl%3AloginImageButton.y=15"
                + "&OnlineServicesLoginStealth%3AVisaLoginControl%3AuserNameTextBox="
                + user
                + "&OnlineServicesLoginStealth%3AVisaLoginControl%3ApasswordTextBox="
                + password
                + "&VisaDropDown=%2Fsecure%2FLogin%2BWorking%2BHoliday.htm"
                + "&__EVENTARGUMENT=&__EVENTTARGET="
                + "&__VIEWSTATE="
                + ToUrlEncode(gViewstate)
                + "&__VIEWSTATEGENERATOR="
                + gViewStateGenerator)
                < 0) 
            {
                return -3;
            }
        
            try
            {
                resp = (HttpWebResponse)req2.GetResponse();
            }
            catch (WebException webEx)
            {
                if (webEx.Status == WebExceptionStatus.Timeout)
                {
                    setLogT("login2 Timeout..");
                    goto post1;
                }
                if (webEx.Status == WebExceptionStatus.NameResolutionFailure)
                {
                    setLogT("login2 NameResolutionFailure..");
                    goto post1;
                }
                if (webEx.Status == WebExceptionStatus.UnknownError)
                {
                    setLogT("post UnknownError..");
                    goto post1;
                }
                if (webEx.Status == WebExceptionStatus.ConnectFailure)
                {
                    setLogT("post ConnectFailure..");
                    goto post1;
                }
                if (webEx.Status == WebExceptionStatus.ConnectionClosed)
                {
                    setLogT("post ConnectionClosed..");
                    goto post1;
                }
                else
                {
                    setLogT("other WebExceptions..");
                    return -3;
                }
            }
            if (resp != null)
            {
                respHtml = resp2html(resp);
                if (respHtml.Equals(""))
                {
                    goto post1;
                }
            }
            else
            {
                goto post1;
            }

            if (respHtml.Contains("incorrect user name or password"))
            {
                setLogT("username/password error!");
                return -1;
            }
            else
            {
                setLogT("password verification OK");
            }

            if (req2.CookieContainer.GetCookies(req2.RequestUri)["ImmigrationAuth"] != null)
            {
                setLogT("got ImmigrationAuth");
            }
            else
            {
                setLogT("got ImmigrationAuth err");
                return -2;
            }
            gCookieContainer = req2.CookieContainer.GetCookies(req2.RequestUri);
            resp.Close();
            setLogT("login succeed");
            return 1;
        }

        public int probe(string url)
        {
            string lastSection="";
            string P = @"(?<=\/)[^\/]+?(?=$|\/$|\?)";
            Match found = (new Regex(P)).Match(url);
            if (found.Success)
            {
                lastSection = found.Groups[0].Value;
            }
            setLogT("post "+ lastSection +"..");
        post1:
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse resp = null;
            setRequest(req);
            req.Method = "POST";
            req.Referer = "https://www.immigration.govt.nz/SilverFern/";
            if (gCookieContainer != null)
            {
                req.CookieContainer.Add(gCookieContainer);
            }
            req.ContentType = "application/x-www-form-urlencoded";
            if (
                writePostData(req, "TS0120d49b_76=0"
                + "&TS0120d49b_86=0"
                + "&TS0120d49b_cr=08eba48ebbab2800e443817b4ffa4b06b983fe9ef7704e54a006aedadf1ecdb6d52731a0895708329e5c6c247eb39b7c08fe389b03894800809f0e86c6d5b2ada4bb53965d3352a581e9186cbb7669fe51daefec752eef50b4fb649cf368df61b0872e03ebc2cbd87197735b6f2ce8cc47157f9998644844e2ae018981e37a89"
                + "&TS0120d49b_ct=0"
                + "&TS0120d49b_id=3"
                + "&TS0120d49b_md=1"
                + "&TS0120d49b_pd=0"
                + "&TS0120d49b_rf=0"
                )
                < 0)
            {
                return -2;
            }
            string respHtml = "";
        
            try
            {
                resp = (HttpWebResponse)req.GetResponse();
            }
            catch (WebException webEx)
            {
                if (webEx.Status == WebExceptionStatus.Timeout)
                {
                    setLogT("post Timeout..");
                    goto post1;
                } 
                if (webEx.Status == WebExceptionStatus.NameResolutionFailure)
                {
                    setLogT("post NameResolutionFailure..");
                    goto post1;
                }
                if (webEx.Status == WebExceptionStatus.UnknownError)
                {
                    setLogT("post UnknownError..");
                    goto post1;
                }
                if (webEx.Status == WebExceptionStatus.ConnectFailure)
                {
                    setLogT("post ConnectFailure..");
                    goto post1;
                }
                if (webEx.Status == WebExceptionStatus.ConnectionClosed)
                {
                    setLogT("post ConnectionClosed..");
                    goto post1;
                }
                else
                {
                    setLogT("other WebExceptions..");
                    return -2;
                }
            }
            if (resp.StatusCode == HttpStatusCode.Redirect)
            {
                setLogT("session expired!");
                return -1;
            }
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                HtmlHandler(resp);
            }
            else
            {
                goto post1;
            }
            //gCookieContainer = req.CookieContainer.GetCookies(req.RequestUri);
            resp.Close();
            return 1;
        }

        public void loginT()
        {
            while (true)
            {
                if (rate.Text.Equals(""))
                {
                    Thread.Sleep(500);
                }
                else if (Convert.ToInt32(rate.Text) > 0)
                {
                    Thread.Sleep(Convert.ToInt32(rate.Text));
                }
                else
                {
                    Thread.Sleep(500);
                }

                int r = loginF();
                if (r == -3)
                {
                    continue;
                }
                if (r != -2)
                {
                    break;
                }
                
            }
        }

        public void autoT()
        {            
            loginT();
            while (true)
            {
                for (int i = 0; i < urlList.Items.Count; i++)
                {
                    if (probe(urlList.GetItemText(urlList.Items[i])) == -1)
                    {
                        loginT();
                    }
                    if (rate.Text.Equals(""))
                    {
                        Thread.Sleep(500);
                    }
                    else if (Convert.ToInt32(rate.Text) > 0)
                    {
                        Thread.Sleep(Convert.ToInt32(rate.Text));
                    }
                    else
                    {
                        Thread.Sleep(500);
                    }                    
                }
            }
        }

        private void loginB_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(loginT);
            t.Start();
        }

        private void autoB_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(autoT);
            t.Start();
        }

        private void rate_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }
        private void logT_TextChanged(object sender, EventArgs e)
        {
            logT.SelectionStart = logT.Text.Length;
            logT.ScrollToCaret();
        }

        public delegate void delegate2();
        public void addURL()
        {
            //string P = @"^http(s)?:\/\/([\w-]+\.)+[\w-]+$";//无法匹配下级页面
            string P = @"^(https?|ftp|file)://[-a-zA-Z0-9+&@#/%?=~_|!:,.;]*[-a-zA-Z0-9+&@#/%=~_|]";
            Match M = (new Regex(P)).Match(inputT.Text);
            if (M.Success)
            {
            }else{
                MessageBox.Show("invalid url!");
                return;
            }            
            if (urlList.InvokeRequired)
            {
                delegate2 sl = new delegate2(delegate()
                {
                    urlList.Items.Add(inputT.Text);
                    inputT.Text = "";
                });
                urlList.Invoke(sl);
            }
            else
            {
                urlList.Items.Add(inputT.Text);
                inputT.Text = "";
            }
            string strCollected = string.Empty;
            for (int i = 0; i < urlList.Items.Count; i++)
            {
                if (strCollected == string.Empty)
                {
                    strCollected = urlList.GetItemText(urlList.Items[i]);
                }
                else
                {
                    strCollected += "\n" + urlList.GetItemText(urlList.Items[i]) ;
                }
            }
            writeFile(System.Environment.CurrentDirectory + "\\" + "urlList", strCollected);
        }

        public void deleteURL()
        {
            if (urlList.InvokeRequired)
            {
                delegate2 sl = new delegate2(delegate()
                {
                    for (int i = urlList.CheckedItems.Count - 1; i >= 0; i--)
                    {
                        urlList.Items.Remove(urlList.CheckedItems[i]);
                    }
                });
                urlList.Invoke(sl);
            }
            else
            {
                for (int i = urlList.CheckedItems.Count - 1; i >= 0; i--)
                {
                    urlList.Items.Remove(urlList.CheckedItems[i]);
                }
            }
            string strCollected = string.Empty;
            for (int i = 0; i < urlList.Items.Count; i++)
            {
                if (strCollected == string.Empty)
                {
                    strCollected = urlList.GetItemText(urlList.Items[i]);
                }
                else
                {
                    strCollected += "\n" + urlList.GetItemText(urlList.Items[i]);
                }
            }
            writeFile(System.Environment.CurrentDirectory + "\\" + "urlList", strCollected);
        }

        private void addB_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(addURL);
            t.Start();
        }

        private void deleteB_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(deleteURL);
            t.Start();
        }
    }
}

