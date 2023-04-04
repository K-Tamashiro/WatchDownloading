using System;
using System.Windows.Forms;
using System.IO;

namespace WatchDownloading
{

    public partial class Form1 : Form
    {
        NotifyIcon notifyIcon;

        private System.IO.FileSystemWatcher watcher = null;

        //string ExePath = Path.GetDirectoryName(Application.ExecutablePath) + @"\_settings.ini";

        string iniFileName = @"\_settings.ini";
        string ExePath = "";
        string WatchPath_from = @"\Downloads";
        string WatchPath_to = @"\Downloads";
        string WatchFile = "*.csv";
        string NotyfiIcon = Path.GetDirectoryName(Application.ExecutablePath) + @"\tetsujin.ico";

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;

        }

        //機動時に一階だけ呼び出す（常駐させる）
        private void Form1_Load(object sender, EventArgs e)
        {
            ExePath = AppdataSave.LocalAppData + @iniFileName;

            var ini = new IniFile(@ExePath);

            textBox1.Text = ini.GetString("Download", "From", Environment.GetEnvironmentVariable("USERPROFILE") + WatchPath_from);
            WatchPath_from = textBox1.Text;
            textBox2.Text = ini.GetString("Download", "To", Environment.GetEnvironmentVariable("USERPROFILE") + WatchPath_to);
            WatchPath_to = textBox2.Text;
            textBox3.Text = ini.GetString("Download", "File", WatchFile);
            WatchFile = textBox3.Text;

            if (ini.GetString("Option", "Copy", "True")=="True")
            {
                radioButton1.Checked = true;
                radioButton2.Checked = false;
            }
            else
            {
                radioButton1.Checked = false;
                radioButton2.Checked = true;
            }


            //初回起動でDirectoryが無ければ作成
            if (!Directory.Exists(AppdataSave.LocalAppData))
            {
                Directory.CreateDirectory(AppdataSave.LocalAppData);
            }
            //初回起動判定（iniファイルの存在確認で初回と判定する）
            if (File.Exists(@ExePath))
            {
                this.button1.PerformClick();  //--- ボタン1を押したことにする
                this.Load -= Form1_Load;
            }
        }

        //監視開始
        private void button1_Click(object sender, EventArgs e)
        {
            int err_cnt = 0;
            if (!Directory.Exists(WatchPath_from))
            {
                Console.WriteLine("コピー元フォルダーが存在しません");
                //メッセージボックスを表示する
                MessageBox.Show("コピー元フォルダーが存在しません",
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                err_cnt++;
            }
            if (!Directory.Exists(WatchPath_to))
            {
                Console.WriteLine("コピー先フォルダーが存在しません");
                //メッセージボックスを表示する
                MessageBox.Show("コピー先フォルダーが存在しません",
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                err_cnt++;
            }
            if (err_cnt == 0)
            {
                // タスクバーに表示しない
                this.ShowInTaskbar = false;
                this.setComponents();
                this.Hide();
                this.Validate();

                StartWatch();
            }
        }

        //タスクトレイアイコンのInitialize
        private void setComponents()
        {
            notifyIcon = new NotifyIcon();
            // アイコンの設定
            notifyIcon.Icon = Properties.Resources.serche_folder;
            // アイコンを表示する
            notifyIcon.Visible = true;
            // アイコンにマウスポインタを合わせたときのテキスト
            notifyIcon.Text = WatchPath_from + " フォルダー監視中";

            // コンテキストメニュー
            ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
            ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem.Text = "&終了";
            toolStripMenuItem.Click += ToolStripMenuItem_Click;
            contextMenuStrip.Items.Add(toolStripMenuItem);
            notifyIcon.ContextMenuStrip = contextMenuStrip;

            // NotifyIconのクリックイベント
            notifyIcon.Click += NotifyIcon_Click;
        }

        //タスクトレイアイコンのクリック
        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            //左クリックの場合はフォームを表示 右クリックはコンテキストが表示されるので何もしない
            if (((System.Windows.Forms.MouseEventArgs)e).Button == MouseButtons.Left)
            {
                //フォームを表示しているときは監視しない
                EndWatch();
                this.Visible = true;

                // タスクバーに表示する
                this.ShowInTaskbar = true;
                // Formの表示/非表示を反転
                this.Visible = true;
                //通知アイコンの削除（削除しないと重複するので）
                if (notifyIcon != null) { notifyIcon.Dispose(); }

            }
        }

        //監視終了 コンテキストで終了を選択
        private void ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //メッセージボックスを表示する
            DialogResult result = MessageBox.Show("監視を終了しますか？",
                "質問",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Exclamation,
                MessageBoxDefaultButton.Button2);

            //何が選択されたか調べる
            if (result == DialogResult.Yes)
            {
                //「はい」が選択された時
                Console.WriteLine("「はい」が選択されました");
                // アプリケーションの終了
                EndWatch();
                Application.Exit();
            }
            else if (result == DialogResult.No)
            {
                //「いいえ」が選択された時
                Console.WriteLine("「いいえ」が選択されました");
            }
            else if (result == DialogResult.Cancel)
            {
                //「キャンセル」が選択された時
                Console.WriteLine("「キャンセル」が選択されました");
            }

        }

