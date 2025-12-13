using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MiniExcelLibs; // 需要 NuGet 安装: MiniExcel

namespace ShuZhiShuiChi
{
    class f5
    {
        // --- 常量定义 ---
        const double G = 9.80665;
        const double KNOT_TO_MS = 0.5144444444444445;

        public f5(string path,string Mode,double hs,double tin,string ptype,double tp,string umode,double u,double Beta)
        {
            
            // 1. 文件读取设置
            string fileName = path;
            if (string.IsNullOrEmpty(fileName))
            {
                MessageBox.Show("你的xlsx文件路径呢？");
            };

            if (!File.Exists(fileName))
            {
                Console.WriteLine($"[错误] 找不到文件：{fileName}");
                MessageBox.Show($"[错误] 找不到文件：{fileName}");
                return;
            }

            // 2. 读取 Excel 数据
            Console.WriteLine("正在读取 Excel...");
            Console.WriteLine($"正在读取 {fileName}");
            var rows = MiniExcel.Query(fileName, useHeaderRow: false)
                .Cast<IDictionary<string, object>>()
                .ToList();
            if (rows.Count < 2)
            {
                Console.WriteLine("错误：Excel 行数太少（至少需要1行标题+1行数据）。");
                Console.ReadKey();
                return;
            }

            // 3. 手动分析第一行（索引 0 的行）来确定列的位置
            var headerRow = rows[0]; 
            
            // 用来存“列号”，比如 "A", "B", "C"
            string omegaCol = null;
            string rCol = null;
            string hCol = null;
            string pCol = null;

            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("正在扫描第一行内容...");
            
            foreach (var cell in headerRow)
            {
                string key = cell.Key; // 例如 "A", "B"
                string val = cell.Value?.ToString()?.Trim(); // 关键！去空格

                if (string.IsNullOrEmpty(val)) continue;

                Console.WriteLine($"列 [{key}] 的内容是: \"{val}\""); // 打印出来让你看清楚

                // 模糊匹配 (不区分大小写)
                if (val.IndexOf("omega", StringComparison.OrdinalIgnoreCase) >= 0)
                    omegaCol = key;
                else if (val.IndexOf("增阻", StringComparison.OrdinalIgnoreCase) >= 0) // 只要包含“增阻”就行
                    rCol = key;
                else if (val.IndexOf("垂荡", StringComparison.OrdinalIgnoreCase) >= 0)
                    hCol = key;
                else if (val.IndexOf("纵摇", StringComparison.OrdinalIgnoreCase) >= 0)
                    pCol = key;
            }

            if (omegaCol == null) ErrorExit($"找不到 omega 列（列名需包含 omega)");
            if (rCol == null) ErrorExit("缺少必要列：增阻均值");
            if (hCol == null) ErrorExit("缺少必要列：垂荡");
            if (pCol == null) ErrorExit("缺少必要列：纵摇");

            // 3. 用户输入交互
            Console.WriteLine("\n--- 频率列含义（非常关键）---");
            // string mode = AskChoice("表格 omega 列是啥？输入 [auto/abs/enc]（默认 auto）：", new[] { "auto", "abs", "enc" }, "auto");
            string mode = Mode;
            // if (Mode == 0) mode = "auto";
            // if (Mode == 1) mode = "abs";
            // if (Mode == 2) mode = "enc";
            // double Hs = AskFloat("输入有义波高 Hs (m)，例如 5.5：", true);
            // double Tin = AskFloat("输入平均周期 T (s)，例如 9：", true);
            // string pType = AskChoice("你的 T 是 Tp 还是 Tz？[Tp/Tz，默认 Tp]：", new[] { "Tp", "Tz" }, "Tp");
            //
            // double Tp = (pType == "Tp") ? Tin : Tin * AskFloat("换算系数 Tp/Tz（回车默认 1.41）：", false, 1.41);
            double Hs = hs;
            double Tin = tin;
            string pType =ptype;
            // if(ptype == 0)  pType = "Tp";
            // if(ptype == 1) pType = "Tz";
            double Tp = tp;
            // string uMode = AskChoice("船速输入单位：m/s 还是 kn？[ms/kn，默认 kn]：", new[] { "ms", "kn" }, "kn");
            string uMode = umode;
            double U_kn = 0, U = 0;
            if (uMode == "kn")
            {
                // U_kn = AskFloat("输入船速 U (kn)，例如 15.5：", true);
                U_kn = u;
                U = U_kn * KNOT_TO_MS;
                Console.WriteLine($"  -> 换算：U = {U:F6} m/s");
            }
            else
            {
                // U = AskFloat("输入船速 U (m/s)，例如 7.97：", true);
                U = u;
            }

            // double beta = AskFloat("输入波向角 beta (deg)，迎浪可填 180：", false, 180.0);
            double beta = Beta;
            // 4. 数据预处理
            var rawData = new List<DataRow>();
            foreach (var r in rows)
            {
                try
                {
                    double w = Convert.ToDouble(r[omegaCol]);
                    double r1 = Convert.ToDouble(r[rCol]);
                    double hh = Convert.ToDouble(r[hCol]);
                    double hp = Convert.ToDouble(r[pCol]);
                    rawData.Add(new DataRow { Omega = w, R1 = r1, Heave = hh, Pitch = hp });
                }
                catch { /* 忽略转换失败的行 */ }
            }

            // 排序 (argsort)
            rawData = rawData.OrderBy(x => x.Omega).ToList();
            if (rawData.Count < 2) ErrorExit("有效数据行太少，无法计算。");

            // 计算等效 Delta (梯形积分权重)
            double[] omegas = rawData.Select(x => x.Omega).ToArray();
            double[] deltas = CalculateNodeDeltas(omegas);

            // 理论能量
            double m0_theory = Math.Pow(Hs, 2) / 16.0;

            // 5. 核心计算逻辑
            // 场景 A: 假设输入是绝对频率 (Abs)
            var resAbs = ComputeScenario("abs", rawData, deltas, Hs, Tp, U, beta);
            // 场景 B: 假设输入是遭遇频率 (Enc)
            var resEnc = ComputeScenario("enc", rawData, deltas, Hs, Tp, U, beta);

            // 6. 模式选择
            ScenarioResult chosen;
            string modeUsed;

            if (mode == "auto")
            {
                double errAbs = Math.Abs(resAbs.M0_Eta - m0_theory) / (m0_theory == 0 ? 1 : m0_theory);
                double errEnc = Math.Abs(resEnc.M0_Eta - m0_theory) / (m0_theory == 0 ? 1 : m0_theory);

                if (errAbs <= errEnc)
                {
                    chosen = resAbs;
                    modeUsed = "abs";
                }
                else
                {
                    chosen = resEnc;
                    modeUsed = "enc";
                }

                Console.WriteLine("\n--- AUTO 判断结果 ---");
                Console.WriteLine($"理论 m0 = Hs^2/16 = {m0_theory:G6}");
                Console.WriteLine($"当作 ω 计算：m0 = {resAbs.M0_Eta:G6}（相对误差 {errAbs * 100:F2}%）");
                Console.WriteLine($"当作 ωe计算：m0 = {resEnc.M0_Eta:G6}（相对误差 {errEnc * 100:F2}%）");
                Console.WriteLine($"✅ 自动选择：{modeUsed.ToUpper()}（把表格 omega 当作 {(modeUsed == "abs" ? "绝对频率 ω" : "遭遇频率 ωe")}）");
            }
            else if (mode == "abs")
            {
                chosen = resAbs;
                modeUsed = "abs";
            }
            else
            {
                chosen = resEnc;
                modeUsed = "enc";
            }

            // 7. 汇总统计
            double m0_heave = chosen.Details.Sum(x => x.Dm0_Heave);
            double m0_pitch = chosen.Details.Sum(x => x.Dm0_Pitch);
            double R_mean = chosen.Details.Sum(x => x.DR);

            double heave_sig = 4.0 * Math.Sqrt(Math.Max(m0_heave, 0));
            double pitch_sig = 4.0 * Math.Sqrt(Math.Max(m0_pitch, 0));

            // 8. 输出 CSV
            string outCsv = $"__权重明细_{modeUsed.ToUpper()}_按表格频率.csv";
            SaveCsv(outCsv, chosen.Details, R_mean, m0_heave, m0_pitch, chosen.FreqName);

            // 9. 控制台最终报告
            Console.WriteLine("\n" + new string('=', 78));
            Console.WriteLine($"计算完成 ✅（模式：{modeUsed.ToUpper()}）");
            Console.WriteLine(new string('=', 78));
            Console.WriteLine($"Hs = {Hs:G6} m | Tp = {Tp:G6} s | U = {U:G6} m/s | beta = {beta:G6} deg");
            Console.WriteLine($"垂荡有义值(≈4*RMS) = {heave_sig:F10}   [单位同表格“垂荡”]");
            Console.WriteLine($"纵摇有义值(≈4*RMS) = {pitch_sig:F10}   [单位同表格“纵摇”]");
            Console.WriteLine($"不规则波平均增阻     = {R_mean:F10}     [单位同表格“增阻均值”]");
            Console.WriteLine(new string('-', 78));
            Console.WriteLine($"谱能量检查：m0 = {chosen.M0_Eta:G6}；理论 Hs^2/16 = {m0_theory:G6}；覆盖率 = {(chosen.M0_Eta / m0_theory * 100):F2}%");
            Console.WriteLine($"明细已保存：{Path.GetFullPath(outCsv)}");
            MessageBox.Show($"明细已保存：{Path.GetFullPath(outCsv)}");
            Console.WriteLine(new string('=', 78));
            MessageBox.Show($"垂荡有义值(≈4*RMS) = {heave_sig:F10}   [单位同表格“垂荡”]");
            MessageBox.Show($"纵摇有义值(≈4*RMS) = {pitch_sig:F10}   [单位同表格“纵摇”]");
            MessageBox.Show($"不规则波平均增阻     = {R_mean:F10}     [单位同表格“增阻均值”]");
            // 打印 Top 20
            Console.WriteLine($"\n【增阻贡献 Top20（频率=你的表格频点）】");
            var topList = chosen.Details.OrderByDescending(x => x.DR).Take(20).ToList();
            
            // 打印表头
            Console.WriteLine($"{"Freq",-10} {"Delta",-10} {"S_used",-12} {"a2_node",-12} {"R1",-12} {"dR",-12} {"占比%",-8}");
            foreach (var item in topList)
            {
                Console.WriteLine($"{item.FreqUsed,-10:F4} {item.Delta,-10:F4} {item.S_Used,-12:F4} {item.A2,-12:F4} {item.R1,-12:F4} {item.DR,-12:F4} {item.RatioDR * 100,-8:F2}");
            }
        }

