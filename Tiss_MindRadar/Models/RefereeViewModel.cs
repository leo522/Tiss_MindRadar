using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tiss_MindRadar.Models
{
	public class RefereeViewModel
	{
        #region 流暢經驗
        public class SmoothExperienceViewModel
        {
            public int QuestionID { get; set; }
            public string QuestionText { get; set; }
        }

        public class SmoothExperienceCategoryViewModel
        {
            public int CategoryID { get; set; }
            public string CategoryItem { get; set; }  // 分類名稱
            public List<SmoothExperienceViewModel> Questions { get; set; } // 問題列表
        }

        public class SmoothExperienceResponseViewModel
        {
            public int QuestionID { get; set; }
            public int Score { get; set; }
        }
        #endregion

        #region 專業能力
        public class ProfessionalCapabilitiesViewModel
        {
            public int QuestionID { get; set; }
            public string QuestionText { get; set; }
        }

        public class ProfessionalCapabilitiesCategoryViewModel
        {
            public int CategoryID { get; set; }
            public string CategoryItem { get; set; }  // 分類名稱
            public List<ProfessionalCapabilitiesViewModel> Questions { get; set; } // 問題列表
        }

        public class ProfessionalCapabilitiesResponseViewModel
        {
            public int QuestionID { get; set; }
            public int Score { get; set; }
        }
        #endregion
    }
}