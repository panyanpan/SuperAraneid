using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SuperAraneid.Helper;

namespace SuperAraneid
{    
    public partial class FrmStrat : Form
    {
        public FrmStrat()
        {
            InitializeComponent();
        }

        //等待采集链接的队列
        private List<string> LinksUrls = new List<string>();
        //等待采集的图片链接的下载队列
        private List<string> LoadingImgUrls = new List<string>();                
        //已采集过图片的链接
        private List<string> UsedUrls = new List<string>();
        //已采集过链接的链接
        private List<string> UsedLinksUrls = new List<string>();
        //已使用过的图片
        private List<string> UsedImgUrls = new List<string>();

        private readonly int _maxtask = 200;        //最大任务数
        private int _downingPicCount = 0;           //下载中的图片数量
        private int _downLoadPicCount = 0;          //已下载的图片数量
        private int _startDownPicCount = 0;         //等待下载的图片

        Thread Tstarting = null;                    

        private void FrmStrat_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
            Tstarting = new Thread(DownLoading);
        }
       
        /// <summary>
        /// 选择保存路径——文件夹
        /// </summary>
        private void SelectFolder()
        {
            fbd_url.ShowDialog();
            string path = fbd_url.SelectedPath;
            if (!string.IsNullOrEmpty(path))
            {                
                Global.FloderUrl = fbd_url.SelectedPath + "\\";
            }
            else
            {
                Global.FloderUrl = Application.StartupPath + Global.FloderMoUrl;
            }

        }

        private void btn_shoudong_Click(object sender, EventArgs e)
        {

            SelectFolder();

            LoadingImgUrls.Add(txt_url.Text);
            LinksUrls.Add(txt_url.Text);

            Global.WebUrl = StringHelper.GetPureUrl(txt_url.Text);
            Tstarting.Start();

            btn_shoudong.Enabled = false;
        }
        /// <summary>
        /// 下载成功
        /// </summary>
        /// <param name="asyncresult"></param>
        private void onDownLoadFinish(IAsyncResult asyncresult)
        {
            AsyncResult result = (AsyncResult)asyncresult;
            StopTimeHandler del = (StopTimeHandler)result.AsyncDelegate;
            string data = (string)result.AsyncState;
            string name = del.EndInvoke(result);
            TextResultChange(name + data);
            _downLoadPicCount++;
            _downingPicCount--;
            TipDownLoad();
        }
        /// <summary>
        /// 下载中
        /// </summary>
        private void StartDownLoad()
        {
            //是否超过最大的队列
            if (_downingPicCount < _maxtask)
            {
                List<string> imgurls = HttpHelper.GetHtmlImageUrlList(LoadingImgUrls.FirstOrDefault());
                UsedUrls.Add(LoadingImgUrls.FirstOrDefault());
                LoadingImgUrls.RemoveAt(0);
                foreach (string url in imgurls)
                {
                    if (!UsedImgUrls.Contains(url))
                    {
                        //创建异步下载
                        DownloadHelper helper = new DownloadHelper();
                        StopTimeHandler stop = new StopTimeHandler(helper.DowloadImg);
                        AsyncCallback callback = new AsyncCallback(onDownLoadFinish);
                        IAsyncResult asyncResult = stop.BeginInvoke(url, callback, "--下载完成 \r\n");
                        //链接载入已使用
                        UsedImgUrls.Add(url);
                        TipStartDownLoad();
                    }

                }
                imgurls.Clear();
                TextUrlChange("");
            }
            else
            {
                Thread.Sleep(5000);
                TextResultChange("-----------任务过多，搜集程序休眠5秒 \r\n");
            }
            DownLoading();
        }
        /// <summary>
        /// 开始爬行网页
        /// </summary>
        /// <param name="url"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        private void CollectionUrls()
        {
            if (LinksUrls.Count > 0)
            {
                try
                {
                    string url = LinksUrls.FirstOrDefault();
                    //加入链接已采集存档
                    UsedLinksUrls.Add(url);
                    //获取该链接的所有URL
                    List<string> urlList = HttpHelper.GetLinks(url);

                    foreach (string url1 in urlList)
                    {
                        //如果没采集过链接
                        if (!UsedLinksUrls.Contains(url1))
                        {
                            LinksUrls.Add(url1);
                        }
                        //如果没采集过图片
                        if (!UsedImgUrls.Contains(url1))//if (!UsedUrls.Contains(url1))
                        {
                            LoadingImgUrls.Add(url1);
                        }
                    }
                    //删除已使用
                    LinksUrls.RemoveAt(0);
                }
                catch (Exception)
                {
                }
                DownLoading();
            }
            else
            {
                TextResultChange("-----------任务结束，全部爬行完毕 \r\n");
            }

        }
        /// <summary>
        /// 收集图片
        /// </summary>
        private void DownLoading()
        {
            if (LoadingImgUrls.Count < 1)
            {
                TextResultChange("-----------下载完毕,正在重新收集链接 \r\n");
                CollectionUrls();
            }
            else
            {
                //继续下载
                //Tdown.Start();
                StartDownLoad();
            }
        }

        private void TipDownLoad()
        {
            lbl_downingcount.Text = _downingPicCount.ToString();
            lbl_piccount.Text = string.Format("已下载{0}张图片", _downLoadPicCount);
        }

        private void TipStartDownLoad()
        {
            _downingPicCount++;
            _startDownPicCount++;
            lbl_startcount.Text = string.Format("{0}张图片等待下载", _startDownPicCount);
        }

        private void TextResultChange(string text)
        {
            txt_result.AppendText(text);
            txt_result.ScrollToCaret();
        }

        private void TextUrlChange(string text)
        {
            txt_usedurl.Text = "";
            txt_usedurl.AppendText(string.Join("\r\n ", UsedUrls));
            txt_usedurl.ScrollToCaret();
        }

        private void FrmStrat_FormClosed(object sender, FormClosedEventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void btn_About_Click(object sender, EventArgs e)
        {
            FromAbout about = new FromAbout();
            about.Show();
        }


    }

}
