namespace ShuZhiShuiChi;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        label1 = new System.Windows.Forms.Label();
        label2 = new System.Windows.Forms.Label();
        button1 = new System.Windows.Forms.Button();
        button2 = new System.Windows.Forms.Button();
        checkBox1 = new System.Windows.Forms.CheckBox();
        checkBox2 = new System.Windows.Forms.CheckBox();
        button3 = new System.Windows.Forms.Button();
        label3 = new System.Windows.Forms.Label();
        button4 = new System.Windows.Forms.Button();
        SuspendLayout();
        // 
        // label1
        // 
        label1.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        label1.Location = new System.Drawing.Point(45, 76);
        label1.Name = "label1";
        label1.Size = new System.Drawing.Size(287, 66);
        label1.TabIndex = 0;
        label1.Text = "船舶波浪增阻时间历程路径";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // label2
        // 
        label2.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        label2.Location = new System.Drawing.Point(45, 178);
        label2.Name = "label2";
        label2.Size = new System.Drawing.Size(287, 66);
        label2.TabIndex = 1;
        label2.Text = "船舶位移时间历程";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        label2.Click += label2_Click;
        // 
        // button1
        // 
        button1.Location = new System.Drawing.Point(362, 77);
        button1.Name = "button1";
        button1.Size = new System.Drawing.Size(287, 66);
        button1.TabIndex = 2;
        button1.Text = "选择路径";
        button1.UseVisualStyleBackColor = true;
        button1.Click += button1_Click;
        // 
        // button2
        // 
        button2.Location = new System.Drawing.Point(362, 179);
        button2.Name = "button2";
        button2.Size = new System.Drawing.Size(287, 66);
        button2.TabIndex = 3;
        button2.Text = "选择路径";
        button2.UseVisualStyleBackColor = true;
        button2.Click += button2_Click;
        // 
        // checkBox1
        // 
        checkBox1.Location = new System.Drawing.Point(690, 89);
        checkBox1.Name = "checkBox1";
        checkBox1.Size = new System.Drawing.Size(156, 44);
        checkBox1.TabIndex = 4;
        checkBox1.Text = "启用";
        checkBox1.UseVisualStyleBackColor = true;
        // 
        // checkBox2
        // 
        checkBox2.Location = new System.Drawing.Point(690, 191);
        checkBox2.Name = "checkBox2";
        checkBox2.Size = new System.Drawing.Size(156, 44);
        checkBox2.TabIndex = 5;
        checkBox2.Text = "启用";
        checkBox2.UseVisualStyleBackColor = true;
        // 
        // button3
        // 
        button3.Font = new System.Drawing.Font("Microsoft YaHei UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        button3.Location = new System.Drawing.Point(234, 431);
        button3.Name = "button3";
        button3.Size = new System.Drawing.Size(485, 113);
        button3.TabIndex = 6;
        button3.Text = "计算";
        button3.UseVisualStyleBackColor = true;
        button3.Click += button3_Click;
        // 
        // label3
        // 
        label3.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        label3.Location = new System.Drawing.Point(45, 278);
        label3.Name = "label3";
        label3.Size = new System.Drawing.Size(287, 66);
        label3.TabIndex = 7;
        label3.Text = "有义值计算程序";
        label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        label3.Click += label3_Click;
        // 
        // button4
        // 
        button4.Location = new System.Drawing.Point(362, 279);
        button4.Name = "button4";
        button4.Size = new System.Drawing.Size(287, 66);
        button4.TabIndex = 8;
        button4.Text = "进入";
        button4.UseVisualStyleBackColor = true;
        button4.Click += button4_Click;
        // 
        // Form1
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(11F, 24F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(965, 586);
        Controls.Add(button4);
        Controls.Add(label3);
        Controls.Add(button3);
        Controls.Add(checkBox2);
        Controls.Add(checkBox1);
        Controls.Add(button2);
        Controls.Add(button1);
        Controls.Add(label2);
        Controls.Add(label1);
        Text = "曲线生成器";
        ResumeLayout(false);
    }

    private System.Windows.Forms.Button button4;

    private System.Windows.Forms.Label label3;

    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Button button1;
    private System.Windows.Forms.Button button2;
    private System.Windows.Forms.CheckBox checkBox1;
    private System.Windows.Forms.CheckBox checkBox2;
    private System.Windows.Forms.Button button3;

    private System.Windows.Forms.Label label1;

    #endregion
}