using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tiss_MindRadar.Models;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;

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

                var questionScoresList = questionScores
                     .Select(q => new Dictionary<string, object>
                     {
                { "QuestionID", q.QuestionID },
                { "QuestionText", q.QuestionText },
                { "Score", q.Score }
                     }).ToList();

                ViewBag.QuestionScores = questionScoresList;

                // **計算每個類別的平均分數**
                var categoryScores = questionScores
                    .GroupBy(q => q.CategoryID)  // **依據 CategoryID 分組**
                    .Select(g => new
                    {
                        CategoryItem = _db.SmoothExperienceCategory
                            .Where(c => c.CategoryID == g.Key)
                            .Select(c => c.CategoryItem)
                            .FirstOrDefault(),
                        AverageScore = Math.Round(g.Average(q => q.Score), 1) // **計算平均值，保留 1 位小數**
                    })
                    .ToList();

                // **存入 ViewBag，讓 View 使用**
                ViewBag.Categories = categoryScores.Select(c => c.CategoryItem).ToList();
                ViewBag.Scores = categoryScores.Select(c => c.AverageScore).ToList();

                return View();
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

                // **查詢每個問題的填答數據**
                var questionScores = _db.ProfessionalCapabilities
                    .Join(_db.ProfessionalCapabilitiesResponse,
                            p => p.QuestionID,
                            r => r.QuestionID,
                            (p, r) => new
                            {
                                p.QuestionID,
                                p.QuestionText,
                                r.Score
                            }).OrderBy(q => q.QuestionID).ToList();

                var questionScoresList = questionScores
                     .Select(q => new Dictionary<string, object>
                     {
                { "QuestionID", q.QuestionID },
                { "QuestionText", q.QuestionText },
                { "Score", q.Score }
                     }).ToList();

                ViewBag.QuestionScores = questionScoresList;

                // **計算類別平均分數**
                var categoryScores = _db.ProfessionalCapabilities
                    .Join(_db.ProfessionalCapabilitiesResponse, p => p.QuestionID, r => r.QuestionID, (p, r) => new { p.CategoryID, r.Score })
                    .GroupBy(g => g.CategoryID)
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

                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion
    }
}