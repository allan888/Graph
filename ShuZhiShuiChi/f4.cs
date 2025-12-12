using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ScottPlot;
using SkiaSharp; 
// 确保安装 NuGet 包: 
// 1. ScottPlot (v5.0.x)
// 2. SkiaSharp.NativeAssets.Linux.NoDependencies (如果是Linux) 或 SkiaSharp
// 3. System.Text.Encoding.CodePages

namespace ShuZhiShuiChi
{
    class f4
    {
        // ======================================================
        // 1. 配置区域
        // ======================================================
        string DATA_DIR = "";
        const double L_REF = 325.5; 
        const double G = 9.81;

        // 周期统计参数
        const int MIN_POINTS_PER_CYCLE = 10;
        const int TAKE_LAST_CYCLES = 10; // 取最后10个周期计算平均 (最稳健的方式)

        public f4(string path)
        {
            DATA_DIR =  path;
            // === 核心修复1：防止控制台乱码 ===
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (!Directory.Exists(DATA_DIR))
            {
                Console.WriteLine($"[错误] 找不到文件夹路径：{DATA_DIR}");
                return;
            }

            // 扫描文件
            var files = Directory.GetFiles(DATA_DIR, "*.txt", SearchOption.AllDirectories)
                                 .Where(f => Path.GetFileName(f).Contains("时间历程"))
                                 .Distinct().ToList();

            if (files.Count == 0)
            {
                Console.WriteLine($"[提示] 未在 {DATA_DIR} 找到符合条件的数据文件。");
                return;
            }

            Console.WriteLine($"找到 {files.Count} 个文件，开始计算 RAO...\n");
            
            // 打印表头
            Console.WriteLine($"{"文件名",-20} | {"波高Hs",-6} | {"垂荡Amp",-9} | {"垂荡RAO",-9} | {"纵摇Amp",-9} | {"纵摇RAO",-9}");
            Console.WriteLine(new string('-', 100));

            var results = new List<MotionResult>();

            foreach (var fp in files)
            {
                try
                {
                    var res = ProcessOneFile(fp);
                    if (res != null)
                    {
                        results.Add(res);
                        Console.WriteLine($"{Path.GetFileNameWithoutExtension(res.Filename),-20} | {res.WaveHeight,-6:F2} | {res.HeaveAmp,-9:F4} | {res.HeaveRAO,-9:F4} | {res.PitchAmp,-9:F4} | {res.PitchRAO,-9:F4}");
                    }
                }
                catch (Exception ex)
                {
                    // Console.WriteLine($"[跳过] {Path.GetFileName(fp)}: {ex.Message}");
                }
            }

            if (results.Count == 0)
            {
                Console.WriteLine("\n[警告] 没有计算出任何结果，请检查文件内容是否正确。");
                return;
            }

            // 按 Lambda/L 排序
            results = results.OrderBy(r => r.LambdaOverL).ToList();

            // 导出 CSV
            string outCsv = Path.Combine(DATA_DIR, "RAO_最终计算结果.csv");
            ExportCsv(results, outCsv);
            Console.WriteLine($"\n[成功] 数据已导出至: {outCsv}");

            // 绘图
            string pngHeave = Path.Combine(DATA_DIR, "RAO_垂荡曲线.png");
            string pngPitch = Path.Combine(DATA_DIR, "RAO_纵摇曲线.png");

            MakeRaoPlot(results, "Heave", pngHeave);
            MakeRaoPlot(results, "Pitch", pngPitch);

            Console.WriteLine($"[成功] 图片已生成:\n  -> {pngHeave}\n  -> {pngPitch}");
        }

        // ======================================================
        // 2. 数据结构
        // ======================================================
        class MotionResult
        {
            public string Filename { get; set; }
            public double Omega { get; set; }
            public double LambdaOverL { get; set; }
            public double WaveHeight { get; set; }
            public double HeaveAmp { get; set; }
            public double HeaveRAO { get; set; }
            public double PitchAmp { get; set; }
            public double PitchRAO { get; set; }
        }

