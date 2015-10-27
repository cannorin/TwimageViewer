using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreTweet;

namespace Twimages
{
    public partial class Form1 : Form
    {
        public List<Tuple<string, Status>> Pages { get; set; }
        public int Index { get; set; }

        public const string ConsumerKey = "pGWbiAb9MzQobemWz3it5ri5k";
        public const string ConsumerSecret = "cF5DaumKjahWk7GGSJm71MRmD5ToKJ3GJ7dT1g07U7SOs7C3Wu";
        public string BearerToken { get { return Properties.Settings.Default.Bearer; } set { Properties.Settings.Default.Bearer = value; } }

        public OAuth2Token Token { get; private set; }

        public Form1()
        {
            InitializeComponent();

            this.BringToFront();
            this.Focus();
            this.KeyPreview = true;
 
            Index = 0; Pages = new List<Tuple<string, Status>>();
            button1.Enabled = button2.Enabled = false;

            if (BearerToken.Equals("none"))
                try
                {
                    BearerToken = OAuth2.GetToken(ConsumerKey, ConsumerSecret).BearerToken;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                    Environment.Exit(1);
                }
            Token = OAuth2Token.Create(ConsumerKey, ConsumerSecret, BearerToken);
            toolStripTextBox1.Focus();
        }

        ~Form1()
        {
            Properties.Settings.Default.Save();
        }

        void LoadImage()
        {
            var x = new Dictionary<string, object>();
            x.Add("screen_name", toolStripTextBox1.Text);
            if (Pages.Count > 0) x.Add("max_id", Pages.Last().Item2.Id);
            x.Add("count", 200);
            try
            {
                foreach (var t in toolStripComboBox1.SelectedIndex == 0 ? Token.Statuses.UserTimeline(x) : Token.Favorites.List(x))
                {
                    if (t.ExtendedEntities != null)
                        foreach (var i in t.ExtendedEntities.Media)
                            Pages.Add(Tuple.Create(i.MediaUrl + ":large", t));
                    else if (t.Entities != null && t.Entities.Media != null)
                        foreach (var i in t.Entities.Media)
                            Pages.Add(Tuple.Create(i.MediaUrl + ":large", t));
                }
                trackBar1.Maximum = Pages.Count - 1;
                UpdateImage();
            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
            }
          
        }

        CancellationTokenSource cts;

        void UpdateImage()
        {
            var p = Pages[Index];

            if (cts != null) cts.Cancel();
            cts = new CancellationTokenSource();
            new Task(() =>
            {
                var r = WebRequest.Create(p.Item1);
                using (var e = r.GetResponse())
                using (var s = e.GetResponseStream())
                {
                    this.Invoke(new Action(() => pictureBox1.Image = Image.FromStream(s)));
                }
            }, cts.Token).Start();

            if (Index == Pages.Count - 1)
                LoadImage();

            button1.Enabled = Index != 0;
            button2.Enabled = Index < Pages.Count - 1;
            openInWebToolStripMenuItem.Enabled = saveToolStripMenuItem.Enabled = trackBar1.Enabled = true;
            toolStripStatusLabel1.Text = string.Format("@{2}: {3} ({0}/{1})", Index, Pages.Count, Pages[Index].Item2.User.ScreenName, Pages[Index].Item2.Text);
            if(!trackBar1.Focused)
                trackBar1.Value = Index;
        }

        void GoNext()
        {
            if(Index < Pages.Count - 1)
            {
                Index++;
                UpdateImage();
            }
        }

        void GoBack()
        {
            if (Index > 0)
            {
                Index--;
                UpdateImage();
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            LoadImage();
            toolStripTextBox1.Enabled = toolStripComboBox1.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            GoBack();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            GoNext();
        }

        private void openInWebToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(Pages.Count > 0)
                System.Diagnostics.Process.Start(string.Format("https://twitter.com/{0}/status/{1}", Pages[Index].Item2.User.ScreenName, Pages[Index].Item2.Id));
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        protected override bool ProcessCmdKey(ref Message m, Keys k)
        {
            if (k == Keys.Right || k == Keys.L || k == Keys.D)
                GoNext();
            if (k == Keys.Left || k  == Keys.H || k == Keys.A)
                GoBack();

            return base.ProcessCmdKey(ref m, k);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            Index = trackBar1.Value;
            UpdateImage();
        }
    }
}
