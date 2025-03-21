using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tiss_MindRadar.Models
{
    public class RadarChartVIewModel
    {
        public string CategoryName { get; set; } // 分類名稱
        public int AverageScore { get; set; } // 平均分數
        public string SurveyDate { get; set; }
        public string Dimension { get; set; }
        public List<RadarChartModel> Scores { get; set; }

        public class RadarChartModel
        {
            public string Label { get; set; }
            public double Value { get; set; }
        }

        public class RadarChartRawData
        {
            public string CategoryName { get; set; } // 分類名稱
            public int UserID { get; set; }          // 用戶 ID
            public int QuestionID { get; set; }      // 問題 ID
            public int Score { get; set; }           // 分數
            public string Category { get; set; }
        }
    }
}