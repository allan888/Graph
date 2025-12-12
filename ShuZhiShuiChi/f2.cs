using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ScottPlot;
using SkiaSharp; // NuGet: ScottPlot
// NuGet: System.Text.Encoding.CodePages

namespace ShuZhiShuiChi
{
    class f2
    {
        // ======================================================
        // 全局配置
        // ======================================================
        string TARGET_DIR = "";
        const bool SAVE_FIGURES = true;

        public f2(string path)
        {
            TARGET_DIR = path;
            // 0. 注册编码支持 (解决 GBK 读取问题)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (!Directory.Exists(TARGET_DIR))
            {
                Console.WriteLine($"错误：找不到路径 {TARGET_DIR}");
                return;
            }

            // 输出路径
            string tablePath = Path.Combine(TARGET_DIR, "位移统计表.csv");
            string figSaveDir = Path.Combine(TARGET_DIR, "处理结果图表");

            // 扫描文件
            var files = Directory.GetFiles(TARGET_DIR, "*.txt");
            var dataFiles = files.Where(f => Path.GetFileName(f).Contains("时间历程")).ToList();

            Console.WriteLine($"在 {TARGET_DIR} 找到 {dataFiles.Count} 个数据文件。");
            Console.WriteLine(new string('-', 50));

            foreach (var fname in dataFiles)
            {
                ProcessSingleFile(fname, tablePath, figSaveDir);
            }

            Console.WriteLine(new string('-', 50));
            Console.WriteLine("全部完成。");
            Console.WriteLine($"CSV表已保存: {tablePath}");
            if (SAVE_FIGURES)
            {
                Console.WriteLine($"图片已保存至: {figSaveDir}");
            }
        }

        // ======================================================
        // 1. 读取文件 (只读取 垂荡:Col3 和 纵摇:Col5)
        // ======================================================
        static (double[] t, Dictionary<int, double[]> data) LoadTimeSeriesCols(string filepath, int[] colIndices)
        {
            List<double> tList = new List<double>();
            Dictionary<int, List<double>> dataLists = colIndices.ToDictionary(k => k, v => new List<double>());
            bool dataStarted = false;

            using (var sr = new StreamReader(filepath, Encoding.GetEncoding("GBK")))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    int maxIdx = colIndices.Max();
                    if (parts.Length < maxIdx + 1) continue;

                    double tVal;
                    // 尝试解析时间
                    if (!double.TryParse(parts[0], out tVal))
                    {
                        if (dataStarted) continue; // 数据中间出现非数字行
                        else continue; // 跳过表头
                    }
                    else
                    {
                        dataStarted = true;
                    }

                    // 尝试解析数据列
                    try
                    {
                        var currentVals = new Dictionary<int, double>();
                        bool parseSuccess = true;
                        foreach (int idx in colIndices)
                        {
                            if (double.TryParse(parts[idx], out double v))
                                currentVals[idx] = v;
                            else
                            {
                                parseSuccess = false;
                                break;
                            }
                        }

                        if (parseSuccess)
                        {
                            tList.Add(tVal);
                            foreach (int idx in colIndices)
                                dataLists[idx].Add(currentVals[idx]);
                        }
                    }
                    catch { continue; }
                }
            }

            if (tList.Count == 0)
                throw new Exception($"文件 {Path.GetFileName(filepath)} 未读取到有效数据。");

            var resultDict = new Dictionary<int, double[]>();
            foreach (var kvp in dataLists)
                resultDict[kvp.Key] = kvp.Value.ToArray();

