using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tiss_MindRadar.ViewModels
{
	public class TeamRawDataViewModel
	{
        public string TeamName { get; set; }
        public string UserName { get; set; }
        public string Category { get; set; }
        public int Score { get; set; }
        public DateTime? SurveyDate { get; set; }
        public string CategoryName { get; set; } // 向度（基礎心理技能等）
        public string SkillTitle { get; set; }   // 技能名稱（如：目標設定、自信心）
        public string Description { get; set; }  // 技能說明
    }

    public class TeamReportViewModel
    {
        public string UserName { get; set; }
        public string Gender { get; set; }
        public string Category { get; set; }
        public double Score { get; set; }
        public DateTime? SurveyDate { get; set; }
    }
}