        // --- 核心算法函数 ---

        // ITTC 双参数谱
        static double ITTC_Spectrum(double Hs, double Tp, double omega)
        {
            if (omega <= 0) return 0;
            double wp = 2.0 * Math.PI / Tp;
            double S = (5.0 / 16.0) * Math.Pow(Hs, 2) * Math.Pow(wp, 4) * Math.Pow(omega, -5) * Math.Exp(-(5.0 / 4.0) * Math.Pow(wp / omega, 4));
            return double.IsInfinity(S) || double.IsNaN(S) ? 0 : S;
        }

        // 遭遇频率变换 ω -> ωe
        static double GetOmegaE(double omega, double U, double beta)
        {
            double cb = Math.Cos(beta * Math.PI / 180.0);
            double a = (U * cb) / G;
            return omega - a * Math.Pow(omega, 2);
        }

        // 从 ωe 反解 ω
        static double SolveOmega(double we, double U, double beta)
        {
            double cb = Math.Cos(beta * Math.PI / 180.0);
            double a = (U * cb) / G;

            if (Math.Abs(a) < 1e-12) return we;

            double disc = 1.0 - 4.0 * a * we;
            if (disc <= 0) return double.NaN;

            double sqrtDisc = Math.Sqrt(disc);
            double w1 = (1.0 + sqrtDisc) / (2.0 * a);
            double w2 = (1.0 - sqrtDisc) / (2.0 * a);

            var cands = new List<double>();
            if (w1 > 0) cands.Add(w1);
            if (w2 > 0) cands.Add(w2);

            if (cands.Count == 0) return double.NaN;
            // 选更接近 ωe 的正根
            return cands.OrderBy(w => Math.Abs(w - we)).First();
        }