        // ======================================================
        // 3. 核心计算
        // ======================================================
        static MotionResult ProcessOneFile(string fp)
        {
            double omega = ExtractOmega(fp);

            var loadRes = LoadMotionData(fp);
            if (loadRes == null) return null;

            double[] t = loadRes.Value.t;
            double[] yHeave = loadRes.Value.heave;
            double[] yPitch = loadRes.Value.pitch;
            double Hs = loadRes.Value.Hs;

            // 默认波高兜底
            if (Hs <= 0.001) Hs = 1.0; 
            double waveAmp = Hs / 2.0;

            // 计算幅值 (取最后10个周期的平均值)
            double heaveAmp = GetConvergedAmplitude(t, yHeave);
            double pitchAmp = GetConvergedAmplitude(t, yPitch);

            double lam = (omega > 0.001) ? (2 * Math.PI * G) / (omega * omega) : 0;
            
            return new MotionResult
            {
                Filename = Path.GetFileName(fp),
                Omega = omega,
                LambdaOverL = lam / L_REF,
                WaveHeight = Hs,
                HeaveAmp = heaveAmp,
                HeaveRAO = heaveAmp / waveAmp,
                PitchAmp = pitchAmp,
                PitchRAO = pitchAmp / waveAmp 
            };
        }

        static double GetConvergedAmplitude(double[] t, double[] y)
        {
            if (y.Length < 10) return 0;

            // 取后2/3段的数据来计算均值，去除直流分量
            double level = y.Skip((int)(y.Length * 0.6)).Average();
            double[] yShift = new double[y.Length];
            for (int i = 0; i < y.Length; i++) yShift[i] = y[i] - level;

            var amps = new List<double>();
            
            // 零点上穿循环
            for (int i = 0; i < yShift.Length - 1; i++)
            {
                if (yShift[i] <= 0 && yShift[i + 1] > 0)
                {
                    int j = i + 1;
                    while (j < yShift.Length - 1)
                    {
                        if (yShift[j] <= 0 && yShift[j + 1] > 0) break;
                        j++;
                    }

                    if (j < yShift.Length - 1)
                    {
                        double maxV = -1e9;
                        double minV = 1e9;
                        int pts = 0;
                        for (int k = i; k <= j; k++)
                        {
                            if (yShift[k] > maxV) maxV = yShift[k];
                            if (yShift[k] < minV) minV = yShift[k];
                            pts++;
                        }

                        if (pts >= MIN_POINTS_PER_CYCLE)
                        {
                            amps.Add((maxV - minV) / 2.0);
                        }
                        i = j - 1; 
                    }
                }
            }

            if (amps.Count == 0) return 0;

            // 取最后 N 个周期
            int countToTake = Math.Min(amps.Count, TAKE_LAST_CYCLES);
            return amps.Skip(amps.Count - countToTake).Average();
        }

        static (double[] t, double[] heave, double[] pitch, double Hs)? LoadMotionData(string fp)
        {
            try
            {
                var lines = File.ReadAllLines(fp, Encoding.GetEncoding("GBK"));
                double hs = 0;
                var tL = new List<double>();
                var hL = new List<double>(); // Col 3
                var pL = new List<double>(); // Col 5

                bool dataStarted = false;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // 解析波高：匹配 "0.100E+01" 这种格式
                    if (!dataStarted && line.Contains("有义波高"))
                    {
                        var m = Regex.Match(line, @"\d+\.\d+E[+\-]\d+"); 
                        if (m.Success) double.TryParse(m.Value, out hs);
                    }

                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    // 确保是数据行（第一列是数字）
                    if (parts.Length < 6) continue;
                    if (!double.TryParse(parts[0], out double t)) continue;

                    dataStarted = true;

                    if (double.TryParse(parts[3], out double h) && 
                        double.TryParse(parts[5], out double p))
                    {
                        tL.Add(t);
                        hL.Add(h);
                        pL.Add(p);
                    }
                }

                if (tL.Count == 0) return null;
                return (tL.ToArray(), hL.ToArray(), pL.ToArray(), hs);
            }
            catch { return null; }
        }

