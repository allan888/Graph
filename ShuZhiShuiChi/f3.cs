using System.Text;
using System.Text.RegularExpressions;
using ScottPlot;
using SkiaSharp; // 需要 NuGet 安装: ScottPlot
// 需要 NuGet 安装: System.Text.Encoding.CodePages

namespace ShuZhiShuiChi
{
    class f3
    {
        // ======================================================
        // 配置区域
        // ======================================================
        // 目标文件夹
        string DATA_DIR = "";
        
        // 船长 & 重力加速度
        const double L_REF = 325.5;
        const double G = 9.81;

        // 收敛判定参数
        const int MIN_POINTS_PER_CYCLE = 10;
        const int MIN_CONVERGED_CYCLES = 5;
        const double TOL_MEAN_REL = 0.005; // 0.5%
        const double TOL_AMP_REL = 0.005;

        // 是否保存单文件诊断图
        const bool SAVE_DIAGNOSTIC_PLOTS = false;
        const string DIAG_DIRNAME = "Diagnostics_Plots";

        public f3(string path)
        {
            DATA_DIR = path;
            // 0. 注册编码支持
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // 1. 扫描文件
            if (!Directory.Exists(DATA_DIR))
            {
                Console.WriteLine($"错误：找不到路径 {DATA_DIR}");
                return;
            }

            var patterns = new[] { "船舶波浪增阻时间历程*.TXT", "*增阻*时间历程*.TXT" };
            var files = new List<string>();

            // 递归查找 (.txt 和 .TXT)
            foreach (var file in Directory.GetFiles(DATA_DIR, "*.txt", SearchOption.AllDirectories))
            {
                string fname = Path.GetFileName(file);
                // 简单匹配包含关键字
                if (fname.Contains("增阻") && fname.Contains("时间历程"))
                {
                    files.Add(file);
                }
            }
            
            // 去重
            files = files.Distinct().ToList();

            if (files.Count == 0)
            {
                Console.WriteLine($"未在 {DATA_DIR} 找到符合条件的文件。");
                return;
            }

            Console.WriteLine($"找到 {files.Count} 个文件，开始处理...");
            Console.WriteLine(new string('-', 50));

            var results = new List<AnalysisResult>();

            // 2. 循环处理
            foreach (var fp in files)
            {
                try
                {
                    var res = ProcessOneFile(fp);
                    if (res != null)
                    {
                        results.Add(res);
                        Console.WriteLine($"[OK] {Path.GetFileName(fp)} -> w={res.Omega:F4}, Raw={res.ConvMean:F4}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[跳过] {Path.GetFileName(fp)}: {ex.Message}");
                }
            }

            if (results.Count == 0)
            {
                Console.WriteLine("没有成功解析任何文件。");
                return;
            }

            // 3. 排序 (按 lambda/L 从小到大)
            results = results.OrderBy(r => r.LambdaOverL).ToList();

            // 4. 导出 CSV
            string outCsv = Path.Combine(DATA_DIR, "RAO_增阻汇总_收敛判据.csv");
            ExportCsv(results, outCsv);
            Console.WriteLine($"\n已导出汇总表: {outCsv}");

            // 5. 导出 RAO 曲线图
            string outPng = Path.Combine(DATA_DIR, "RAO_增阻曲线_收敛判据.png");
            MakeRaoPlot(results, outPng);
            Console.WriteLine($"已导出曲线图: {outPng}");

            if (SAVE_DIAGNOSTIC_PLOTS)
            {
                Console.WriteLine($"诊断图已保存在: {Path.Combine(DATA_DIR, DIAG_DIRNAME)}");
            }
        }

        // ======================================================
        // 数据结构
        // ======================================================
        class AnalysisResult
        {
            public string Filename { get; set; }
            public double Omega { get; set; }       // rad/s
            public double Lambda { get; set; }      // m
            public double LambdaOverL { get; set; } // 无量纲
            public double ConvMean { get; set; }    // 收敛均值
            public double ConvAmp { get; set; }     // 收敛幅值
            public int ConvStartCycle { get; set; }
            public int TotalCycles { get; set; }
        }

        class PeriodStats
        {
            public int[] PeriodIdx;
            public double[] MeanPer;
            public double[] AmpPer;
            public double[] CycleStarts;
            public double Level;
            public double[] CrossTimes;
        }

        // ======================================================
        // 核心逻辑
        // ======================================================

        // 1. 处理单个文件
        AnalysisResult ProcessOneFile(string fp)
        {
            // 提取频率
            double omega = ExtractOmegaFromFilename(fp);

            // 读取数据
            var (t, y) = LoadTimeSeries(fp, 1);

            // 周期划分
            var stats = ComputePeriodStatsZeroCross(t, y,  MIN_POINTS_PER_CYCLE,null);

            // 收敛判断
            var (startIdx, convMean, convAmp) = DetectConvergence(
                stats.MeanPer, stats.AmpPer, 
                MIN_CONVERGED_CYCLES, TOL_MEAN_REL, TOL_AMP_REL);

            // 物理计算
            double lam = (omega > 0) ? (2 * Math.PI * G) / (omega * omega) : 0;
            double lamOverL = lam / L_REF;

            // (可选) 绘制诊断图
            if (SAVE_DIAGNOSTIC_PLOTS)
            {
                PlotDiagnostics(fp, t, y, stats, startIdx, convMean, convAmp, omega);
            }

            return new AnalysisResult
            {
                Filename = Path.GetFileName(fp),
                Omega = omega,
                Lambda = lam,
                LambdaOverL = lamOverL,
                ConvMean = convMean,
                ConvAmp = convAmp,
                ConvStartCycle = startIdx + 1,
                TotalCycles = stats.PeriodIdx.Length
            };
        }

        // 2. 读取时间历程
        static (double[] t, double[] y) LoadTimeSeries(string filename, int colIndex)
        {
            // 尝试几种编码
            var encodings = new[] { Encoding.GetEncoding("GBK"), Encoding.UTF8 };
            
            foreach (var enc in encodings)
            {
                var tList = new List<double>();
                var yList = new List<double>();
                bool dataStarted = false;

                try
                {
                    using (var sr = new StreamReader(filename, enc))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            if (line.Contains("*")) continue; // 跳过溢出

                            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length < 2) continue;

                            double tVal;
                            if (!double.TryParse(parts[0], out tVal))
                            {
                                if (dataStarted) continue; // 数据流中出现非数字，可能异常
                                else continue; // 跳过表头
                            }
                            else
                            {
                                dataStarted = true;
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

                    if (tList.Count > 0)
                        return (tList.ToArray(), yList.ToArray());
                }
                catch
                {
                    continue; // 尝试下一个编码
                }
            }

            throw new Exception("无法读取数据 (编码或格式错误)");
        }

        // 3. 从文件名提取频率
        static double ExtractOmegaFromFilename(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            // 正则匹配浮点数 (包括科学计数法)
            var matches = Regex.Matches(name, @"[-+]?\d*\.?\d+(?:[eE][-+]?\d+)?");
            
            if (matches.Count == 0) throw new Exception("文件名中未包含频率数字");

            // 取最后一个匹配到的数字
            string numStr = matches[matches.Count - 1].Value;
            if (double.TryParse(numStr, out double val)) return val;

            throw new Exception($"无法解析频率: {numStr}");
        }

        // 4. 零点上穿划分周期
        static PeriodStats ComputePeriodStatsZeroCross(double[] t, double[] y,int minPoints,double? level = null)
        {
            // 确定参考水平 (后1/3均值)
            if (level == null)
            {
                int start = (int)(y.Length * 2.0 / 3.0);
                double sum = 0; int count = 0;
                for(int i=start; i<y.Length; i++) { sum += y[i]; count++; }
                level = (count > 0) ? sum / count : 0;
            }

            double[] yShift = y.Select(v => v - level.Value).ToArray();
            var crossTimes = new List<double>();

            for (int i = 0; i < t.Length - 1; i++)
            {
                if (yShift[i] <= 0.0 && yShift[i+1] > 0.0)
                {
                    double t1 = t[i], t2 = t[i+1];
                    double y1 = yShift[i], y2 = yShift[i+1];
                    double frac = (y2 != y1) ? -y1 / (y2 - y1) : 0.0;
                    crossTimes.Add(t1 + (t2 - t1) * frac);
                }
            }

            if (crossTimes.Count < 2) throw new Exception("零点上穿点不足，无法划分周期");

            var starts = new List<double>();
            var means = new List<double>();
            var amps = new List<double>();

            for (int k = 0; k < crossTimes.Count - 1; k++)
            {
                double start = crossTimes[k];
                double end = crossTimes[k + 1];

                // 提取周期内数据
                // 优化：不使用LINQ全表扫描，只扫描大致范围（这里为了代码简洁用简单循环）
                double segSum = 0;
                double segMax = -double.MaxValue;
                double segMin = double.MaxValue;
                int segCount = 0;

                // 假设t是有序的，可以优化查找，这里暴力查找
                for(int i=0; i<t.Length; i++)
                {
                    if (t[i] >= start && t[i] < end)
                    {
                        segSum += y[i];
                        if (y[i] > segMax) segMax = y[i];
                        if (y[i] < segMin) segMin = y[i];
                        segCount++;
                    }
                }

                if (segCount < minPoints) continue;

                starts.Add(start);
                means.Add(segSum / segCount);
                amps.Add(0.5 * (segMax - segMin));
            }

            if (starts.Count == 0) throw new Exception("没有合格的周期");

            return new PeriodStats
            {
                PeriodIdx = Enumerable.Range(1, starts.Count).ToArray(),
                MeanPer = means.ToArray(),
                AmpPer = amps.ToArray(),
                CycleStarts = starts.ToArray(),
                Level = level.Value,
                CrossTimes = crossTimes.ToArray()
            };
        }

        // 5. 自动判断收敛 (RMS 残差判据)
        static (int startIdx, double convMean, double convAmp) DetectConvergence(
            double[] meanPer, double[] ampPer, int minConverged, double tolMean, double tolAmp)
        {
            int N = meanPer.Length;
            if (N < minConverged + 1)
                return (0, meanPer.Average(), ampPer.Average());

            int bestStart = Math.Max(0, N - minConverged);

            // 遍历寻找收敛起始点
            for (int start = 0; start <= N - minConverged; start++)
            {
                // 取片段
                int count = N - start;
                if (count < 2) break; // 无法计算差分

                // 提取片段
                var mSeg = new double[count]; Array.Copy(meanPer, start, mSeg, 0, count);
                var aSeg = new double[count]; Array.Copy(ampPer, start, aSeg, 0, count);

                double rmsLevelM = CalcRms(mSeg);
                if (rmsLevelM == 0) continue;
                double rmsChangeM = CalcRmsDiff(mSeg);
                double rMean = rmsChangeM / rmsLevelM;

                double rmsLevelA = CalcRms(aSeg);
                if (rmsLevelA == 0) continue;
                double rmsChangeA = CalcRmsDiff(aSeg);
                double rAmp = rmsChangeA / rmsLevelA;

                if (rMean <= tolMean && rAmp <= tolAmp)
                {
                    bestStart = start;
                    break;
                }
            }

            // 计算最终结果
            double sumM = 0, sumA = 0;
            int finalCount = N - bestStart;
            for(int i=bestStart; i<N; i++)
            {
                sumM += meanPer[i];
                sumA += ampPer[i];
            }

            return (bestStart, sumM/finalCount, sumA/finalCount);
        }

        // 辅助：RMS 计算
        static double CalcRms(double[] arr)
        {
            double sumSq = 0;
            foreach (var v in arr) sumSq += v * v;
            return Math.Sqrt(sumSq / arr.Length);
        }

        static double CalcRmsDiff(double[] arr)
        {
            double sumSq = 0;
            for (int i = 0; i < arr.Length - 1; i++)
            {
                double d = arr[i + 1] - arr[i];
                sumSq += d * d;
            }
            return Math.Sqrt(sumSq / (arr.Length - 1));
        }

        // ======================================================
        // 输出与绘图
        // ======================================================

        static void ExportCsv(List<AnalysisResult> results, string path)
        {
            using (var sw = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                sw.WriteLine("file,omega(rad/s),lambda(m),lambda/L,Raw_mean(conv),Raw_amp(conv),conv_start_cycle,n_cycles");
                foreach (var r in results)
                {
                    sw.WriteLine($"{r.Filename},{r.Omega},{r.Lambda},{r.LambdaOverL},{r.ConvMean},{r.ConvAmp},{r.ConvStartCycle},{r.TotalCycles}");
                }
            }
        }

        static void MakeRaoPlot(List<AnalysisResult> results, string path)
        {
            ScottPlot.Plot plot = new();

            // 准备数据
            double[] x = results.Select(r => r.LambdaOverL).ToArray();
            double[] y = results.Select(r => r.ConvMean).ToArray();

            // 绘图
            var sp = plot.Add.Scatter(x, y);
            sp.MarkerShape = MarkerShape.Cross;
            sp.MarkerSize = 10;
            sp.LineWidth = 2;
            sp.Color = ScottPlot.Color.FromHex("#1f77b4"); // tab:blue
            sp.LegendText = "Raw (收敛均值)";

            // 设置标题和标签内容
            plot.Title("波浪增阻 RAO 结果（收敛段统计）");
            plot.XLabel("波长船长比 (λ/L)");
            plot.YLabel("R_raw (收敛均值/偏移量)");
            
            // 设置网格样式
            plot.Grid.MajorLineColor = Colors.Black.WithAlpha(0.2);
            plot.Grid.LinePattern = LinePattern.Dotted;

            // ★★★ 关键修复：调用字体设置函数 ★★★
            SetChineseFont(plot);

            // 保存图片
            plot.SavePng(path, 1200, 800);
        }

        void PlotDiagnostics(string fileFull, double[] t, double[] y, PeriodStats stats, 
            int startIdx, double convMean, double convAmp, double omega)
        {
            string diagDir = Path.Combine(DATA_DIR, DIAG_DIRNAME);
            if (!Directory.Exists(diagDir)) Directory.CreateDirectory(diagDir);

            ScottPlot.Plot plot = new();
            // 这里为了简化，只画一张包含收敛均值的图
            // 实际要像 Python 那样画 3x1 subplots 比较麻烦，C# 推荐画在一张大图或分开存
            
            // 下面演示：画每周期均值收敛过程
            double[] cycles = stats.PeriodIdx.Select(i => (double)i).ToArray();
            var sp = plot.Add.Scatter(cycles, stats.MeanPer);
            sp.MarkerShape = MarkerShape.FilledCircle;
            sp.Color = Colors.Blue;

            var hl = plot.Add.HorizontalLine(convMean);
            hl.LinePattern = LinePattern.Dashed;
            hl.Color = Colors.Red;
            hl.LegendText = $"收敛均值={convMean:E3}";

            // 绘制绿色收敛区域
            int total = stats.PeriodIdx.Length;
            double x1 = startIdx + 1;
            double x2 = total;
            var span = plot.Add.HorizontalSpan(x1, x2);
            span.FillStyle.Color = Colors.Green.WithAlpha(0.2);

            plot.Title($"诊断图: {Path.GetFileName(fileFull)} (w={omega})");
            plot.XLabel("周期编号");
            plot.YLabel("每周期平均阻值");
            
            // 字体
            try
            {
                plot.Font.Set("Songti SC");
                plot.Font.Automatic();
            } catch { }

            string savePath = Path.Combine(diagDir, $"convergence_{omega:F4}.png");
            plot.SavePng(savePath, 800, 400);
        }
        // ======================================================
        // 辅助：强制设置中文字体 (解决方块字问题)
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