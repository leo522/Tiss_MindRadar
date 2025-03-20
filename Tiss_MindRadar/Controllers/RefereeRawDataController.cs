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

namespace Tiss_MindRadar.Controllers
{
   public class RefereeRawDataController : Controller
   {
        private TISS_MindRadarEntities _db = new TISS_MindRadarEntities(); //資料庫

        #region 流暢經驗_裁判版量表分數 (雷達圖)
        public ActionResult SmoothExperienceResult()
        {
            try
            {

                if (Session["UserID"] == null)
                {
                    return RedirectToAction("Login", "UserAccount");
                }

                ViewBag.RefereeName = Session["UserName"];
                ViewBag.RefereeTeamName = Session["RefereeTeamName"];

                // **查詢每個問題的填答數據**
                var questionScores = _db.SmoothExperience
                    .Join(_db.SmoothExperienceResponse,
                          s => s.QuestionID,
                          r => r.QuestionID,
                          (s, r) => new
                          {
                              s.QuestionID,
                              s.QuestionText,
                              s.CategoryID,
                              r.Score
                          })
                    .OrderBy(q => q.QuestionID)
                    .ToList();

                // **分組問題，按主題分類**
                var groupedQuestions = questionScores
                    .GroupBy(q => q.CategoryID)
                    .Select(g => new SmoothExperienceCategoryViewModel
                    {
                        CategoryID = g.Key,
                        CategoryItem = _db.SmoothExperienceCategory
                            .Where(c => c.CategoryID == g.Key)
                            .Select(c => c.CategoryItem)
                            .FirstOrDefault(),
                        Questions = g.Select(q => new SmoothExperienceViewModel
                        {
                            QuestionID = q.QuestionID,
                            QuestionText = q.QuestionText,
                            Score = q.Score
                        }).ToList()
                    })
                    .ToList();

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
                    })
                    .ToList();

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
        public ActionResult ProfessionalCapabilitiesResult()
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    return RedirectToAction("Login", "UserAccount");
                }

                ViewBag.RefereeName = Session["UserName"];
                ViewBag.RefereeTeamName = Session["RefereeTeamName"];

                var questionScores = _db.ProfessionalCapabilities
                    .Join(_db.ProfessionalCapabilitiesResponse,
                          p => p.QuestionID,
                          r => r.QuestionID,
                          (p, r) => new
                          {
                              p.QuestionID,
                              p.QuestionText,
                              r.Score,
                              p.CategoryID
                          })
                    .OrderBy(q => q.QuestionID)
                    .ToList();

                var groupedQuestions = questionScores
                    .GroupBy(q => q.CategoryID)
                    .Select(g => new ProfessionalCapabilitiesCategoryViewModel
                    {
                        CategoryID = g.Key,
                        CategoryItem = _db.ProfessionalCapabilitiesCategory
                            .Where(c => c.CategoryID == g.Key)
                            .Select(c => c.CategoryItem)
                            .FirstOrDefault(),
                        Questions = g.Select(q => new ProfessionalCapabilitiesViewModel
                        {
                            QuestionID = q.QuestionID,
                            QuestionText = q.QuestionText,
                            Score = q.Score
                        }).ToList()
                    })
                    .ToList();

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