namespace ShuZhiShuiChi;

public partial class Form1 : Form
{
    public string S1 = "";
    public string S2 = "";
    public Form1()
    {
        InitializeComponent();
    }

    private void button1_Click(object sender, EventArgs e)
    {
        S1 = SelectFolder();
    }
    public string SelectFolder()
    {
        // 创建对话框实例
        FolderBrowserDialog dialog = new FolderBrowserDialog();
    
        // 设置对话框的说明信息
        dialog.Description = "请选择一个文件夹";
    
        // 允许创建新文件夹 (可选)
        dialog.ShowNewFolderButton = true;

        // 显示对话框，并判断用户是否点击了“确定”
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            // 返回选择的路径
            string selectedPath = dialog.SelectedPath;
            return selectedPath;
        }

        // 用户点击了取消
        return string.Empty;
    }

    private void button2_Click(object sender, EventArgs e)
    {
        S2 = SelectFolder();
    }

    private void button3_Click(object sender, EventArgs e)
    {
        if (checkBox1.Checked)
        {
            if (S1 != "")
            {
                new f1(S1);
                new f3(S1);
            }
            else
            {
                MessageBox.Show("路径不能为空");
            }
        }
        if (checkBox2.Checked)
        {
            if (S2 != "")
            {
                new f2(S2);
                new f4(S2);
            }
            else
            {
                MessageBox.Show("路径不能为空");
            }
        }
        MessageBox.Show("计算完毕");
    }

    private void label2_Click(object sender, EventArgs e)
    {
        
    }
}