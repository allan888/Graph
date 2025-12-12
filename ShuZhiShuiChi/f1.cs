using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ScottPlot;
using SkiaSharp; // 需要 NuGet 安装: ScottPlot
// 需要 NuGet 安装: System.Text.Encoding.CodePages

namespace ShuZhiShuiChi
{
    class f1
    {
        // ======================================================
        // 全局设置
        // ======================================================
        // 目标文件夹
        string TARGET_DIR = "";

        // 是否保存图片
        const bool SAVE_FIGURES = true;

        public f1(string path)
        {
            TARGET_DIR = path;
            // 0. 注册编码支持 (为了支持 GBK 读取)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (!Directory.Exists(TARGET_DIR))
            {
                Console.WriteLine($"错误：文件夹路径不存在 -> {TARGET_DIR}");
                return;
            }

            // 输出路径
            string tablePath = Path.Combine(TARGET_DIR, "增阻统计表.csv");
            string figSaveDir = Path.Combine(TARGET_DIR, "图表结果");

            // 扫描文件 (查找 .txt 且包含 "时间历程")
            var files = Directory.GetFiles(TARGET_DIR, "*.txt");
            var txtFiles = files.Where(f => Path.GetFileName(f).Contains("时间历程")).ToList();

            Console.WriteLine($"在 {TARGET_DIR} 找到 {txtFiles.Count} 个数据文件。");
            Console.WriteLine(new string('-', 40));

            foreach (var fname in txtFiles)
            {
                ProcessSingleFile(fname, tablePath, figSaveDir);
            }

            Console.WriteLine(new string('-', 40));
            Console.WriteLine("全部完成。");
            Console.WriteLine($"CSV表已保存: {tablePath}");
            if (SAVE_FIGURES)
            {
                Console.WriteLine($"图片已保存至: {figSaveDir}");
            }
        }

        // ======================================================
        // 1. 从文件读取时间历程
        // ======================================================
        static (double[] t, double[] y) LoadTimeSeries(string filename, int colIndex = 1)
        {
            List<double> tList = new List<double>();
            List<double> yList = new List<double>();
            bool dataStarted = false;

            // 使用 GBK 编码读取
            using (var sr = new StreamReader(filename, Encoding.GetEncoding("GBK")))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) continue;

                    double tVal;
                    if (double.TryParse(parts[0], out tVal))
                    {
                        dataStarted = true;
                    }
                    else
                    {
                        if (!dataStarted) continue;
                        else continue;
                    }

                    if (parts.Length <= colIndex) continue;

