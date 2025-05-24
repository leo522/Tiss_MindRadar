using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tiss_MindRadar.Models;
using Tiss_MindRadar.ViewModels;
using Tiss_MindRadar.Utility;

namespace Tiss_MindRadar.Controllers
{
    public class TeamRawDataController : Controller
    {
        private TISS_MindRadarEntities _db = new TISS_MindRadarEntities(); //資料庫

        #region 選擇檢測分數頁面
        public ActionResult ChooseTeamState()
        {
            return View();
        }
        #endregion

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
            var users = _db.Users.Where(u => u.TeamID == teamId).ToList().Select(u => new
            {
                u.UserID,
                UserName = MaskingHelper.MaskUserName(u.UserName)
            }).ToList();

            return Json(users);
        }
        #endregion

        #region 檢查選手是否有數據
        [HttpPost]
        public JsonResult CheckUserData(int? userId)
        {
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "未提供選手 ID" });
            }

            var hasData = _db.PsychologicalResponse.Any(p => p.UserID == userId);

            if (!hasData)
            {
                return Json(new { success = false, message = "該選手無心理狀態數據" });
            }

            return Json(new { success = true });
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
                List<RadarChartViewModel> radarData = new List<RadarChartViewModel>();

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
                        string query = @"SELECT d.CategoryName AS Dimension, d.SubCategory AS CategoryName, 
                                        COALESCE(AVG(pr.Score), 0) AS AverageScore FROM PsychologicalResponse pr
                                        INNER JOIN PsychologicalStateDescription d ON pr.CategoryID = d.ID
                                        WHERE pr.UserID = @p0 AND pr.SurveyDate = @p1
                                        GROUP BY d.CategoryName, d.SubCategory, d.ID, d.HeaderID
                                        ORDER BY CASE 
                                        WHEN d.CategoryName = '基礎心理技能' THEN 1 
                                        WHEN d.CategoryName = '身體心理技能' THEN 2 
                                        WHEN d.CategoryName = '認知技能' THEN 3 ELSE 4 END, d.ID";

                        object[] parameters = { userId, date };
                        var data = _db.Database.SqlQuery<RadarChartViewModel>(query, parameters).ToList();

                        foreach (var item in data)
                        {
                            item.SurveyDate = date?.ToString("yyyy-MM-dd");
                        }

                        ViewBag.MaskedUserName = MaskingHelper.MaskUserName(_db.Users.Where(u => u.UserID == userId).Select(u => u.UserName).FirstOrDefault());

                        radarData.AddRange(data);
                    }
                }

                // 取得心理狀態技能描述
                var psy = _db.PsychologicalStateDescription.OrderBy(d => d.ID).ToList();
                var headers = _db.PsychologicalStateHeader.ToList();
                var mentalStates = _db.MentalState.OrderBy(m => m.QuestionNumber).ToList();

                ViewBag.PsychologicalDescriptions = psy;
                ViewBag.PsychologicalHeaders = headers;
                ViewBag.MentalStateQuestions = mentalStates;

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

        #region 身心狀態檢測雷達圖_各隊選手分數
        [HttpPost]
        public ActionResult GetMentalPhysicalStateTeamRawData(int? userId, int? teamId)
        {
            if (Session["UserID"] == null || Session["UserRole"]?.ToString() != "Consultant")
                return RedirectToAction("Login", "UserAccount");

            ViewBag.Teams = new SelectList(_db.Team.Where(t => t.isenable), "TeamID", "TeamName", teamId);

            // 取得選手清單
            if (teamId.HasValue)
            {
                var userIds = _db.MentalStateResponse
                    .Join(_db.Users, msr => msr.UserID, u => u.UserID, (msr, u) => new { msr, u })
                    .Where(x => x.u.TeamID == teamId)
                    .Select(x => x.u.UserID)
                    .Distinct()
                    .ToList();

                var users = _db.Users
                    .Where(u => userIds.Contains(u.UserID))
                    .Select(u => new { u.UserID, u.UserName })
                    .ToList();

                ViewBag.Users = new SelectList(users, "UserID", "UserName", userId);
            }
            else
            {
                ViewBag.Users = new SelectList(new List<object>(), "UserID", "UserName");
            }

            var rawData = new List<TeamRawDataViewModel>();
            var radarData = new List<RadarChartViewModel>();

            if (userId.HasValue)
            {
                // 取得表格用的原始資料
                rawData = _db.MentalStateResponse
                    .Join(_db.Users, m => m.UserID, u => u.UserID, (m, u) => new { m, u })
                    .Join(_db.MentalPhysicalState, temp => temp.m.CategoryID, q => q.QuestionNumber, (temp, q) => new { temp, q })
                    .Where(x => x.temp.u.UserID == userId)
                    .Select(x => new TeamRawDataViewModel
                    {
                        TeamName = x.temp.u.TeamName,
                        UserName = x.temp.u.UserName,
                        Category = x.q.QuestionText,
                        Score = x.temp.m.Score,
                        SurveyDate = x.temp.m.SurveyDate
                    }).AsEnumerable()
                    .Select(x => 
                    {x.UserName = MaskingHelper.MaskUserName(x.UserName);
                        return x;
                    }).ToList();

                // 取得最近一次測驗日期
                var recentDate = _db.MentalStateResponse
                    .Where(x => x.UserID == userId && x.SurveyDate != null)
                    .Select(x => x.SurveyDate)
                    .OrderByDescending(d => d)
                    .FirstOrDefault();

                // 將該次測驗的每題平均分數作為雷達圖資料
                if (recentDate != null)
                {
                    radarData = _db.MentalStateResponse
                        .Join(_db.MentalPhysicalState, r => r.CategoryID, q => q.QuestionNumber, (r, q) => new { r, q })
                        .Where(x => x.r.UserID == userId && x.r.SurveyDate == recentDate)
                        .GroupBy(x => x.q.QuestionText)
                        .ToList()
                        .Select(g => new RadarChartViewModel
                        {
                            CategoryName = g.Key,
                            AverageScore = Convert.ToInt32(g.Average(x => x.r.Score)),
                            SurveyDate = recentDate.Value.ToString("yyyy-MM-dd")
                        })
                        .ToList();
                }

                ViewBag.MaskedUserName = MaskingHelper.MaskUserName(
                    _db.Users.FirstOrDefault(u => u.UserID == userId)?.UserName);
            }

            ViewBag.RadarData = radarData;
            return View("GetMentalPhysicalStateTeamRawData", rawData);
        }
        #endregion

        #region 身心狀態檢測_根據隊伍ID取得選手
        [HttpPost]
        public JsonResult MentalPhysical_GetUsersByTeam(int teamId)
        {
            try
            {
                var usersRaw = _db.Users
                    .Where(u => u.TeamID == teamId && u.UserName != null)
                    .ToList(); // 先從資料庫取出再進行處理

                var users = usersRaw.Select(u => new
                {
                    u.UserID,
                    UserName = MaskingHelper.MaskUserName(u.UserName)
                }).ToList();

                return Json(users);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { error = "伺服器錯誤：" + ex.Message });
            }
        }
        #endregion

        #region 身心狀態檢測_檢查選手是否有身心狀態數據
        [HttpPost]
        public JsonResult MentalPhysical_CheckUserData(int? userId)
        {
            if (!userId.HasValue)
                return Json(new { success = false });

            var hasData = _db.MentalStateResponse
                            .Any(p => p.UserID == userId && p.SurveyDate != null);

            return Json(new { success = hasData });
        }
        #endregion

        #region 計算該隊伍中有填寫過問卷的選手數量
        [HttpPost]
        public JsonResult GetSurveyCountByTeam(int teamId)
        {
            int count = _db.PsychologicalResponse
                .Where(p => _db.Users.Any(u => u.UserID == p.UserID && u.TeamID == teamId))
                .Select(p => p.UserID)
                .Distinct()
                .Count();

            return Json(new { success = true, surveyCount = count });
        }
        #endregion
    }
}