        // 节点等效宽度（等价梯形积分）
        static double[] CalculateNodeDeltas(double[] x)
        {
            int n = x.Length;
            double[] delta = new double[n];
            if (n < 2) return delta;

            double[] dx = new double[n - 1];
            for (int i = 0; i < n - 1; i++) dx[i] = x[i + 1] - x[i];

            delta[0] = dx[0] / 2.0;
            delta[n - 1] = dx[n - 2] / 2.0;
            for (int i = 1; i < n - 1; i++)
            {
                delta[i] = (dx[i - 1] + dx[i]) / 2.0;
            }
            return delta;
        }

        // --- 业务逻辑封装 ---

        static ScenarioResult ComputeScenario(string tag, List<DataRow> rawData, double[] deltas, double Hs, double Tp, double U, double beta)
        {
            var details = new List<DetailRow>();
            double m0_sum = 0;
            string fName = (tag == "abs") ? "omega(rad/s)" : "omega_e(rad/s)";

            for (int i = 0; i < rawData.Count; i++)
            {
                var row = rawData[i];
                double delta = deltas[i];
                double omega_used = row.Omega;
                double S_used = 0;
                
                // 附加信息
                double w_abs = 0;
                double w_enc = 0;
                double jac = 0;

                if (tag == "abs")
                {
                    // 假设输入是 ω
                    w_abs = omega_used;
                    S_used = ITTC_Spectrum(Hs, Tp, w_abs);
                    
                    // 计算对应的 ωe (仅用于显示)
                    w_enc = GetOmegaE(w_abs, U, beta);
                    jac = double.NaN; // abs 模式下不需要 Jacobian 修正 S
                }
                else
                {
                    // 假设输入是 ωe
                    w_enc = omega_used;
                    w_abs = SolveOmega(w_enc, U, beta);
                    
                    // 计算 Jacobian
                    if (!double.IsNaN(w_abs))
                    {
                        double cb = Math.Cos(beta * Math.PI / 180.0);
                        double a = (U * cb) / G;
                        double denom = 1.0 - 2.0 * a * w_abs;
                        if (denom != 0) jac = Math.Abs(1.0 / denom);
                        
                        double S_abs = ITTC_Spectrum(Hs, Tp, w_abs);
                        S_used = S_abs * jac;
                    }
                    else
                    {
                        S_used = 0;
                        jac = double.NaN;
                    }
                }

                if (double.IsNaN(S_used) || double.IsInfinity(S_used)) S_used = 0;

                double a2 = 2.0 * S_used * delta;
                double dR = row.R1 * a2;
                double dm0_h = Math.Pow(row.Heave, 2) * S_used * delta;
                double dm0_p = Math.Pow(row.Pitch, 2) * S_used * delta;
                
                // 积分 m0 (梯形积分思想：sum(S*Delta))
                m0_sum += S_used * delta;

                details.Add(new DetailRow
                {
                    FreqUsed = omega_used,
                    Delta = delta,
                    S_Used = S_used,
                    A2 = a2,
                    R1 = row.R1,
                    DR = dR,
                    Heave = row.Heave,
                    Dm0_Heave = dm0_h,
                    Pitch = row.Pitch,
                    Dm0_Pitch = dm0_p,
                    
                    // 附加列
                    Omega_Abs = w_abs,
                    Omega_Enc = w_enc,
                    Jacobian = jac
                });
            }

            return new ScenarioResult { Tag = tag, FreqName = fName, M0_Eta = m0_sum, Details = details };
        }