        //監視開始
        private void StartWatch()
        {
            if (watcher != null) return;
            
            watcher = new System.IO.FileSystemWatcher();
            //監視するディレクトリを指定
            watcher.Path = WatchPath_from;
            //最終アクセス日時、最終更新日時、ファイル、フォルダ名の変更を監視する
            watcher.NotifyFilter =
                (System.IO.NotifyFilters.LastAccess
                | System.IO.NotifyFilters.LastWrite
                | System.IO.NotifyFilters.FileName
                | System.IO.NotifyFilters.DirectoryName);
            //csvファイルのみ監視
            watcher.Filter = WatchFile;
            //UIのスレッドにマーシャリングする
            //コンソールアプリケーションでの使用では必要ない
            watcher.SynchronizingObject = this;

            //イベントハンドラの追加 Createイベントのみ監視
            watcher.Created += new System.IO.FileSystemEventHandler(watcher_Changed);
            //watcher.Changed += new System.IO.FileSystemEventHandler(watcher_Changed);
            //watcher.Deleted += new System.IO.FileSystemEventHandler(watcher_Changed);
            //watcher.Renamed += new System.IO.RenamedEventHandler(watcher_Renamed);

            //監視を開始する
            watcher.EnableRaisingEvents = true;
            Console.WriteLine("監視を開始しました。");
        }

