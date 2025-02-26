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

        #region 取得所有隊伍
        [HttpGet]
        public JsonResult GetTeams()
        {
            var teams = _db.Team
                .Where(t => t.isenable == true)
                .Select(t => new { t.TeamID, t.TeamName })
                .ToList();

            return Json(teams, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region 根據隊伍ID取得選手
        [HttpPost]
        public JsonResult GetUsersByTeam(int teamId)
        {
            var users = _db.Users
                .Where(u => u.TeamID == teamId)
                .Select(u => new { u.UserID, u.UserName })
                .ToList();

            return Json(users);
        }
        #endregion

        #region 心理狀態檢測雷達圖_各隊選手分數
        [HttpPost]
        public ActionResult GetMentalStateTeamRawData(int? userId, int? teamId)
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    return RedirectToAction("Login", "UserAccount");
                }
                if (Session["UserRole"] == null || Session["UserRole"].ToString() != "Consultant")
                {
                    return RedirectToAction("AccessDenied", "RadarError");
                }

                // 取得所有隊伍
                var teams = _db.Team.Where(t => t.isenable == true).ToList();
                ViewBag.Teams = new SelectList(teams, "TeamID", "TeamName", teamId);

                // 取得隊伍的選手
                if (teamId.HasValue)
                {
                    var users = _db.Users
                        .Where(u => u.TeamID == teamId)
                        .Select(u => new { u.UserID, u.UserName })
                        .ToList();
                    ViewBag.Users = new SelectList(users, "UserID", "UserName", userId);
                }
                else
                {
                    ViewBag.Users = new SelectList(new List<object>(), "UserID", "UserName");
                }

                List<TeamRawDataViewModel> rawData = new List<TeamRawDataViewModel>();
                List<RadarChartVIewModel> radarData = new List<RadarChartVIewModel>();

                if (userId.HasValue)
                {
                    rawData = _db.PsychologicalResponse
                        .Join(_db.Users, pr => pr.UserID, u => u.UserID, (pr, u) => new { pr, u })
                        .Join(_db.MentalState, temp => temp.pr.CategoryID, ms => ms.QuestionNumber, (temp, ms) => new { temp, ms })
                        .Where(result => result.temp.u.UserID == userId)
                        .Select(result => new TeamRawDataViewModel
                        {
                            TeamName = result.temp.u.TeamName,
                            UserName = result.temp.u.UserName,
                            Category = result.ms.QuestionText,
                            Score = result.temp.pr.Score,
                            SurveyDate = result.temp.pr.SurveyDate
                        })
                        .OrderBy(r => r.SurveyDate)
                        .ToList();

                    // 查詢該選手的雷達圖數據（最近 3 筆測試日期）
                    var recentDates = _db.PsychologicalResponse
                        .Where(p => p.UserID == userId)
                        .Select(p => p.SurveyDate)
                        .Distinct()
                        .OrderByDescending(d => d)
                        .Take(3)
                        .ToList();

                    foreach (var date in recentDates)
                    {
                        string query = @"SELECT c.CategoryName, COALESCE(AVG(pr.Score), 0) AS AverageScore 
                                FROM PsychologicalResponse pr
                                INNER JOIN PsychologicalStateCategory c ON pr.CategoryID = c.ID 
                                WHERE pr.UserID = @p0 AND pr.SurveyDate = @p1 
                                GROUP BY c.CategoryName";

                        object[] parameters = { userId, date };
                        var data = _db.Database.SqlQuery<RadarChartVIewModel>(query, parameters).ToList();

                        foreach (var item in data)
                        {
                            item.SurveyDate = date?.ToString("yyyy-MM-dd");
                        }
                        radarData.AddRange(data);
                    }
                }

                // 取得心理狀態技能說明
                var psyDescriptions = _db.PsychologicalStateDescription.OrderBy(d => d.ID).ToList();
                ViewBag.PsychologicalDescriptions = psyDescriptions;

                ViewBag.RadarData = radarData;

                return View(rawData);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "發生錯誤：" + ex.Message;
                return View(new List<TeamRawDataViewModel>());
            }
        }
        #endregion
    }
}