                    double yVal;
                    if (double.TryParse(parts[colIndex], out yVal))
                    {
                        tList.Add(tVal);
                        yList.Add(yVal);
                    }
                }
            }

            if (tList.Count == 0)
                throw new Exception($"没有从文件 {Path.GetFileName(filename)} 中读到数据。");

            return (tList.ToArray(), yList.ToArray());
        }

        // ======================================================
        // 2. 用“零点上穿”划分周期
        // ======================================================
        class PeriodStats
        {
            public int[] PeriodIndex;
            public double[] MeanPer;
            public double[] AmpPer;
            public double[] CycleStarts;
            public double Level;
            public double[] CrossTimes;
        }

        static PeriodStats ComputePeriodStatsZeroCross(double[] t, double[] y, double? level = null,
            int minPointsPerCycle = 10)
        {
            if (level == null)
            {
                int idxStart = (int)(y.Length * 2.0 / 3.0);
                // 计算后1/3段的平均值
                double sum = 0;
                for (int i = idxStart; i < y.Length; i++) sum += y[i];
                level = sum / (y.Length - idxStart);
            }

            double[] yShift = new double[y.Length];
            for (int i = 0; i < y.Length; i++) yShift[i] = y[i] - level.Value;

            List<double> crossTimes = new List<double>();

            for (int i = 0; i < t.Length - 1; i++)
            {
                double y1 = yShift[i];
                double y2 = yShift[i + 1];

                if (y1 <= 0.0 && y2 > 0.0)
                {
                    double t1 = t[i];
                    double t2 = t[i + 1];
                    double frac = (y2 != y1) ? -y1 / (y2 - y1) : 0.0;
                    double tc = t1 + (t2 - t1) * frac;
                    crossTimes.Add(tc);
                }
            }

            if (crossTimes.Count < 2) return null;

            List<double> cycleStarts = new List<double>();
            List<double> meanPer = new List<double>();
            List<double> ampPer = new List<double>();

            for (int k = 0; k < crossTimes.Count - 1; k++)
            {
                double start = crossTimes[k];
                double end = crossTimes[k + 1];

                // 提取该周期内的数据
                List<double> seg = new List<double>();
                for (int i = 0; i < t.Length; i++)
                {
                    if (t[i] >= start && t[i] < end)
                        seg.Add(y[i]);
                }

                if (seg.Count < minPointsPerCycle) continue;

                cycleStarts.Add(start);
                meanPer.Add(seg.Average());
                ampPer.Add(0.5 * (seg.Max() - seg.Min()));
            }

            if (cycleStarts.Count == 0) return null;

            int[] pIndex = Enumerable.Range(1, cycleStarts.Count).ToArray();

            return new PeriodStats
            {
                PeriodIndex = pIndex,
                MeanPer = meanPer.ToArray(),
                AmpPer = ampPer.ToArray(),
                CycleStarts = cycleStarts.ToArray(),
                Level = level.Value,
                CrossTimes = crossTimes.ToArray()
            };
        }

        // ======================================================
        // 3. 自动判断收敛
        // ======================================================
        static (int startIdx, double convMean, double convAmp) DetectConvergence(
            double[] meanPer, double[] ampPer,
            int minConvergedCycles = 8,
            double tolMeanRel = 0.02,
            double tolAmpRel = 0.02)
        {
            int N = meanPer.Length;
            if (N < minConvergedCycles + 1)
                return (0, meanPer.Average(), ampPer.Average());

            int? bestStart = null;

            for (int start = 0; start <= N - minConvergedCycles; start++)
            {
                // 获取片段
                int count = N - start; // 实际上我们应该取从 start 到最后的片段，还是取固定窗口？Python代码取的是 start:
                // Python: m_seg = mean_per[start:] -> 取从start到结尾

                // 为了计算Diff，我们需要至少2个点
                if (count < 2) break;

                double[] mSeg = new double[count];
                Array.Copy(meanPer, start, mSeg, 0, count);

                double[] aSeg = new double[count];
                Array.Copy(ampPer, start, aSeg, 0, count);

                // 计算 RMS Change Mean
                double rmsChangeM = CalcRmsDiff(mSeg);
                double rmsLevelM = CalcRms(mSeg);
                if (rmsLevelM == 0) continue;
                double R_mean = rmsChangeM / rmsLevelM;

                // 计算 RMS Change Amp
                double rmsChangeA = CalcRmsDiff(aSeg);
                double rmsLevelA = CalcRms(aSeg);
                if (rmsLevelA == 0) continue;
                double R_amp = rmsChangeA / rmsLevelA;

                if (R_mean <= tolMeanRel && R_amp <= tolAmpRel)
                {
                    bestStart = start;
                    break;
                }
            }

            if (bestStart == null)
            {
                bestStart = Math.Max(0, N - minConvergedCycles);
            }

            // 计算收敛段的均值
            int finalStart = bestStart.Value;
            double finalMean = 0;
            double finalAmp = 0;
            int finalCount = N - finalStart;

            for (int i = finalStart; i < N; i++)
            {
                finalMean += meanPer[i];
                finalAmp += ampPer[i];
            }

            finalMean /= finalCount;
            finalAmp /= finalCount;

            return (finalStart, finalMean, finalAmp);
        }

        // 辅助函数：计算数组的 RMS (Root Mean Square)
        static double CalcRms(double[] arr)
        {
            double sumSq = 0;
            foreach (var v in arr) sumSq += v * v;
            return Math.Sqrt(sumSq / arr.Length);
        }

        // 辅助函数：计算数组差分的 RMS
        static double CalcRmsDiff(double[] arr)
        {
            if (arr.Length < 2) return 0;
            double sumSqDiff = 0;
            for (int i = 0; i < arr.Length - 1; i++)
            {
                double diff = arr[i + 1] - arr[i];
                sumSqDiff += diff * diff;
            }

            return Math.Sqrt(sumSqDiff / (arr.Length - 1));
        }

        // ======================================================
        // 4. 更新统计表
        // ======================================================
        static void UpdateSummaryTable(string tablePath, string keyStr, double meanVal, double ampVal)
        {
            List<string[]> dataRows = new List<string[]>();
            string[] header = { "频率(文件名后缀)", "增阻均值", "增阻幅值" };

            // 读取旧数据
            if (File.Exists(tablePath))
            {
                try
                {
                    using (var sr = new StreamReader(tablePath, Encoding.UTF8))
                    {
                        // 简单的 CSV 解析
                        string line = sr.ReadLine(); // header
                        if (line != null)
                        {
                            while ((line = sr.ReadLine()) != null)
                            {
                                if (string.IsNullOrWhiteSpace(line)) continue;
                                var parts = line.Split(',');
                                if (parts.Length > 0) dataRows.Add(parts);
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            // 过滤旧数据 (Key匹配)
            var newDataRows = new List<string[]>();
            double keyVal;
            bool keyIsNum = double.TryParse(keyStr, out keyVal);

            foreach (var row in dataRows)
            {
                if (row.Length == 0) continue;

                // 尝试比较频率
                double rowKeyVal;
                if (keyIsNum && double.TryParse(row[0], out rowKeyVal))
                {
                    if (Math.Abs(rowKeyVal - keyVal) < 1e-9) continue; // 覆盖
                }
                else
                {
                    if (row[0] == keyStr) continue;
                }

                newDataRows.Add(row);
            }

            // 添加新数据
            newDataRows.Add(new string[] { keyStr, meanVal.ToString("E6"), ampVal.ToString("E6") });

            // 排序
            newDataRows.Sort((a, b) =>
            {
                double va, vb;
                bool aIsNum = double.TryParse(a[0], out va);
                bool bIsNum = double.TryParse(b[0], out vb);
                if (aIsNum && bIsNum) return va.CompareTo(vb);
                return string.Compare(a[0], b[0]);
            });

            // 写入
            using (var sw = new StreamWriter(tablePath, false, new UTF8Encoding(true))) // 带BOM的UTF8
            {
                sw.WriteLine(string.Join(",", header));
                foreach (var row in newDataRows)
                {
                    sw.WriteLine(string.Join(",", row));
                }
            }
        }

       // ======================================================
        // 5. 单个文件处理逻辑 (修改版：第一张图无点，保留默认色)
        // ======================================================
        static void ProcessSingleFile(string filepath, string tablePath, string figSaveDir)
        {
            string filename = Path.GetFileName(filepath);
            Console.WriteLine($"正在处理: {filename}");

            // 1. 提取频率
            string nameNoExt = Path.GetFileNameWithoutExtension(filename);
            string prefix = "船舶波浪增阻时间历程";
            string keyStr = nameNoExt.StartsWith(prefix) ? nameNoExt.Substring(prefix.Length) : nameNoExt;
            if (string.IsNullOrEmpty(keyStr)) keyStr = "Unknown";

            // 2. 读数据
            double[] t, y;
            try
            {
                (t, y) = LoadTimeSeries(filepath, 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  读取失败: {ex.Message}");
                return;
            }

            // 3. 周期计算
            var res = ComputePeriodStatsZeroCross(t, y, null, 10);
            if (res == null)
            {
                Console.WriteLine("  周期划分失败 (数据不足或无零点上穿)。");
                return;
            }

            // 估算实测周期
            double? T_est_mean = null;
            if (res.CrossTimes.Length >= 2)
            {
                List<double> diffs = new List<double>();
                for (int i = 0; i < res.CrossTimes.Length - 1; i++)
                    diffs.Add(res.CrossTimes[i + 1] - res.CrossTimes[i]);

                int halfIdx = diffs.Count / 2;
                if (halfIdx < diffs.Count)
                {
                    double sum = 0;
                    int count = 0;
                    for (int i = halfIdx; i < diffs.Count; i++) { sum += diffs[i]; count++; }
                    T_est_mean = sum / count;
                }
            }

            // 4. 收敛判断
            var (convStartIdx, convMean, convAmp) = DetectConvergence(res.MeanPer, res.AmpPer, 8, 0.005, 0.005);
            int convStartCycle = convStartIdx + 1;
            double tConvStart = (convStartIdx < res.CycleStarts.Length) ? res.CycleStarts[convStartIdx] : res.CycleStarts.Last();

            Console.WriteLine($"  -> 提取频率: {keyStr} | 收敛均值: {convMean:E4}");

            // 5. 写表
            UpdateSummaryTable(tablePath, keyStr, convMean, convAmp);

            // 6. 画图 (ScottPlot 5)
            if (SAVE_FIGURES)
            {
                if (!Directory.Exists(figSaveDir)) Directory.CreateDirectory(figSaveDir);
                var paleBlueColor = ScottPlot.Color.FromHex("#1f77b4"); 
                // --- 创建三个独立的图表 ---
                ScottPlot.Plot plot1 = new();
                ScottPlot.Plot plot2 = new();
                ScottPlot.Plot plot3 = new();

                // --- 图1：原始时间历程 ---
                var line1 = plot1.Add.Scatter(t, y);
                line1.LegendText = "原始波浪增阻 R(t)";
                
                // 【关键修改】设置第一张图的 Marker 为 None，即不显示点，只显示线
                line1.MarkerStyle = MarkerStyle.None;

                // 参考线和区域依然使用默认风格（或最基础的灰色/半透明色，不影响整体默认感）
                var hLine1 = plot1.Add.HorizontalLine(res.Level);
                hLine1.LinePattern = LinePattern.Dashed;
                hLine1.Color = paleBlueColor.WithAlpha(0.2);
                hLine1.LineWidth = 0.8f;
                hLine1.LegendText = $"零点参考水平 ≈ {res.Level:E3} N";

                var span1 = plot1.Add.HorizontalSpan(tConvStart, t.Last());
                // 使用默认蓝色的半透明，确保不引入新颜色体系
                span1.FillStyle.Color = paleBlueColor.WithAlpha(0.2);

                plot1.XLabel("时间 t (s)");
                plot1.YLabel("波浪增阻 R(t) (N)");
                string title0 = $"波浪增阻时间历程（{keyStr}）";
                if (T_est_mean.HasValue) title0 += $"\n实测稳态周期 T_est ≈ {T_est_mean.Value:F3} s";
                plot1.Title(title0);
                plot1.ShowLegend();

                // --- 图2：每周期平均阻值 ---
                double[] pIdxDoubles = res.PeriodIndex.Select(i => (double)i).ToArray();
                var sc2 = plot2.Add.Scatter(pIdxDoubles, res.MeanPer);
                // 图2保留显示点(MarkerShape.FilledCircle是默认)
                sc2.LegendText = "每周期平均阻值";

                var hLine2 = plot2.Add.HorizontalLine(convMean);
                hLine2.LinePattern = LinePattern.Dashed;
                hLine2.LegendText = $"收敛均值 ≈ {convMean:E3} N";
                hLine2.Color = paleBlueColor.WithAlpha(0.2);

                var span2 = plot2.Add.HorizontalSpan(convStartCycle - 0.5, res.PeriodIndex.Length + 0.5);
                span2.FillStyle.Color =paleBlueColor.WithAlpha(0.2);

                plot2.XLabel("周期编号 n");
                plot2.YLabel("每周期平均阻值 (N)");
                plot2.Title("每周期平均波浪增阻");
                plot2.ShowLegend();

                // --- 图3：每周期幅值 ---
                var sc3 = plot3.Add.Scatter(pIdxDoubles, res.AmpPer);
                // 图3保留显示点
                sc3.LegendText = "每周期幅值";

                var hLine3 = plot3.Add.HorizontalLine(convAmp);
                hLine3.LinePattern = LinePattern.Dashed;
                hLine3.LegendText = $"收敛幅值 ≈ {convAmp:E3} N";
                hLine3.Color = paleBlueColor.WithAlpha(0.2);

                var span3 = plot3.Add.HorizontalSpan(convStartCycle - 0.5, res.PeriodIndex.Length + 0.5);
                span3.FillStyle.Color = paleBlueColor.WithAlpha(0.2);

                plot3.XLabel("周期编号 n");
                plot3.YLabel("每周期幅值 (N)");
                plot3.Title("每周期波浪增阻幅值");
                plot3.ShowLegend();

                // 【关键】在所有设置完成后，调用字体修复
                SetChineseFont(plot1);
                SetChineseFont(plot2);
                SetChineseFont(plot3);

                // === 拼图逻辑 (保持不变，确保不覆盖) ===
                int width = 1000;
                int height = 1200;
                int hOne = height / 3;

                byte[] bytes1 = plot1.GetImageBytes(width, hOne, ImageFormat.Png);
                byte[] bytes2 = plot2.GetImageBytes(width, hOne, ImageFormat.Png);
                byte[] bytes3 = plot3.GetImageBytes(width, hOne, ImageFormat.Png);

                using var surf = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(width, height));
                using var canvas = surf.Canvas;
                // 这里必须 Clear(White)，否则默认是透明背景，保存出来可能看不太清
                // 这不属于“改变颜色”，而是设定画布底色。
                canvas.Clear(SkiaSharp.SKColors.White);

                using (var img1 = SkiaSharp.SKImage.FromEncodedData(SkiaSharp.SKData.CreateCopy(bytes1)))
                using (var img2 = SkiaSharp.SKImage.FromEncodedData(SkiaSharp.SKData.CreateCopy(bytes2)))
                using (var img3 = SkiaSharp.SKImage.FromEncodedData(SkiaSharp.SKData.CreateCopy(bytes3)))
                {
                    canvas.DrawImage(img1, 0f, 0f);
                    canvas.DrawImage(img2, 0f, (float)hOne);
                    canvas.DrawImage(img3, 0f, (float)hOne * 2);
                }

                string savePath = Path.Combine(figSaveDir, $"Figure_{keyStr}.png");
                using var finalImage = surf.Snapshot();
                using var data = finalImage.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                using var stream = File.OpenWrite(savePath);
                data.SaveTo(stream);

                Console.WriteLine($"   [Info] 图片已保存: {Path.GetFileName(savePath)}");
            }
        }

        // ======================================================
        // 辅助：强制设置中文字体 (终极修复版)
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