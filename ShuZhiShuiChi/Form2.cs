namespace ShuZhiShuiChi;

public partial class Form2 : Form
{
    private string path = "";
    public Form2()
    {
        InitializeComponent();
    }

    private void button1_Click(object sender, EventArgs e)
    {
        path = SelectFile();
    }
    public string SelectFile()
    {
        // 创建文件选择对话框实例
        OpenFileDialog openFileDialog = new OpenFileDialog();

        // 设置参数
        openFileDialog.Title = "请选择一个文件";
        openFileDialog.Filter = "Excel文件 (*.xlsx)|*.xlsx"; // 过滤文件类型
        openFileDialog.InitialDirectory = @"C:\"; // 默认打开目录
        openFileDialog.RestoreDirectory = true; // 关闭对话框后恢复原来的目录

        // 显示对话框并判断用户是否点击了“确定”
        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            // 返回选中的文件路径
            return openFileDialog.FileName;
        }

        // 如果用户取消，返回 null 或空字符串
        return null;
    }

    private void button2_Click(object sender, EventArgs e)
    {
        new f5(path, comboBox1.Text, double.Parse(textBox1.Text), double.Parse(textBox2.Text), comboBox2.Text,
            double.Parse(comboBox3.Text), comboBox4.Text, double.Parse(textBox3.Text), double.Parse(textBox4.Text));
    }
}