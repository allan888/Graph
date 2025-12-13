using System.ComponentModel;

namespace ShuZhiShuiChi;

partial class Form2
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
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
        button1 = new System.Windows.Forms.Button();
        label2 = new System.Windows.Forms.Label();
        comboBox1 = new System.Windows.Forms.ComboBox();
        label3 = new System.Windows.Forms.Label();
        textBox1 = new System.Windows.Forms.TextBox();
        label4 = new System.Windows.Forms.Label();
        textBox2 = new System.Windows.Forms.TextBox();
        label5 = new System.Windows.Forms.Label();
        comboBox2 = new System.Windows.Forms.ComboBox();
        label6 = new System.Windows.Forms.Label();
        comboBox3 = new System.Windows.Forms.ComboBox();
        label7 = new System.Windows.Forms.Label();
        comboBox4 = new System.Windows.Forms.ComboBox();
        label8 = new System.Windows.Forms.Label();
        textBox3 = new System.Windows.Forms.TextBox();
        label9 = new System.Windows.Forms.Label();
        textBox4 = new System.Windows.Forms.TextBox();
        button2 = new System.Windows.Forms.Button();
        SuspendLayout();
        // 
        // label1
        // 
        label1.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        label1.Location = new System.Drawing.Point(44, 40);
        label1.Name = "label1";
        label1.Size = new System.Drawing.Size(309, 71);
        label1.TabIndex = 0;
        label1.Text = "RAO.xlsx路径";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // button1
        // 
        button1.Location = new System.Drawing.Point(377, 41);
        button1.Name = "button1";
        button1.Size = new System.Drawing.Size(309, 71);
        button1.TabIndex = 1;
        button1.Text = "打开";
        button1.UseVisualStyleBackColor = true;
        button1.Click += button1_Click;
        // 
        // label2
        // 
        label2.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        label2.Location = new System.Drawing.Point(44, 111);
        label2.Name = "label2";
        label2.Size = new System.Drawing.Size(309, 71);
        label2.TabIndex = 2;
        label2.Text = "表格 omega 列是啥？";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // comboBox1
        // 
        comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        comboBox1.FormattingEnabled = true;
        comboBox1.Items.AddRange(new object[] { "auto", "abs", "enc" });
        comboBox1.Location = new System.Drawing.Point(376, 131);
        comboBox1.Name = "comboBox1";
        comboBox1.Size = new System.Drawing.Size(309, 32);
        comboBox1.TabIndex = 3;
        // 
        // label3
        // 
        label3.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        label3.Location = new System.Drawing.Point(44, 171);
        label3.Name = "label3";
        label3.Size = new System.Drawing.Size(309, 71);
        label3.TabIndex = 4;
        label3.Text = "输入有义波高 Hs (m)";
        label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // textBox1
        // 
        textBox1.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        textBox1.Location = new System.Drawing.Point(359, 192);
        textBox1.Name = "textBox1";
        textBox1.Size = new System.Drawing.Size(355, 34);
        textBox1.TabIndex = 5;
        // 
        // label4
        // 
        label4.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        label4.Location = new System.Drawing.Point(44, 242);
        label4.Name = "label4";
        label4.Size = new System.Drawing.Size(309, 71);
        label4.TabIndex = 6;
        label4.Text = "输入平均周期 T (s)";
        label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // textBox2
        // 
        textBox2.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        textBox2.Location = new System.Drawing.Point(359, 260);
        textBox2.Name = "textBox2";
        textBox2.Size = new System.Drawing.Size(355, 34);
        textBox2.TabIndex = 7;
        // 
        // label5
        // 
        label5.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        label5.Location = new System.Drawing.Point(44, 313);
        label5.Name = "label5";
        label5.Size = new System.Drawing.Size(309, 71);
        label5.TabIndex = 8;
        label5.Text = "你的 T 是 Tp 还是 Tz？";
        label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // comboBox2
        // 
        comboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        comboBox2.FormattingEnabled = true;
        comboBox2.Items.AddRange(new object[] { "Tp", "Tz" });
        comboBox2.Location = new System.Drawing.Point(377, 334);
        comboBox2.Name = "comboBox2";
        comboBox2.Size = new System.Drawing.Size(309, 32);
        comboBox2.TabIndex = 9;
        // 
        // label6
        // 
        label6.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        label6.Location = new System.Drawing.Point(44, 384);
        label6.Name = "label6";
        label6.Size = new System.Drawing.Size(309, 71);
        label6.TabIndex = 10;
        label6.Text = "换算系数";
        label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // comboBox3
        // 
        comboBox3.FormattingEnabled = true;
        comboBox3.Items.AddRange(new object[] { "1.41" });
        comboBox3.Location = new System.Drawing.Point(376, 405);
        comboBox3.Name = "comboBox3";
        comboBox3.Size = new System.Drawing.Size(309, 32);
        comboBox3.TabIndex = 11;
        // 
        // label7
        // 
        label7.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        label7.Location = new System.Drawing.Point(44, 455);
        label7.Name = "label7";
        label7.Size = new System.Drawing.Size(309, 71);
        label7.TabIndex = 12;
        label7.Text = "船速输入单位";
        label7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // comboBox4
        // 
        comboBox4.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        comboBox4.FormattingEnabled = true;
        comboBox4.Items.AddRange(new object[] { "ms", "kn" });
        comboBox4.Location = new System.Drawing.Point(376, 476);
        comboBox4.Name = "comboBox4";
        comboBox4.Size = new System.Drawing.Size(309, 32);
        comboBox4.TabIndex = 13;
        // 
        // label8
        // 
        label8.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        label8.Location = new System.Drawing.Point(44, 526);
        label8.Name = "label8";
        label8.Size = new System.Drawing.Size(309, 71);
        label8.TabIndex = 14;
        label8.Text = "输入船速";
        label8.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // textBox3
        // 
        textBox3.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        textBox3.Location = new System.Drawing.Point(359, 544);
        textBox3.Name = "textBox3";
        textBox3.Size = new System.Drawing.Size(355, 34);
        textBox3.TabIndex = 15;
        // 
        // label9
        // 
        label9.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        label9.Location = new System.Drawing.Point(44, 597);
        label9.Name = "label9";
        label9.Size = new System.Drawing.Size(309, 71);
        label9.TabIndex = 16;
        label9.Text = "输入波向角 beta";
        label9.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // textBox4
        // 
        textBox4.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        textBox4.Location = new System.Drawing.Point(359, 615);
        textBox4.Name = "textBox4";
        textBox4.Size = new System.Drawing.Size(355, 34);
        textBox4.TabIndex = 17;
        // 
        // button2
        // 
        button2.Font = new System.Drawing.Font("Microsoft YaHei UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        button2.Location = new System.Drawing.Point(253, 696);
        button2.Name = "button2";
        button2.Size = new System.Drawing.Size(432, 151);
        button2.TabIndex = 18;
        button2.Text = "计算";
        button2.UseVisualStyleBackColor = true;
        button2.Click += button2_Click;
        // 
        // Form2
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(11F, 24F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(965, 886);
        Controls.Add(button2);
        Controls.Add(textBox4);
        Controls.Add(label9);
        Controls.Add(textBox3);
        Controls.Add(label8);
        Controls.Add(comboBox4);
        Controls.Add(label7);
        Controls.Add(comboBox3);
        Controls.Add(label6);
        Controls.Add(comboBox2);
        Controls.Add(label5);
        Controls.Add(textBox2);
        Controls.Add(label4);
        Controls.Add(textBox1);
        Controls.Add(label3);
        Controls.Add(comboBox1);
        Controls.Add(label2);
        Controls.Add(button1);
        Controls.Add(label1);
        Text = "有义值计算程序";
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.Button button2;

    private System.Windows.Forms.TextBox textBox4;

    private System.Windows.Forms.Label label8;
    private System.Windows.Forms.TextBox textBox3;
    private System.Windows.Forms.Label label9;

    private System.Windows.Forms.Label label7;
    private System.Windows.Forms.ComboBox comboBox4;

    private System.Windows.Forms.Label label6;
    private System.Windows.Forms.ComboBox comboBox3;

    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.ComboBox comboBox2;

    private System.Windows.Forms.TextBox textBox2;

    private System.Windows.Forms.Label label4;

    private System.Windows.Forms.TextBox textBox1;

    private System.Windows.Forms.Label label3;

    private System.Windows.Forms.ComboBox comboBox1;

    private System.Windows.Forms.Button button1;
    private System.Windows.Forms.Label label2;

    private System.Windows.Forms.Label label1;

    #endregion
}