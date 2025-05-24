using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tiss_MindRadar.ViewModels
{
    public class RadarChartViewModel
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

        public class RadarCommentViewModel
        {
            public int CommentID { get; set; }
            public string CommentText { get; set; }
            public string SurveyDate { get; set; }
            public string CreatedAt { get; set; }
            public string UserName { get; set; }
            public string Role { get; set; }
            public List<RadarReplyViewModel> Replies { get; set; }
        }

        public class RadarReplyViewModel
        {
            public string ReplyText { get; set; }
            public string CreatedAt { get; set; }
            public string PsychologistName { get; set; }
        }
    }
}