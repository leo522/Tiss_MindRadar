using OfficeOpenXml.Style;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tiss_MindRadar.Models;
using System.Runtime.Remoting.Messaging;
using System.Runtime.InteropServices.ComTypes;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Numerics;

namespace Tiss_MindRadar.Controllers
{
    public class ReportRawDataController : Controller
    {
        private TISS_MindRadarEntities _db = new TISS_MindRadarEntities(); //資料庫

        #region 產生各隊伍選手明細
        public ActionResult TeamReportData(int? teamId)
        {
            // 取得所有啟用的隊伍
            var teams = _db.Team.Where(t => t.isenable == true).ToList();
            ViewBag.Teams = new SelectList(teams, "TeamID", "TeamName", teamId);

            List<TeamReportViewModel> reportData = new List<TeamReportViewModel>();

            if (teamId.HasValue)
            {
                reportData = _db.PsychologicalResponse
                    .Join(_db.Users, pr => pr.UserID, u => u.UserID, (pr, u) => new { pr, u })
                    .Join(_db.MentalState, temp => temp.pr.CategoryID, ms => ms.QuestionNumber, (temp, ms) => new { temp, ms })
                    .Join(_db.UserProfile, temp => temp.temp.u.UserID, up => up.UserID, (temp, up) => new { temp, up })
                    .Where(result => result.temp.temp.u.TeamID == teamId)
                    .Select(result => new TeamReportViewModel
                    {
                        UserName = result.temp.temp.u.UserName,
                        Gender = result.up.Gender,
                        Category = result.temp.ms.QuestionText,
                        Score = result.temp.temp.pr.Score,
                        SurveyDate = result.temp.temp.pr.SurveyDate
                    })
                    .OrderBy(r => r.UserName)
                    .ThenBy(r => r.SurveyDate)
                    .ToList();
            }

            return View(reportData);
        }
        #endregion

