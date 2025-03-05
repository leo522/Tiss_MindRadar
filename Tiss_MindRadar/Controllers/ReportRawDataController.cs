using OfficeOpenXml.Style;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tiss_MindRadar.Models;

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
                        Gender = result.up.Gender, // 取得性別欄位
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
            if (!int.TryParse(Request.Form["teamId"], out int teamId))
            {
                return new HttpStatusCodeResult(400, "缺少必要參數");
            }

            var team = _db.Team.FirstOrDefault(t => t.TeamID == teamId);
            if (team == null) return HttpNotFound("隊伍不存在");

            var reportData = _db.PsychologicalResponse
                .Join(_db.Users, pr => pr.UserID, u => u.UserID, (pr, u) => new { pr, u })
                .Where(x => x.u.TeamID == teamId)
                .Select(x => new
                {
                    x.u.UserName,
                    Gender = x.u.UserProfile.FirstOrDefault().Gender, // 取第一筆 UserProfile
                    x.pr.SurveyDate,
                    x.pr.Score
                }).ToList();

            if (!reportData.Any()) return Content("沒有數據可下載");

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add($"{team.TeamName} 報表");

                worksheet.Cells[1, 1].Value = "姓名";
                worksheet.Cells[1, 2].Value = "性別";
                worksheet.Cells[1, 3].Value = "填答日期";
                worksheet.Cells[1, 4].Value = "分數";

                int row = 2;
                foreach (var data in reportData)
                {
                    worksheet.Cells[row, 1].Value = data.UserName;
                    worksheet.Cells[row, 2].Value = data.Gender;
                    worksheet.Cells[row, 3].Value = data.SurveyDate?.ToString("yyyy/MM/dd");
                    worksheet.Cells[row, 4].Value = data.Score;
                    row++;
                }

                var stream = new MemoryStream(package.GetAsByteArray());
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{team.TeamName}_報表.xlsx");
            }
        }
        #endregion
    }
}