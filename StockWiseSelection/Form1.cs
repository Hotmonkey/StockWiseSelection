using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Threading;

namespace StockWiseSelection
{
    public delegate void ScreenCallback(List<string> result);
    public partial class Form1 : Form
    {
        List<string> ScreenResult;
        Thread tGetStock;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (tGetStock != null && tGetStock.ThreadState == ThreadState.Background)
            {
                tGetStock.Abort();
            }
            StockAction sa = new StockAction();
            tGetStock = new Thread(new ParameterizedThreadStart(sa.StockScreen));
            tGetStock.IsBackground = true;
            StockScreenArgs ssa;
            try
            {
                ssa = new StockScreenArgs(this.textBox1.Text, this.textBox2.Text, int.Parse(this.textBox3.Text), this.textBox4, ScreenDone);
            }
            catch(Exception exc)
            {
                MessageBox.Show("输入有误，请重新输入！");
                return;
            }
            tGetStock.Start(ssa);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (tGetStock.ThreadState == ThreadState.Background)
            {
                MessageBox.Show("筛选未完成！");
            }
            else if (ScreenResult == null)
            {
                MessageBox.Show("未筛选数据！");
            }
            else
            {
                MessageBox.Show("这里要显示保存的对话框");
            }
        }

        private void ScreenDone(List<string> result)
        {
            this.ScreenResult = result;
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(Char.IsNumber(e.KeyChar) || e.KeyChar == 8))
            {
                e.Handled = true;
            }
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(Char.IsNumber(e.KeyChar) || e.KeyChar == 8))
            {
                e.Handled = true;
            }
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(Char.IsNumber(e.KeyChar) || e.KeyChar == 8))
            {
                e.Handled = true;
            }
        }
    }

    class StockScreenArgs
    {
        string format = "yyyyMMdd";

        string beginDate;
        string endDate;
        int declineDayCount;
        TextBox resultTb;
        ScreenCallback sCallback;

        public string BeginDate { get { return beginDate; } }
        public string EndDate { get { return endDate; } }
        public int DeclineDayCount { get { return declineDayCount; } }
        public TextBox ResultTb { get { return resultTb; } }
        public ScreenCallback SCallback { get { return sCallback; } }

        public StockScreenArgs()
        {
 
        }

        public StockScreenArgs(string beginDate, string endDate, int declineDayCount, TextBox tb, ScreenCallback sCallback)
        {
            DateTime.ParseExact(beginDate, format, System.Globalization.CultureInfo.InvariantCulture);
            DateTime.ParseExact(endDate, format, System.Globalization.CultureInfo.InvariantCulture);
            this.beginDate = beginDate;
            this.endDate = endDate;
            this.declineDayCount = declineDayCount;
            this.resultTb = tb;
            this.sCallback = sCallback;
        }

        public void Settings(string beginDate, string endDate, int declineDayCount, TextBox tb, ScreenCallback sCallback)
        {
            this.beginDate = beginDate;
            this.endDate = endDate;
            this.declineDayCount = declineDayCount;
            this.resultTb = tb;
            this.sCallback = sCallback;
        }
    }

    class StockAction
    {
        /// <summary>
        /// http获取数据
        /// </summary>
        /// <param name="Url"></param>
        /// <returns></returns>
        private string HttpGet(string Url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "GET";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream);
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }

        private List<string> SplitOriginStr(string origin)
        {
            List<string> result = origin.Split('\r', '\n').ToList<string>();
            result.RemoveAt(result.Count - 1);
            return result;
        }

        public void StockScreen(object data)
        {
            StockScreenArgs ssa = (StockScreenArgs)data;
            ssa.ResultTb.Text = "开始筛选\r\n";
            List<string> result = new List<string>();
            StockUrl stockUrl = new StockUrl();
            string[] headArray = new string[] { "sh600", "sh601", "sh603", "sz000", "sz300" };
            string EndDate = ssa.EndDate;
            string BeginDate = ssa.BeginDate;
            int headIndex = 0;
            int tailIndex = 0;
            string stockStr = "";
            for (headIndex = 0; headIndex < headArray.Length; headIndex++)
            {
                for (tailIndex = 1; tailIndex <= 999; tailIndex++)
                {
                    stockStr = headArray[headIndex] + tailIndex.ToString("000");
                    stockUrl.Settings(headArray[headIndex], tailIndex.ToString("000"), BeginDate, EndDate);
                    string StockOriginData = HttpGet(stockUrl.URL);
                    List<string> historyStrings = SplitOriginStr(StockOriginData);
                    List<StockHistory> history = new List<StockHistory>();
                    for (int i = 0; i < historyStrings.Count; i++)
                    {
                        history.Add(new StockHistory(historyStrings[i]));
                    }
                    int declineCount = 0;
                    for (int i = 1; i < history.Count; i++)
                    {
                        if (history[i].Close - history[i - 1].Close < 0)
                        {
                            declineCount++;
                        }
                    }
                    bool choose = false;
                    if (declineCount >= ssa.DeclineDayCount)
                    {
                        choose = true;
                    }
                    if (choose)
                    {
                       ssa.ResultTb.AppendText(stockStr + "\r\n");
                       result.Add(stockStr);
                    }
                }
            }
            ssa.ResultTb.AppendText("筛选完成！");
            ssa.SCallback(result);
        }
    }

    class StockUrl
    {
        string urlHead = "http://biz.finance.sina.com.cn/stock/flash_hq/kline_data.php?&rand=random(10000)&symbol=";
        string stockHead = "sh000";
        string stockTail = "001";
        string urlEndDateHead = "&end_date=";
        string urlEndDate = "20151203";
        string urlBeginDateHead = "&begin_date=";
        string urlBeginDate = "20151201";
        string urlTail = "&type=plain";

        public string URL { get { return urlHead + stockHead + stockTail + urlEndDateHead + urlEndDate + urlBeginDateHead + urlBeginDate + urlTail; } }

        public StockUrl()
        {

        }

        public StockUrl(string stockHead, string stockTail, string urlBeginDate, string urlEndDate)
        {
            this.stockHead = stockHead;
            this.stockTail = stockTail;
            this.urlBeginDate = urlBeginDate;
            this.urlEndDate = urlEndDate;
        }

        public void Settings(string stockHead, string stockTail, string urlBeginDate, string urlEndDate)
        {
            this.stockHead = stockHead;
            this.stockTail = stockTail;
            this.urlBeginDate = urlBeginDate;
            this.urlEndDate = urlEndDate;
        }
    }

    class StockHistory
    {
        DateTime dt;
        float open;
        float high;
        float close;
        float low;
        int total;

        public DateTime DT { get { return dt; } }
        public float Open { get { return open; } }
        public float Hign { get { return high; } }
        public float Close { get { return close; } }
        public float Low { get { return low; } }
        public int Total { get { return total; } }


        public StockHistory(string historyStr)
        {
            string[] array = historyStr.Split(',');
            dt = DateTime.Parse(array[0]);
            open = float.Parse(array[1]);
            high = float.Parse(array[2]);
            close = float.Parse(array[3]);
            low = float.Parse(array[4]);
            total = int.Parse(array[5]);
        }
    }
}
