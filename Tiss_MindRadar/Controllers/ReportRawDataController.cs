using System;
using System.Collections.Generic;
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
                    .Where(result => result.temp.u.TeamID == teamId)
                    .Select(result => new TeamReportViewModel
                    {
                        UserName = result.temp.u.UserName,
                        Category = result.ms.QuestionText,
                        Score = result.temp.pr.Score,
                        SurveyDate = result.temp.pr.SurveyDate
                    })
                    .OrderBy(r => r.UserName)
                    .ThenBy(r => r.SurveyDate)
                    .ToList();
            }

            return View(reportData);
        }
        #endregion
    }
}