using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tiss_MindRadar.Models;

namespace Tiss_MindRadar.Controllers
{
    public class TeamRawDataController : Controller
    {
        private TISS_MindRadarEntities _db = new TISS_MindRadarEntities(); //資料庫

        #region 心理狀態檢測雷達圖_各隊選手分數
        [HttpGet]
        public ActionResult GetMentalStateTeamRawData()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "UserAccount");
            }

            var rawData = _db.PsychologicalResponse
    .Join(_db.Users, pr => pr.UserID, u => u.UserID, (pr, u) => new { pr, u })
    .Join(_db.Team, temp => temp.u.TeamName, t => t.TeamName, (temp, t) => new TeamRawDataViewModel
    {
        TeamName = t.TeamName,
        UserName = temp.u.UserName,
        Category = temp.pr.CategoryID.ToString(), // 確保 Category 是 string
        Score = temp.pr.Score,
        SurveyDate = temp.pr.SurveyDate
    })
    .OrderBy(r => r.TeamName)
    .ThenBy(r => r.UserName)
    .ThenBy(r => r.SurveyDate)
    .ToList();

            return View(rawData);
        }
        #endregion
    }
}