        //Button2のClickイベントハンドラ
        private void Button2_Click(object sender, System.EventArgs e)
        {
            EndWatch();
            Application.Exit();
        }
        //監視終了
        private void EndWatch()
        {
            try
            {
                //タスクトレイアイコンの削除
                //if (notifyIcon != null) { notifyIcon.Dispose(); }
                //監視を終了
                if (watcher != null)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                    watcher = null;
                    Console.WriteLine("監視を終了しました。");
                }
            }
            catch
            {
                //アプリ終了
                //Application.Exit();
            }
        }

        //イベントハンドラ
        private void watcher_Changed(System.Object source, System.IO.FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case System.IO.WatcherChangeTypes.Changed:
                    Console.WriteLine(
                        "ファイル 「" + e.FullPath + "」が変更されました。");
                    break;
                //監視はファイルが作成された場合のみ監視する
                case System.IO.WatcherChangeTypes.Created:
                    Console.WriteLine(
                        "ファイル 「" + e.FullPath + "」が作成されました。");
                    //コピー出来るか判定（ダウンロード直後はプロッセスロックがかかっているので）
                    EndWatch();
                    int cnt = 0;
                    while (!IsFileLocked(e.FullPath))
                    {
                        cnt++;
                        if(cnt > 10000)
                        {
                            break;
                        }
                        //ダウンロード直後はLockされているのでフリーになるまでループ
                    }
                    string fn = "";
                    if (WatchPath_to.Substring(WatchPath_to.Length - 1) == @"\")
                    {
                        fn = WatchPath_to + e.Name;
                    }
                    else
                    {
                        fn = WatchPath_to + @"\" + e.Name;
                    }

                    try
                    {
                        if (radioButton1.Checked == true)
                        {
                            //上書きコピー
                            File.Copy(e.FullPath, fn, true);
                            Console.WriteLine(
                                "ファイル 「" + e.FullPath + "」が「" + WatchPath_to + "」にコピーされました。");
                        }
                        else
                        {
                            //ファイル移動は上書き出来ないので予め削除メソッドを実行
                            File.Delete(fn);
                            //上書き移動
                            File.Move(e.FullPath, fn);
                            Console.WriteLine(
                                "ファイル 「" + e.FullPath + "」が「" + WatchPath_to + "」に移動されました。");
                        }
                    }
                    catch
                    {
                        EndWatch();
                        StartWatch();
                    }

                    break;
                case System.IO.WatcherChangeTypes.Deleted:
                    Console.WriteLine(
                        "ファイル 「" + e.FullPath + "」が削除されました。");
                    break;
            }
            StartWatch();
        }

        //ファイルがロックされているか確認
        private static bool IsFileLocked(string path)
        {
            FileStream stream = null;

            try
            {
                stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch
            {
                //フリー
                return true;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }
            //ロック中
            return false;
        }
        
        //リネームイベント
        private void watcher_Renamed(System.Object source,
            System.IO.RenamedEventArgs e)
        {
            Console.WriteLine(
                "ファイル 「" + e.FullPath + "」の名前が変更されました。");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SelectFolderDLG(1);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            SelectFolderDLG(2);
        }

        //フォルダー選択ダイアログ
        private void SelectFolderDLG(int A)
        {
            //FolderBrowserDialogクラスのインスタンスを作成
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            //上部に表示する説明テキストを指定する
            if (A == 1)
            {
                fbd.Description = "ダウンロードフォルダを指定してください。";
                fbd.SelectedPath = WatchPath_from;
            }
            else
            {
                fbd.Description = "コピー先のダウンロードフォルダを指定してください。";
                fbd.SelectedPath = WatchPath_to;
            }
            //ルートフォルダを指定する
            //デフォルトでDesktop
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            //最初に選択するフォルダを指定する
            //RootFolder以下にあるフォルダである必要がある
            //fbd.SelectedPath = @"%USERPROFILE%\\Downloads";
            //ユーザーが新しいフォルダを作成できるようにする
            //デフォルトでTrue
            fbd.ShowNewFolderButton = true;

            //ダイアログを表示する
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                //選択されたフォルダを表示する
                Console.WriteLine(fbd.SelectedPath);
                var ini = new IniFile(@ExePath);
                if (A == 1)
                {
                    ini.WriteString("Download", "From", fbd.SelectedPath); // 文字列を書き込み
                    textBox1.Text = fbd.SelectedPath;
                    WatchPath_from = textBox1.Text;
                }
                else
                {
                    ini.WriteString("Download", "To", fbd.SelectedPath); // 文字列を書き込み
                    textBox2.Text = fbd.SelectedPath;
                    WatchPath_to = textBox2.Text;
                }
            }
        }

        //ファイルフィルター登録
        private void button5_Click(object sender, EventArgs e)
        {
            var ini = new IniFile(@ExePath);
            ini.WriteString("Download", "File", textBox3.Text); // 文字列を書き込み
            WatchFile = textBox3.Text;
            //メッセージボックスを表示する
            MessageBox.Show("監視する拡張子を登録しました。",
                "登録",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            var ini = new IniFile(@ExePath);
            if (radioButton1.Checked == true)
            {
                ini.WriteString("Option", "Copy", @"True"); // 文字列を書き込み
            }
            else
            {
                ini.WriteString("Option", "Copy", @"False"); // 文字列を書き込み
            }
        }
    }
}