        // --- 辅助工具 ---

        static void SaveCsv(string path, List<DetailRow> details, double totalR, double totalH, double totalP, string freqName)
        {
            var sb = new StringBuilder();
            // Header
            sb.AppendLine($"{freqName},Delta_omega,S_eta_used,a2_node,R1,dR,dR_Ratio,H_heave,dm0_heave,dm0_heave_Ratio,H_pitch,dm0_pitch,dm0_pitch_Ratio,Omega_Abs_Ref,Omega_Enc_Ref,Jacobian");

            foreach (var d in details)
            {
                double r_ratio = (totalR != 0) ? d.DR / totalR : 0;
                double h_ratio = (totalH != 0) ? d.Dm0_Heave / totalH : 0;
                double p_ratio = (totalP != 0) ? d.Dm0_Pitch / totalP : 0;

                sb.AppendLine($"{d.FreqUsed},{d.Delta},{d.S_Used},{d.A2},{d.R1},{d.DR},{r_ratio},{d.Heave},{d.Dm0_Heave},{h_ratio},{d.Pitch},{d.Dm0_Pitch},{p_ratio},{d.Omega_Abs},{d.Omega_Enc},{d.Jacobian}");
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        static double AskFloat(string prompt, bool positive, double? def = null)
        {
            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input) && def.HasValue) return def.Value;
                if (double.TryParse(input, out double val))
                {
                    if (positive && val <= 0) { Console.WriteLine("  必须是正数。"); continue; }
                    return val;
                }
                Console.WriteLine("  格式错误。");
            }
        }

        static string AskChoice(string prompt, string[] choices, string def)
        {
            var lowerChoices = choices.Select(c => c.ToLower()).ToList();
            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine()?.Trim().ToLower();
                if (string.IsNullOrEmpty(input)) return def;
                int idx = lowerChoices.IndexOf(input);
                if (idx >= 0) return choices[idx];
                Console.WriteLine($"  请选择: {string.Join("/", choices)}");
            }
        }

        static void ErrorExit(string msg)
        {
            Console.WriteLine($"[错误] {msg}");
            Console.WriteLine("按任意键退出...");
            Environment.Exit(1);
        }
    }

    // --- 数据结构 ---

    class DataRow
    {
        public double Omega { get; set; }
        public double R1 { get; set; }
        public double Heave { get; set; }
        public double Pitch { get; set; }
    }

    class DetailRow
    {
        public double FreqUsed { get; set; }
        public double Delta { get; set; }
        public double S_Used { get; set; }
        public double A2 { get; set; }
        public double R1 { get; set; }
        public double DR { get; set; }
        public double Heave { get; set; }
        public double Dm0_Heave { get; set; }
        public double Pitch { get; set; }
        public double Dm0_Pitch { get; set; }
        public double RatioDR { get; set; } // 稍后计算

        // Extra info
        public double Omega_Abs { get; set; }
        public double Omega_Enc { get; set; }
        public double Jacobian { get; set; }
    }

    class ScenarioResult
    {
        public string Tag { get; set; }
        public string FreqName { get; set; }
        public double M0_Eta { get; set; }
        public List<DetailRow> Details { get; set; }
    }
}