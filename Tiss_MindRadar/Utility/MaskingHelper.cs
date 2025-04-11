using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tiss_MindRadar.Utility
{
	public static class MaskingHelper
	{
        #region 名字去識別化
        public static string MaskUserName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            if (name.Length <= 2) return name.Substring(0, 1) + "O";
            return name.Substring(0, 1) + "O" + name.Substring(2);
        }
        #endregion
    }
}