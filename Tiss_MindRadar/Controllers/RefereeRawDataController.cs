using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tiss_MindRadar.Models;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using static Tiss_MindRadar.Models.RefereeViewModel;
using System.Data.Entity;

namespace Tiss_MindRadar.Controllers
{
    public class RefereeRawDataController : Controller
    {
        private TISS_MindRadarEntities _db = new TISS_MindRadarEntities(); //資料庫

        #region 流暢經驗_裁判版量表分數 (雷達圖)
        public ActionResult SmoothExperienceResult(DateTime? selectedDate)
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    return RedirectToAction("Login", "UserAccount");
                }

                ViewBag.RefereeName = Session["UserName"];
                ViewBag.RefereeTeamName = Session["RefereeTeamName"];

                // 取得所有填寫日期（不重複）
                var allDates = _db.SmoothExperienceResponse
                    .Select(r => r.SubmittedAt).AsEnumerable()
                    .Select(r => r.Date).Distinct().OrderByDescending(d => d).ToList();

                ViewBag.SurveyDates = allDates;

                // 若未指定則取最新日期
                DateTime targetDate = selectedDate ?? allDates.FirstOrDefault();
                ViewBag.SelectedDate = targetDate.ToString("yyyy-MM-dd");

                // 以區間方式查詢當日所有資料（避免 Date 屬性轉換錯誤）
                DateTime startDate = targetDate.Date;
                DateTime endDate = startDate.AddDays(1);

                // 先查出當日的 Response 資料（必要）
                var dailyResponses = _db.SmoothExperienceResponse.Where(r => r.SubmittedAt >= startDate && r.SubmittedAt < endDate).ToList();

                var responseQuestionIds = dailyResponses.Select(r => r.QuestionID).Distinct().ToList();

                // 加入題目資訊
                var questionScores = _db.SmoothExperience.Where(q => responseQuestionIds.Contains(q.QuestionID)).ToList()
                                        .Join(dailyResponses,
                                            q => q.QuestionID,
                                            r => r.QuestionID,
                                            (q, r) => new
                                        {
                                            q.QuestionID,
                                            q.QuestionText,
                                            q.CategoryID,
                                            r.Score
                                        }).OrderBy(x => x.QuestionID).ToList();

                // **分組問題，按主題分類**
                var groupedQuestions = questionScores.GroupBy(q => q.CategoryID)
                        .Select(g => new SmoothExperienceCategoryViewModel
                    {
                        CategoryID = g.Key,
                        CategoryItem = _db.SmoothExperienceCategory.Where(c => c.CategoryID == g.Key).Select(c => c.CategoryItem)
                                            .FirstOrDefault(),
                        Questions = g.Select(q => new SmoothExperienceViewModel
                        {
                            QuestionID = q.QuestionID,
                            QuestionText = q.QuestionText,
                            Score = q.Score
                        }).ToList()
                        }).ToList();

                ViewBag.QuestionGroups = groupedQuestions;

                // **計算類別平均分數，提供給雷達圖**
                var categoryScores = questionScores
                    .GroupBy(q => q.CategoryID)
                    .Select(g => new
                    {
                        CategoryName = _db.SmoothExperienceCategory
                            .Where(c => c.CategoryID == g.Key)
                            .Select(c => c.CategoryItem)
                            .FirstOrDefault(),
                        AverageScore = Math.Round(g.Average(x => x.Score), 1) // **取到小數點第一位**
                    }).ToList();

                // ✅ **確保 ViewBag 不為空，避免 JS 出錯**
                ViewBag.Categories = categoryScores.Select(c => c.CategoryName).ToList();
                ViewBag.Scores = categoryScores.Select(c => c.AverageScore).ToList();

                return View(groupedQuestions);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 專業能力_裁判版量表分數 (雷達圖)
        public ActionResult ProfessionalCapabilitiesResult(DateTime? selectedDate)
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    return RedirectToAction("Login", "UserAccount");
                }

                ViewBag.RefereeName = Session["UserName"];
                ViewBag.RefereeTeamName = Session["RefereeTeamName"];

                var allDates = _db.ProfessionalCapabilitiesResponse.Select(r => r.SubmittedAt).AsEnumerable()
                                    .Select(r => r.Date).Distinct().OrderByDescending(d => d).ToList();

                ViewBag.SurveyDates = allDates;

                //若未指定則取最新日期
                DateTime targetDate = selectedDate ?? allDates.FirstOrDefault();
                ViewBag.SelectedDate = targetDate.ToString("yyyy-MM-dd");

                //以區間方式查詢當日所有資料（避免 Date 屬性轉換錯誤）
                DateTime startDate = targetDate.Date;
                DateTime endDate = startDate.AddDays(1);

                var dailyResponses = _db.ProfessionalCapabilitiesResponse.Where(r => r.SubmittedAt >= startDate && r.SubmittedAt < endDate).ToList();

                var responseQuestionIds = dailyResponses.Select(r => r.QuestionID).Distinct().ToList();

                var questionScores = _db.ProfessionalCapabilities.Where(q => responseQuestionIds.Contains(q.QuestionID)).ToList().Join(dailyResponses,
                                        q => q.QuestionID,
                                        r => r.QuestionID,
                                        (q, r) => new
                                        {
                                            q.QuestionID,
                                            q.QuestionText,
                                            q.CategoryID,
                                            r.Score
                                        }).OrderBy(q => q.QuestionID).ToList();

                var groupedQuestions = questionScores.GroupBy(q => q.CategoryID)
                                        .Select(g => new ProfessionalCapabilitiesCategoryViewModel
                                    {
                                        CategoryID = g.Key,
                                        CategoryItem = _db.ProfessionalCapabilitiesCategory.Where(c => c.CategoryID == g.Key)
                                        .Select(c => c.CategoryItem).FirstOrDefault(),
                                        Questions = g.Select(q => new ProfessionalCapabilitiesViewModel
                                    {
                                        QuestionID = q.QuestionID,
                                        QuestionText = q.QuestionText,
                                        Score = q.Score
                                    }).ToList()
                                    }).ToList();

                //計算各類別的平均分數，提供給雷達圖
                var categoryScores = questionScores
                    .GroupBy(q => q.CategoryID)
                    .Select(g => new
                    {
                        CategoryName = _db.ProfessionalCapabilitiesCategory
                            .Where(c => c.CategoryID == g.Key)
                            .Select(c => c.CategoryItem)
                            .FirstOrDefault(),
                        AverageScore = Math.Round(g.Average(x => x.Score), 1) // **取到小數點第一位**
                    })
                    .ToList();

                ViewBag.Categories = categoryScores.Select(c => c.CategoryName).ToList();
                ViewBag.Scores = categoryScores.Select(c => c.AverageScore).ToList();

                return View(groupedQuestions);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "發生錯誤：" + ex.Message);
                return View(new List<ProfessionalCapabilitiesCategoryViewModel>());
            }
        }
        #endregion
    }
}