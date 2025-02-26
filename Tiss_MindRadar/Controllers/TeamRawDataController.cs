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
        [HttpGet]
        public JsonResult GetUsersByTeam(int teamId)
        {
            var users = _db.Users
                .Where(u => u.TeamID == teamId)
                .Select(u => new { u.UserID, u.UserName })
                .ToList();

            return Json(users, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region 心理狀態檢測雷達圖_各隊選手分數
        [HttpGet]
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
                    return RedirectToAction("AccessDenied", "RadarError"); // 沒權限的話導向權限不足頁面
                }

                // 載入隊伍清單
                var teams = _db.Team.Where(t => t.isenable == true).ToList();
                ViewBag.Teams = new SelectList(teams, "TeamID", "TeamName", teamId); // 保持選擇狀態

                // 如果隊伍已選擇，則載入該隊伍的選手
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
                    ViewBag.Users = new SelectList(new List<object>(), "UserID", "UserName"); // 預設空選單
                }

                // 如果選擇了選手，則載入雷達圖數據
                List<TeamRawDataViewModel> rawData = new List<TeamRawDataViewModel>();

                if (userId.HasValue)
                {
                    rawData = _db.PsychologicalResponse
                        .Join(_db.Users, pr => pr.UserID, u => u.UserID, (pr, u) => new { pr, u })
                        .Where(temp => temp.u.UserID == userId)
                        .Select(temp => new TeamRawDataViewModel
                        {
                            TeamName = temp.u.TeamName,
                            UserName = temp.u.UserName,
                            Category = temp.pr.CategoryID.ToString(),
                            Score = temp.pr.Score,
                            SurveyDate = temp.pr.SurveyDate
                        })
                        .OrderBy(r => r.SurveyDate)
                        .ToList();
                }

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