using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tiss_MindRadar.Models
{
	public class RefereeViewModel
	{
        public class SmoothExperienceViewModel
        {
            public int QuestionID { get; set; }
            public string QuestionText { get; set; }
        }

        public class SmoothExperienceResponseViewModel
        {
            public int QuestionID { get; set; }
            public int Score { get; set; }
        }
    }
}