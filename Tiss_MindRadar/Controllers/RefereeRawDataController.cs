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

        #region 查看流暢經驗_裁判版量表分數 (雷達圖)
        public ActionResult SmoothExperienceResult()
        {
            // **查詢每個問題的填答數據**
            var questionScores = _db.SmoothExperience
                .Join(_db.SmoothExperienceResponse,
                        s => s.QuestionID,
                        r => r.QuestionID,
                        (s, r) => new
                    {
                        s.QuestionID,
                        s.QuestionText,
                        r.Score
                    }).OrderBy(q => q.QuestionID).ToList();

            var questionScoresList = questionScores
                 .Select(q => new Dictionary<string, object>
                {
                    { "QuestionID", q.QuestionID },
                    { "QuestionText", q.QuestionText },
                    { "Score", q.Score }
                 }).ToList();

            // **將結果存入 ViewBag**
            ViewBag.QuestionScores = questionScoresList;

            return View();
        }
        #endregion
    }
}