            return (tList.ToArray(), resultDict);
        }

        // ======================================================
        // 2. 零点上穿划分周期 & 计算幅值
        // ======================================================
        class PeriodResult
        {
            public int[] PeriodIdx;
            public double[] MeanPer;
            public double[] AmpPer;
            public double[] CycleStarts;
        }

        static PeriodResult ComputePeriodStatsZeroCross(double[] t, double[] y, int minPointsPerCycle = 5)
        {
            int idxStart = (int)(y.Length * 2.0 / 3.0);
            // 计算后1/3均值
            double sum = 0; int count = 0;
            for (int i = idxStart; i < y.Length; i++) { sum += y[i]; count++; }
            double level = (count > 0) ? sum / count : 0;

            double[] yShift = y.Select(v => v - level).ToArray();
            List<double> crossTimes = new List<double>();

            for (int i = 0; i < t.Length - 1; i++)
            {
                if (yShift[i] <= 0.0 && yShift[i + 1] > 0.0)
                {
                    double t1 = t[i]; double t2 = t[i + 1];
                    double y1 = yShift[i]; double y2 = yShift[i + 1];
                    double frac = (y2 != y1) ? -y1 / (y2 - y1) : 0.0;
                    crossTimes.Add(t1 + (t2 - t1) * frac);
                }
            }

            if (crossTimes.Count < 2) return null;

            List<double> starts = new List<double>();
            List<double> means = new List<double>();
            List<double> amps = new List<double>();

            for (int k = 0; k < crossTimes.Count - 1; k++)
            {
                double start = crossTimes[k];
                double end = crossTimes[k + 1];
                
                // 提取片段
                var seg = new List<double>();
                for(int i=0; i<t.Length; i++)
                {
                    if (t[i] >= start && t[i] < end) seg.Add(y[i]);
                }

                if (seg.Count < minPointsPerCycle) continue;

                starts.Add(start);
                means.Add(seg.Average());
                amps.Add(0.5 * (seg.Max() - seg.Min()));
            }

            if (starts.Count == 0) return null;

            return new PeriodResult
            {
                PeriodIdx = Enumerable.Range(1, starts.Count).ToArray(),
                CycleStarts = starts.ToArray(),
                MeanPer = means.ToArray(),
                AmpPer = amps.ToArray()
            };
        }

        // ======================================================
        // 3. 收敛性判断
        // ======================================================
        static (int startIdx, double convMean, double convAmp) DetectConvergence(
             double[] meanPer, double[] ampPer, int minConvergedCycles = 12, double tol = 0.02)
        {
            int N = meanPer.Length;
            if (N < minConvergedCycles + 1)
                return (0, meanPer.Average(), ampPer.Average());

            int bestStart = Math.Max(0, N - minConvergedCycles);

            for (int start = 0; start <= N - minConvergedCycles; start++)
            {
                int len = N - start;
                if (len < 2) break;

                // 提取片段
                double[] mSeg = meanPer.Skip(start).ToArray();
                double[] aSeg = ampPer.Skip(start).ToArray();

                double meanSqM = mSeg.Select(x => x * x).Average();
                double meanSqA = aSeg.Select(x => x * x).Average();

                if (meanSqM == 0 || meanSqA == 0) continue;

                double diffSqMeanM = Enumerable.Range(0, mSeg.Length - 1)
                    .Select(i => Math.Pow(mSeg[i + 1] - mSeg[i], 2)).Average();
                double diffSqMeanA = Enumerable.Range(0, aSeg.Length - 1)
                    .Select(i => Math.Pow(aSeg[i + 1] - aSeg[i], 2)).Average();

                double rmsChangeM = Math.Sqrt(diffSqMeanM) / Math.Sqrt(meanSqM);
                double rmsChangeA = Math.Sqrt(diffSqMeanA) / Math.Sqrt(meanSqA);

                if (rmsChangeM <= tol && rmsChangeA <= tol)
                {
                    bestStart = start;
                    break;
                }
            }

            // 计算最终均值
            double[] finalM = meanPer.Skip(bestStart).ToArray();
            double[] finalA = ampPer.Skip(bestStart).ToArray();
            return (bestStart, finalM.Average(), finalA.Average());
        }

        // ======================================================
        // 4. 更新 CSV 统计表
        // ======================================================
        static void UpdateMotionSummaryTable(string csvPath, string freqStr, double heaveAmp, double pitchAmp)
        {
            List<string[]> dataRows = new List<string[]>();
            string[] header = { "频率", "垂荡平均幅值(m)", "纵摇平均幅值(deg)" };

            if (File.Exists(csvPath))
            {
                try
                {
                    using (var sr = new StreamReader(csvPath, new UTF8Encoding(true)))
                    {
                        string line = sr.ReadLine(); // header
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                                dataRows.Add(line.Split(','));
                        }
                    }
                }
                catch { }
            }

            // 过滤旧数据
            var newDataRows = new List<string[]>();
            double currentFreq = double.TryParse(freqStr, out double v) ? v : 0.0;

            foreach (var row in dataRows)
            {
                if (row.Length == 0) continue;
                if (double.TryParse(row[0], out double rFreq))
                {
                    if (Math.Abs(rFreq - currentFreq) < 1e-6) continue;
                }
                newDataRows.Add(row);
            }

            newDataRows.Add(new string[] { freqStr, heaveAmp.ToString("F6"), pitchAmp.ToString("F6") });

            // 排序
            newDataRows.Sort((a, b) =>
            {
                double fa = double.TryParse(a[0], out double va) ? va : double.MaxValue;
                double fb = double.TryParse(b[0], out double vb) ? vb : double.MaxValue;
                return fa.CompareTo(fb);
            });

            using (var sw = new StreamWriter(csvPath, false, new UTF8Encoding(true)))
            {
                sw.WriteLine(string.Join(",", header));
                foreach (var row in newDataRows) sw.WriteLine(string.Join(",", row));
            }
        }

        // ======================================================
        // 5. 单文件处理 (包含绘图逻辑)
        // ======================================================
        static void ProcessSingleFile(string filepath, string tablePath, string figSaveDir)
        {
            string filename = Path.GetFileName(filepath);
            Console.WriteLine($"\n>>> 正在处理文件：{filename}");

            // 1. 提取频率
            string nameNoExt = Path.GetFileNameWithoutExtension(filename);
            string prefix = "船舶位移时间历程";
            string freqInput = nameNoExt.StartsWith(prefix) ? nameNoExt.Substring(prefix.Length) : nameNoExt;
            if (string.IsNullOrEmpty(freqInput)) freqInput = "Unknown";

            // 2. 读取数据
            int colHeave = 3;
            int colPitch = 5;
            double[] t;
            Dictionary<int, double[]> dataDict;
            try
            {
                (t, dataDict) = LoadTimeSeriesCols(filepath, new[] { colHeave, colPitch });
            }
            catch (Exception e)
            {
                Console.WriteLine($"   [Error] 读取失败: {e.Message}");
                return;
            }

            double[] yHeave = dataDict[colHeave];
            double[] yPitch = dataDict[colPitch];

            // 3. 计算统计量
            var resHeave = ComputePeriodStatsZeroCross(t, yHeave);
            var resPitch = ComputePeriodStatsZeroCross(t, yPitch);

            double ampHMean = 0.0;
            double ampPMean = 0.0;
            int? startCycleH = null, startCycleP = null;

            if (resHeave != null)
            {
                // 【修正】注意这里：var (idx, _, amp)
                // 之前写成了 (idx, mean, _)，导致取到了 oscillation mean (接近0)
                // 现在取第三个参数：amplitude mean (幅值均值)
                var (idx, _, amp) = DetectConvergence(resHeave.MeanPer, resHeave.AmpPer);
                
                ampHMean = amp; // 赋值给 ampHMean，这样画线就在幅值高度了
                startCycleH = resHeave.PeriodIdx[idx];
                Console.WriteLine($"   [垂荡] 收敛幅值 = {ampHMean:F4} m");
            }

            if (resPitch != null)
            {
                // 【修正】同上，取第三个参数
                var (idx, _, amp) = DetectConvergence(resPitch.MeanPer, resPitch.AmpPer);
                
                ampPMean = amp; 
                startCycleP = resPitch.PeriodIdx[idx];
                Console.WriteLine($"   [纵摇] 收敛幅值 = {ampPMean:F4} deg");
            }

            // 4. 写入 CSV
            UpdateMotionSummaryTable(tablePath, freqInput, ampHMean, ampPMean);

            // ==================================================
            // 5. 画图 (ScottPlot 5)
            // ==================================================
            if (SAVE_FIGURES)
            {
                if (!Directory.Exists(figSaveDir)) Directory.CreateDirectory(figSaveDir);

                // 创建两个独立的图表
                ScottPlot.Plot plot1 = new();
                ScottPlot.Plot plot2 = new();

                // 颜色定义 (完美还原 Python 中的颜色)
                // Python 'tab:blue' 的 HEX 值是 #1f77b4
                var paleBlueColor = ScottPlot.Color.FromHex("#1f77b4"); 
                
                // 深灰色
                // var darkGray = ScottPlot.Color.FromHex("#333333"); 
                var darkGray = paleBlueColor; 
                // 坐标轴黑色
                var axisColor = ScottPlot.Colors.Black;

                // === 图1: 垂荡 (Heave) ===
                if (resHeave != null)
                {
                    double[] px = resHeave.PeriodIdx.Select(x => (double)x).ToArray();
                    
                    // 曲线 + 点 (Python: marker='o', markersize=5)
                    var sp = plot1.Add.Scatter(px, resHeave.AmpPer);
                    sp.Color = paleBlueColor;
                    sp.MarkerShape = MarkerShape.FilledCircle;
                    sp.MarkerSize = 5;
                    sp.LineWidth = 1.5f;
                    sp.LegendText = "每周期幅值";

                    // 均值线 (Python: color='#333333', linestyle='--')
                    var hl = plot1.Add.HorizontalLine(ampHMean);
                    hl.Color = darkGray;
                    hl.LinePattern = LinePattern.Dashed;
                    hl.LineWidth = 1.5f;
                    hl.LegendText = $"收敛均值={ampHMean:F4}m";

                    // 阴影区域
                    if (startCycleH.HasValue)
                    {
                        var span = plot1.Add.HorizontalSpan(startCycleH.Value - 0.5, resHeave.PeriodIdx.Last() + 0.5);
                        span.FillStyle.Color = paleBlueColor.WithAlpha(0.2);
                        // Span legend hack usually not needed, kept clean
                    }
                }
                
                // 设置样式 (还原 Python 风格)
                plot1.Axes.Color(axisColor); // 边框和刻度黑色
                plot1.Grid.MajorLineColor = Colors.Black.WithAlpha(0.2); // grid alpha=0.6 equivalent logic
                plot1.Grid.LinePattern = LinePattern.Dotted; // linestyle=':'

                plot1.YLabel("垂荡幅值 (m)");
                plot1.Title($"垂荡幅值收敛过程 (f={freqInput})");
                plot1.ShowLegend();

                // === 图2: 纵摇 (Pitch) ===
                if (resPitch != null)
                {
                    double[] px = resPitch.PeriodIdx.Select(x => (double)x).ToArray();

                    // 曲线 + 点 (Python: marker='s', markersize=5)
                    var sp = plot2.Add.Scatter(px, resPitch.AmpPer);
                    sp.Color = paleBlueColor;
                    sp.MarkerShape = MarkerShape.FilledSquare; // 纵摇用方块点
                    sp.MarkerSize = 5;
                    sp.LineWidth = 1.5f;
                    sp.LegendText = "每周期幅值";

                    // 均值线
                    var hl = plot2.Add.HorizontalLine(ampPMean);
                    hl.Color = darkGray;
                    hl.LinePattern = LinePattern.Dashed;
                    hl.LineWidth = 1.5f;
                    hl.LegendText = $"收敛均值={ampPMean:F4}°";

                    // 阴影区域
                    if (startCycleP.HasValue)
                    {
                        var span = plot2.Add.HorizontalSpan(startCycleP.Value - 0.5, resPitch.PeriodIdx.Last() + 0.5);
                        span.FillStyle.Color = paleBlueColor.WithAlpha(0.2);
                    }
                    
                    // 强制整数刻度 (Python: MaxNLocator(integer=True))
                    // ScottPlot 自动刻度通常很好，强制整数可以这样：
                    plot2.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic { IntegerTicksOnly = true };
                }

                // 设置样式
                plot2.Axes.Color(axisColor);
                plot2.Grid.MajorLineColor = Colors.Black.WithAlpha(0.2);
                plot2.Grid.LinePattern = LinePattern.Dotted;

                plot2.XLabel("周期编号 (Period Index)");
                plot2.YLabel("纵摇幅值 (deg)");
                plot2.Title($"纵摇幅值收敛过程 (f={freqInput})");
                plot2.ShowLegend();

                // ★★★ 关键：设置中文字体 (解决方块) ★★★
                SetChineseFont(plot1);
                SetChineseFont(plot2);

                // === 拼图保存逻辑 (解决遮挡和布局问题) ===
                int width = 1200;
                int totalHeight = 1000;
                int hOne = totalHeight / 2; // 上下各一半

                // 1. 获取两张图的字节流
                // 使用 ScottPlot.ImageFormat.Png 指定格式
                byte[] bytes1 = plot1.GetImageBytes(width, hOne, ScottPlot.ImageFormat.Png);
                byte[] bytes2 = plot2.GetImageBytes(width, hOne, ScottPlot.ImageFormat.Png);

                // 2. 拼图
                using var surf = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(width, totalHeight));
                using var canvas = surf.Canvas;
                canvas.Clear(SkiaSharp.SKColors.White); // 必须设置白底

                using (var img1 = SkiaSharp.SKImage.FromEncodedData(SkiaSharp.SKData.CreateCopy(bytes1)))
                using (var img2 = SkiaSharp.SKImage.FromEncodedData(SkiaSharp.SKData.CreateCopy(bytes2)))
                {
                    canvas.DrawImage(img1, 0f, 0f);
                    canvas.DrawImage(img2, 0f, (float)hOne);
                }

                string savePath = Path.Combine(figSaveDir, $"Motion_{freqInput}.png");
                using var finalImage = surf.Snapshot();
                using var data = finalImage.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                using var stream = File.OpenWrite(savePath);
                data.SaveTo(stream);

                Console.WriteLine($"   [Info] 图片已保存: {Path.GetFileName(savePath)}");
            }
        }

        // ======================================================
        // 辅助：强制设置中文字体 (SimHei 修复版)
        // ======================================================
        static void SetChineseFont(ScottPlot.Plot plot)
        {
            // 首选微软雅黑，备选黑体 (防止某些系统识别不出YaHei)
            // 在 SkiaSharp 中，字体名称必须严格匹配
            string fontName = "Songti SC"; 

            // 1. 遍历所有坐标轴 (上下左右)，设置 轴标题 和 刻度数值
            foreach (var axis in plot.Axes.GetAxes())
            {
                // 设置轴名称 (比如 "时间 t (s)")
                axis.Label.FontName = fontName;
                axis.Label.FontSize = 14; 

                // 设置刻度数值 (比如 "0.0", "1.0") -> 这是最容易变方块的地方
                axis.TickLabelStyle.FontName = fontName;
                axis.TickLabelStyle.FontSize = 11;
            }

            // 2. 设置主标题
            plot.Axes.Title.Label.FontName = fontName;
            plot.Axes.Title.Label.FontSize = 16;
            // 确保加粗 (可选)
            plot.Axes.Title.Label.Bold = true;

            // 3. 设置图例字体 (Legend)
            // 注意：图例字体的设置属性在不同版本中可能略有不同
            try
            {
                plot.Legend.FontName = SKFontManager.Default.MatchCharacter('汉').FamilyName;
                plot.Legend.FontSize = 12;
            }
            catch
            {
                // 忽略版本差异导致的异常
            }
            plot.Font.Automatic();
        }
    }
}