        static double ExtractOmega(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            var matches = Regex.Matches(name, @"(\d+\.\d+)");
            if (matches.Count > 0)
            {
                string valStr = matches[matches.Count - 1].Value;
                if (double.TryParse(valStr, out double val)) return val;
            }
            return 0;
        }

        // ======================================================
        // 4. 绘图与导出 (包含终极字体修复)
        // ======================================================
        static void ExportCsv(List<MotionResult> res, string path)
        {
            using var sw = new StreamWriter(path, false, new UTF8Encoding(true)); // 带BOM的UTF8
            sw.WriteLine("Filename,Omega(rad/s),Lambda/L,WaveHeight(m),HeaveAmp(m),HeaveRAO(m/m),PitchAmp(deg),PitchRAO(deg/m)");
            foreach (var r in res)
            {
                sw.WriteLine($"{r.Filename},{r.Omega},{r.LambdaOverL:F4},{r.WaveHeight},{r.HeaveAmp:F4},{r.HeaveRAO:F4},{r.PitchAmp:F4},{r.PitchRAO:F4}");
            }
        }

        static void MakeRaoPlot(List<MotionResult> res, string type, string path)
        {
            ScottPlot.Plot plot = new();

            var x = res.Select(r => r.LambdaOverL).ToArray();
            var y = (type == "Heave") 
                ? res.Select(r => r.HeaveRAO).ToArray() 
                : res.Select(r => r.PitchRAO).ToArray();

            // 绘制
            var sp = plot.Add.Scatter(x, y);
            sp.LineWidth = 2;
            sp.MarkerSize = 8;
            // 颜色：垂荡=蓝，纵摇=红
            sp.Color = (type == "Heave") ? ScottPlot.Color.FromHex("#1f77b4") : ScottPlot.Color.FromHex("#d62728");

            // 标题
            string titleCN = (type == "Heave") ? "垂荡运动响应算子 (Heave RAO)" : "纵摇运动响应算子 (Pitch RAO)";
            string yLabelCN = (type == "Heave") ? "RAO (m/m)" : "RAO (deg/m)";

            plot.Title(titleCN);
            plot.XLabel("波长船长比 (λ/L)");
            plot.YLabel(yLabelCN);

            plot.Grid.MajorLineColor = Colors.Black.WithAlpha(0.15);
            plot.Grid.LinePattern = LinePattern.Dotted;

            // === 核心修复2：动态检测中文字体 ===
            SetChineseFont(plot);

            plot.SavePng(path, 1000, 600);
        }

        static void SetChineseFont(ScottPlot.Plot plot)
        {
            // 1. 尝试动态查找系统里能显示中文的字体
            // 这是一个“询问系统”的过程，比硬编码 "SimHei" 更靠谱
            string fontName = "Microsoft YaHei"; // 默认兜底
            try 
            {
                var typeface = SKFontManager.Default.MatchCharacter('中');
                if (typeface != null)
                {
                    fontName = typeface.FamilyName;
                }
            }
            catch {}

            // 2. 暴力应用到所有元素
            try { plot.Font.Set(fontName); } catch { }

            // 标题
            plot.Axes.Title.Label.FontName = fontName;
            plot.Axes.Title.Label.FontSize = 18;
            plot.Axes.Title.Label.Bold = true;

            // 轴标签 & 刻度 (最容易乱码的地方)
            foreach (var ax in plot.Axes.GetAxes())
            {
                ax.Label.FontName = fontName;
                ax.Label.FontSize = 14;
                
                ax.TickLabelStyle.FontName = fontName; 
                ax.TickLabelStyle.FontSize = 12;
            }
        }
    }
}