        #region 下載Excel報表
        [HttpPost]
        public ActionResult ExportTeamReportToExcel()
        {
            try
            {
                if (!int.TryParse(Request.Form["teamId"], out int teamId))
                {
                    return new HttpStatusCodeResult(400, "缺少必要參數");
                }

                var team = _db.Team.FirstOrDefault(t => t.TeamID == teamId);
                if (team == null) return HttpNotFound("隊伍不存在");

                string safeTeamName = string.Concat(team.TeamName.Split(Path.GetInvalidFileNameChars()));
                string fileName = $"{safeTeamName}_報表.xlsx";

                // **取得所有心理測驗題目，包含 QuestionNumber & CategoryID**
                var questionData = _db.MentalState
                    .OrderBy(q => q.QuestionNumber)
                    .Select(q => new { q.QuestionNumber, q.QuestionText, q.CategoryID })
                    .ToList();

                // **取得小類別**
                var smallCategories = _db.PsychologicalStateCategory.ToDictionary(c => c.ID, c => c.CategoryName);

                var reportData = _db.PsychologicalResponse
                    .Join(_db.Users, pr => pr.UserID, u => u.UserID, (pr, u) => new { pr, u })
                    .Join(_db.MentalState, temp => temp.pr.CategoryID, ms => ms.CategoryID, (temp, ms) => new { temp, ms })
                    .Join(_db.UserProfile, temp => temp.temp.u.UserID, up => up.UserID, (temp, up) => new { temp, up })
                    .Where(result => result.temp.temp.u.TeamID == teamId)
                    .Select(result => new
                    {
                        UserName = result.temp.temp.u.UserName,
                        Gender = result.up.Gender ?? "未指定",
                        CategoryID = result.temp.ms.CategoryID, // 使用 CategoryID 來計算平均
                        QuestionText = result.temp.ms.QuestionText,
                        Score = result.temp.temp.pr.Score,
                        SurveyDate = result.temp.temp.pr.SurveyDate
                    })
                    .OrderBy(r => r.UserName)
                    .ThenBy(r => r.SurveyDate)
                    .ToList();

                if (!reportData.Any()) return Content("沒有數據可下載");

                // **進行分組**
                var groupedData = reportData
                    .GroupBy(r => new { r.UserName, r.Gender, r.SurveyDate })
                    .Select(g => new
                    {
                        g.Key.UserName,
                        g.Key.Gender,
                        SurveyDate = g.Key.SurveyDate?.ToString("yyyy/MM/dd"),

                        // **建立字典：Key=QuestionText, Value=Score**
                        QuestionScores = g.ToDictionary(q => q.QuestionText, q => q.Score),

                        // **小類別平均數：Key=CategoryID, Value=該類別的平均分數**
                        CategoryAverages = g.GroupBy(x => x.CategoryID)
                                            .ToDictionary(c => c.Key, c => c.Average(x => x.Score))
                    })
                    .ToList();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add($"{team.TeamName} 報表");

                    // **建立 Excel 標題**
                    List<string> headers = new List<string> { "姓名", "性別", "填答日期" };
                    headers.AddRange(questionData.Select(q => q.QuestionText)); // **所有題目名稱**
                    headers.AddRange(smallCategories.Values); // **所有小類別名稱**

                    for (int i = 0; i < headers.Count; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = headers[i];
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        worksheet.Cells[1, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    int row = 2;
                    foreach (var record in groupedData)
                    {
                        worksheet.Cells[row, 1].Value = record.UserName;
                        worksheet.Cells[row, 2].Value = record.Gender;
                        worksheet.Cells[row, 3].Value = record.SurveyDate;

                        int colIndex = 4;

                        // **填入每個心理測驗題目的分數**
                        foreach (var question in questionData)
                        {
                            worksheet.Cells[row, colIndex].Value = record.QuestionScores.ContainsKey(question.QuestionText)
                                ? Math.Round(Convert.ToDouble(record.QuestionScores[question.QuestionText]), 1)  // 強制轉換成 double
                                : (double?)null;
                            colIndex++;
                        }


                        // **填入小類別平均數**
                        foreach (var category in smallCategories)
                        {
                            worksheet.Cells[row, colIndex].Value = record.CategoryAverages.ContainsKey(category.Key)
                                ? Math.Round(record.CategoryAverages[category.Key], 1)
                                : (double?)null;
                            colIndex++;
                        }

                        row++;
                    }

                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    var stream = new MemoryStream(package.GetAsByteArray());
                    stream.Position = 0;

                    Response.Headers.Remove("Content-Disposition");
                    Response.Headers["Content-Disposition"] = $"attachment; filename=\"{HttpUtility.UrlEncode(fileName)}\"";
                    Response.Headers["Content-Type"] = "application/octet-stream";

                    return File(stream, "application/octet-stream", fileName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Error] 下載 Excel 失敗：" + ex.Message);
                return new HttpStatusCodeResult(500, "伺服器內部錯誤，請稍後再試");
            }
        }

        //[HttpPost]
        //public ActionResult ExportTeamReportToExcel()
        //{
        //    try
        //    {
        //        if (!int.TryParse(Request.Form["teamId"], out int teamId))
        //        {
        //            return new HttpStatusCodeResult(400, "缺少必要參數");
        //        }

        //        var team = _db.Team.FirstOrDefault(t => t.TeamID == teamId);
        //        if (team == null) return HttpNotFound("隊伍不存在");

        //        // 避免特殊字元影響檔名，移除可能不合法的字元
        //        string safeTeamName = string.Concat(team.TeamName.Split(Path.GetInvalidFileNameChars()));
        //        string fileName = $"{safeTeamName}_報表.xlsx";
        //        string encodedFileName = Uri.EscapeDataString(fileName);

        //        //取得 MentalState 內所有心理測驗題目
        //        var questionTexts = _db.MentalState.OrderBy(q => q.QuestionNumber).Select(q => q.QuestionText).ToList();

        //        // 取得報表數據，按心理類別進行分組
        //        var reportData = _db.PsychologicalResponse
        //            .Join(_db.Users, pr => pr.UserID, u => u.UserID, (pr, u) => new { pr, u })
        //            .Join(_db.MentalState, temp => temp.pr.CategoryID, ms => ms.QuestionNumber, (temp, ms) => new { temp, ms })
        //            .Join(_db.UserProfile, temp => temp.temp.u.UserID, up => up.UserID, (temp, up) => new { temp, up })
        //            .Where(result => result.temp.temp.u.TeamID == teamId)
        //            .Select(result => new TeamReportViewModel
        //            {
        //                UserName = result.temp.temp.u.UserName,
        //                Gender = result.up.Gender ?? "未指定",
        //                Category = result.temp.ms.QuestionText,
        //                Score = result.temp.temp.pr.Score,
        //                SurveyDate = result.temp.temp.pr.SurveyDate
        //            })
        //            .OrderBy(r => r.UserName)
        //            .ThenBy(r => r.SurveyDate)
        //            .ToList();

        //        if (!reportData.Any()) return Content("沒有數據可下載");

        //        // **進行分組**
        //        var groupedData = reportData
        //            .GroupBy(r => new { r.UserName, r.Gender, r.SurveyDate })
        //            .Select(g => new
        //            {
        //                g.Key.UserName,
        //                g.Key.Gender,
        //                SurveyDate = g.Key.SurveyDate?.ToString("yyyy/MM/dd"),
        //                CategoryScores = g.GroupBy(x => x.Category).ToDictionary(q => q.Key, q => q.Average(x => x.Score))
        //            })
        //            .ToList();
        //        // **從資料庫動態讀取小類別**
        //        var smallCategories = _db.PsychologicalStateCategory
        //            .ToDictionary(c => c.CategoryName, c => c.ID);  // ID 即為對應的題目範圍

        //        ExcelPackage.LicenseContext = LicenseContext.NonCommercial; //**設定 LicenseContext 避免錯誤**
        //        // 計算男女大類別平均數
        //        var categoryScores = new Dictionary<string, (double maleAvg, double femaleAvg)>
        //        {
        //            { "一、基礎心理技能", GetCategoryAverage(new[] { "目標設定", "自信心", "承諾" }, reportData, "男", "女") },
        //            { "二、身體心理技能", GetCategoryAverage(new[] { "壓力反應", "害怕控制", "活化", "放鬆" }, reportData, "男", "女") },
        //            { "三、認知技能", GetCategoryAverage(new[] { "意象", "心智練習", "專注", "再專注", "競賽計畫" }, reportData, "男", "女") }
        //        };

        //        using (var package = new ExcelPackage())
        //        {
        //            var worksheet = package.Workbook.Worksheets.Add($"{team.TeamName} 報表");

        //            // 建立標題列
        //            List<string> headers = new List<string> { "姓名", "性別", "填答日期" };

        //            headers.AddRange(questionTexts);

        //            headers.AddRange(new string[]
        //            {
        //                "一、基礎心理技能", "1.目標設定", "2.自信心", "3.承諾",
        //                "二、身體心理技能", "1.壓力反應", "2.害怕控制", "3.活化/激發", "4.放鬆",
        //                "三、認知技能", "1.意象", "2.心智練習", "3.專注", "4.再專注", "5.競賽計畫"
        //            });

        //            for (int i = 0; i < headers.Count; i++)
        //            {
        //                worksheet.Cells[1, i + 1].Value = headers[i];
        //                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        //                worksheet.Cells[1, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        //            }

        //            // **填入數據**
        //            int row = 2;
        //            foreach (var record in groupedData)
        //            {
        //                worksheet.Cells[row, 1].Value = record.UserName;
        //                worksheet.Cells[row, 2].Value = record.Gender;
        //                worksheet.Cells[row, 3].Value = record.SurveyDate;

        //                for (int i = 0; i < questionTexts.Count; i++) // **填入每個心理技能的分數**
        //                {
        //                    string category = questionTexts[i]; // 心理技能名稱
        //                    worksheet.Cells[row, i + 4].Value = record.CategoryScores.TryGetValue(category, out var score)
        //                        ? Math.Round(score, 1): (double?)null;
        //                }

        //                // 安全計算平均數的方法，確保保留小數第一位
        //                //double SafeAverage(IEnumerable<double> scores) => scores.Any() ? Math.Round(scores.Average(), 1) : 0;

        //                //// 計算小類別平均數
        //                //double avgGoalSetting = SafeAverage(record..Scores.Where(kv => kv.Key == 1 || kv.Key == 2).Select(kv => (double)kv.Value));
        //                //double avgConfidence = SafeAverage(record.Scores.Where(kv => kv.Key == 3 || kv.Key == 4).Select(kv => (double)kv.Value));
        //                //double avgCommitment = SafeAverage(record.Scores.Where(kv => kv.Key == 5 || kv.Key == 6).Select(kv => (double)kv.Value));
        //                //double avgStressResponse = SafeAverage(record.Scores.Where(kv => kv.Key == 7 || kv.Key == 8).Select(kv => (double)kv.Value));
        //                //double avgFearControl = SafeAverage(record.Scores.Where(kv => kv.Key == 9 || kv.Key == 10).Select(kv => (double)kv.Value));
        //                //double avgActivation = SafeAverage(record.Scores.Where(kv => kv.Key == 11 || kv.Key == 12).Select(kv => (double)kv.Value));
        //                //double avgRelaxation = SafeAverage(record.Scores.Where(kv => kv.Key == 13 || kv.Key == 14).Select(kv => (double)kv.Value));
        //                //double avgImagery = SafeAverage(record.Scores.Where(kv => kv.Key == 15 || kv.Key == 16).Select(kv => (double)kv.Value));
        //                //double avgMentalPractice = SafeAverage(record.Scores.Where(kv => kv.Key == 17 || kv.Key == 18).Select(kv => (double)kv.Value));
        //                //double avgFocus = SafeAverage(record.Scores.Where(kv => kv.Key == 19 || kv.Key == 20).Select(kv => (double)kv.Value));
        //                //double avgRefocus = SafeAverage(record.Scores.Where(kv => kv.Key == 21 || kv.Key == 22).Select(kv => (double)kv.Value));
        //                //double avgCompetitionPlan = SafeAverage(record.Scores.Where(kv => kv.Key == 23 || kv.Key == 24).Select(kv => (double)kv.Value));

        //                //// 計算三大類別平均數
        //                //double avgBasicSkills = SafeAverage(new[] { avgGoalSetting, avgConfidence, avgCommitment }); //基礎心理技能
        //                //double avgPhysicalSkills = SafeAverage(new[] { avgStressResponse, avgFearControl, avgActivation, avgRelaxation }); //身體心理技能
        //                //double avgCognitiveSkills = SafeAverage(new[] { avgImagery, avgMentalPractice, avgFocus, avgRefocus, avgCompetitionPlan }); //認知技能

        //                //// 填入大類別與小類別平均數，並四捨五入到小數點第一位
        //                //worksheet.Cells[row, 28].Value = avgBasicSkills != 0.0 ? Math.Round(avgBasicSkills, 1) : (double?)null; //基礎心理技能平均
        //                //worksheet.Cells[row, 29].Value = avgGoalSetting != 0.0 ? Math.Round(avgGoalSetting, 1) : (double?)null;
        //                //worksheet.Cells[row, 30].Value = avgConfidence != 0.0 ? Math.Round(avgConfidence, 1) : (double?)null;
        //                //worksheet.Cells[row, 31].Value = avgCommitment != 0.0 ? Math.Round(avgCommitment, 1) : (double?)null;
        //                //worksheet.Cells[row, 32].Value = avgPhysicalSkills != 0.0 ? Math.Round(avgPhysicalSkills, 1) : (double?)null; //身體心理技能平均
        //                //worksheet.Cells[row, 33].Value = avgStressResponse != 0.0 ? Math.Round(avgStressResponse, 1) : (double?)null;
        //                //worksheet.Cells[row, 34].Value = avgFearControl != 0.0 ? Math.Round(avgFearControl, 1) : (double?)null;
        //                //worksheet.Cells[row, 35].Value = avgActivation != 0.0 ? Math.Round(avgActivation, 1) : (double?)null;
        //                //worksheet.Cells[row, 36].Value = avgRelaxation != 0.0 ? Math.Round(avgRelaxation, 1) : (double?)null;
        //                //worksheet.Cells[row, 37].Value = avgCognitiveSkills != 0.0 ? Math.Round(avgCognitiveSkills, 1) : (double?)null; //認知技能平均
        //                //worksheet.Cells[row, 38].Value = avgImagery != 0.0 ? Math.Round(avgImagery, 1) : (double?)null;
        //                //worksheet.Cells[row, 39].Value = avgMentalPractice != 0.0 ? Math.Round(avgMentalPractice, 1) : (double?)null;
        //                //worksheet.Cells[row, 40].Value = avgFocus != 0.0 ? Math.Round(avgFocus, 1) : (double?)null;
        //                //worksheet.Cells[row, 41].Value = avgRefocus != 0.0 ? Math.Round(avgRefocus, 1) : (double?)null;
        //                //worksheet.Cells[row, 42].Value = avgCompetitionPlan != 0.0 ? Math.Round(avgCompetitionPlan, 1) : (double?)null;

        //                row++;
        //            }


        //            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        //            // 在此處插入雷達圖
        //            var radarChart = worksheet.Drawings.AddChart("RadarChart", eChartType.Radar);

        //            // 設定雷達圖標題
        //            radarChart.Title.Text = $"{team.TeamName} 向度大類別平均分數（男女區分）";

        //            // **大類別名稱**
        //            string[] categories = { "基礎心理技能", "身體心理技能", "認知技能" };

        //            // **對應的男女性別平均數**
        //            double[] maleAverages = {
        //                        categoryScores["一、基礎心理技能"].maleAvg,
        //                        categoryScores["二、身體心理技能"].maleAvg,
        //                        categoryScores["三、認知技能"].maleAvg
        //                };

        //            double[] femaleAverages = {
        //                        categoryScores["一、基礎心理技能"].femaleAvg,
        //                        categoryScores["二、身體心理技能"].femaleAvg,
        //                        categoryScores["三、認知技能"].femaleAvg
        //                };

        //            // **填入數據到 Excel**
        //            int chartStartRow = row + 2;
        //            int chartStartCol = 1;

        //            worksheet.Cells[chartStartRow, chartStartCol].Value = "向度大類別";
        //            worksheet.Cells[chartStartRow, chartStartCol + 1].Value = "男";
        //            worksheet.Cells[chartStartRow, chartStartCol + 2].Value = "女";

        //            for (int i = 0; i < categories.Length; i++)
        //            {
        //                worksheet.Cells[chartStartRow + i + 1, chartStartCol].Value = categories[i];
        //                worksheet.Cells[chartStartRow + i + 1, chartStartCol + 1].Value = maleAverages[i];
        //                worksheet.Cells[chartStartRow + i + 1, chartStartCol + 2].Value = femaleAverages[i];
        //            }

        //            // 設定圖表範圍
        //            var seriesMale = radarChart.Series.Add(
        //                worksheet.Cells[chartStartRow + 1, chartStartCol + 1, chartStartRow + categories.Length, chartStartCol + 1],
        //                worksheet.Cells[chartStartRow + 1, chartStartCol, chartStartRow + categories.Length, chartStartCol]
        //            );
        //            seriesMale.Header = "男性平均分數";

        //            var seriesFemale = radarChart.Series.Add(
        //                worksheet.Cells[chartStartRow + 1, chartStartCol + 2, chartStartRow + categories.Length, chartStartCol + 2],
        //                worksheet.Cells[chartStartRow + 1, chartStartCol, chartStartRow + categories.Length, chartStartCol]
        //            );
        //            seriesFemale.Header = "女性平均分數";

        //            // **調整雷達圖大小與位置**
        //            radarChart.SetPosition(chartStartRow - 2, 0, chartStartCol + 4, 0);
        //            radarChart.SetSize(600, 400);

        //            var stream = new MemoryStream(package.GetAsByteArray());
        //            stream.Position = 0; // 修正檔案串流的位置

        //            Response.Headers.Remove("Content-Disposition"); // 先移除可能的舊值
        //            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{HttpUtility.UrlEncode(fileName)}\"";
        //            Response.Headers["Content-Type"] = "application/octet-stream";

        //            return File(stream, "application/octet-stream", fileName);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine("[Error] 下載 Excel 失敗：" + ex.Message);
        //        return new HttpStatusCodeResult(500, "伺服器內部錯誤，請稍後再試");
        //    }
        //}
        #endregion

        #region 計算每個大類別的平均分數
        private (double maleAvg, double femaleAvg) GetCategoryAverage(string[] categories, List<TeamReportViewModel> reportData, string maleGender, string femaleGender)
        {
            double SafeAverage(IEnumerable<double> scores) => scores.Any() ? Math.Round(scores.Average(), 1) : 0.0;

            var maleScores = reportData.Where(r => r.Gender == maleGender && categories.Contains(r.Category))
                .Select(r => r.Score).ToList();

            var femaleScores = reportData.Where(r => r.Gender == femaleGender && categories.Contains(r.Category))
                .Select(r => r.Score).ToList();

            return (SafeAverage(maleScores), SafeAverage(femaleScores));
        }
        #endregion
    }
}